﻿using SqCommon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//Since the System.Web.dll is dropped, the JavaScriptSerializer was also not included in ASP.Net 5. And Microsoft also suggesting us to use the `Newtonsoft.Json`.
//You can use Newtonsoft.Json, it's a dependency of Microsoft.AspNet.Mvc.Formatters.Json witch 
//is a dependency of Microsoft.AspNet.Mvc. So, you don't need to add a dependency in your project.json.

namespace HealthMonitor
{
    public partial class HealthMonitor
    {
        const int c_maxAllowedFail = 1;     // when Linux restarts every day, one query can be a failed one. That is OK. Don't send warning email.
        int m_nFail = 0;
        bool m_isThisServiceOutageWarningEmailWasSent = false;  // to avoid sending the same warning email every 10 minutes; send only once
        ConcurrentQueue<Tuple<DateTime, bool>> m_rtpsLastDownloads = new ConcurrentQueue<Tuple<DateTime, bool>>();

        // imagine how and when a human user would check that the service is still OK. He wouldn't check it on the weekends e.g.
        private void RtpsTimer_Elapsed(object p_stateObj)   // Real Time Price Service, // Timer is coming on a ThreadPool thread
        {
            try
            {
                Utils.Logger.Info(String.Format("RtpsTimer_Elapsed_{0} ({1} minutes) BEGIN", m_nRtpsTimerCalled, cRtpsTimerFrequencyMinutes));
                m_nRtpsTimerCalled++;
                if (!PersistedState.IsRealtimePriceServiceTimerEnabled)
                    return;

                DateTime utcNow = DateTime.UtcNow;
                TimeSpan utcNowTime = utcNow.TimeOfDay;
                // when developing, we don't want to restrict the time it can run, only on Linux production environment
                if (!IsRunningAsLocalDevelopment())
                {
                    // Now, in 2015, there is no point at checking it overnight, as no Developer will be able to fix it.
                    // in Utc time USA stock market is open around 15:30-21:00 or 14:30-20:00, but developer's sleeping in the main factor
                    // in the future, we may trade non-stop, 7/24, 7 days, 24 hours, but not now, so we can save resources by not checking overnight
                    if (utcNowTime > new TimeSpan(23, 15, 0) || utcNowTime < new TimeSpan(7, 30, 0))
                        return;
                    //similarly, even though we can trade on the weekend, we don't do that now, so save resources, don't check
                    if (utcNow.DayOfWeek == DayOfWeek.Saturday || utcNow.DayOfWeek == DayOfWeek.Sunday)
                        return;
                }


                string url = "https://www.snifferquant.net/rtp?s=VXX,^VIX,^GSPC,SVXY&f=l"; // 2018-10-10: thinking about removing ^VIX,^GSPC so less strain on VBroker. But the point of HealthMonitor is to see if there is a problem (no index data subscription). So, keep them.
                string rtpsReply = String.Empty;
                if (Utils.DownloadStringWithRetry(out rtpsReply, url, 5, TimeSpan.FromSeconds(5), false))
                    Utils.Logger.Info(url + " returned: " + (rtpsReply.Substring(0, (rtpsReply.Length > 45) ? 45 : rtpsReply.Length)).Replace("\r\n", "").Replace("\n", ""));   // it is better to see it as one line in the log file
                else
                {
                    Utils.Logger.Error("Failed download multiple (5x) times :" + url);
                }

                bool? isRtpsReplyOk = null;
                if (IsRunningAsLocalDevelopment())
                {
                    if (rtpsReply.StartsWith("<html><body>Choose an authentication scheme:", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Utils.Logger.Info("RtpsTimer_Elapsed(). RunningAsLocalDevelopment() (WindowsPC). Download was OK, but webserver asked for GoogleAuth. If you want to debug further functionality, put the Developer PC IP (dynamic) to the webserver whitelist.");
                        isRtpsReplyOk = true;       // imitate that it is OK. Because when it is run locally, we don't want to receive a warning email
                    }
                }

                if (isRtpsReplyOk == null)
                    isRtpsReplyOk = IsRtpsReplyOk(rtpsReply);

                m_rtpsLastDownloads.Enqueue(new Tuple<DateTime, bool>(DateTime.UtcNow, ((bool)isRtpsReplyOk)));
                while (m_rtpsLastDownloads.Count > 2 * 24 * 6)     // to avoid increasing memory forever, trim the records after 2 days
                {
                    Tuple<DateTime, bool> rtpsDownload = null;
                    m_rtpsLastDownloads.TryDequeue(out rtpsDownload);
                }

                if (!((bool)isRtpsReplyOk))
                {
                    Utils.Logger.Info("RtpsTimer_Elapsed(). !isRtpsReplyOk");
                    m_nFail++;
                    if ((m_nFail > c_maxAllowedFail) && !m_isThisServiceOutageWarningEmailWasSent)
                    {
                        Utils.Logger.Info("RtpsTimer_Elapsed(). Sending Warning email.");
                        new Email
                        {
                            ToAddresses = Utils.Configuration["EmailGyantal"],
                            Subject = "SQ HealthMonitor: WARNING! RealTime Price Service stopped working.",
                            Body = $"SQ HealthMonitor: WARNING! RealTime Price Service stopped working.\nRtpsTimer_Elapsed() failed {m_nFail} times.\n{url}\n returned this: '" + rtpsReply + "'",
                            IsBodyHtml = false
                        }.Send();
                        m_isThisServiceOutageWarningEmailWasSent = true;
                    }
                }
                else
                {
                    m_nFail = 0;
                    Utils.Logger.Info("RtpsTimer_Elapsed(). isRtpsReplyOk");
                    if (m_isThisServiceOutageWarningEmailWasSent)
                    {  // it was bad, but now it is corrected somehow
                        new Email
                        {
                            ToAddresses = Utils.Configuration["EmailGyantal"],
                            Subject = "SQ HealthMonitor: OK! RealTime Price Service has recovered.",
                            Body = "SQ HealthMonitor: OK! RealTime Price Service has recovered.",
                            IsBodyHtml = false
                        }.Send();
                        m_isThisServiceOutageWarningEmailWasSent = false;
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Logger.Error(e, "RtpsTimer_Elapsed() exception.");
            }

            Utils.Logger.Info(String.Format("RtpsTimer_Elapsed_{0} ({1} minutes) END", m_nRtpsTimerCalled, cRtpsTimerFrequencyMinutes));
        }

        //on weekends and on days when stock market is not open(holidays) this is what it returns, which is fine:
        //>>>on Saturday: [{"Symbol":"VXX"},{"Symbol":"^VIX"},{"Symbol":"^VXV"},{"Symbol":"^GSPC","LastUtc":"2015-12-13T00:39:41","Last":2010.52,"UtcTimeType":"LastChangedTime"},{"Symbol":"XIV"}]
        //>>>On Sunday: [{"Symbol":"VXX"},{"Symbol":"^VIX"},{"Symbol":"^VXV"},{"Symbol":"^GSPC"},{"Symbol":"XIV"}]
        //But this return without prices at least it shows that the Service is not crashed, it returns data
        //>>>Monday night: [{"Symbol":"VXX","LastUtc":"2015-12-15T00:56:09","Last":21.97,"BidUtc":"2015-12-15T00:59:58","Bid":21.88,"AskUtc":"2015-12-15T00:58:33","Ask":22,"UtcTimeType":"LastChangedTime"},{"Symbol":"^VIX","LastUtc":"2015-12-14T21:14:47","Last":22.73,"UtcTimeType":"LastChangedTime"},{"Symbol":"^VXV","LastUtc":"2015-12-14T21:14:47","Last":23.3,"UtcTimeType":"LastChangedTime"},{"Symbol":"^GSPC","LastUtc":"2015-12-15T00:13:07","Last":2022.52,"UtcTimeType":"LastChangedTime"},{"Symbol":"XIV","LastUtc":"2015-12-15T00:59:34","Last":24.23,"BidUtc":"2015-12-15T00:59:58","Bid":24.17,"AskUtc":"2015-12-15T00:59:03","Ask":24.23,"UtcTimeType":"LastChangedTime"}]
        private bool IsRtpsReplyOk(string rtpsReply)
        {
            // implement check logic here.
            if (String.IsNullOrEmpty(rtpsReply)) // 1. If nothing is returned or webClient.DownloadString() crashed
                return false;

            try
            {
                var rtpsReplyDicts = Utils.LoadFromJSON<List<Dictionary<string, string>>>(rtpsReply);
                var vxxItem = rtpsReplyDicts.Find(r => r["Symbol"] == "VXX");
                if (vxxItem == null)
                    return false;   // 2. if record is not found
                string vxxLastPriceStr;
                bool isVXXHasLastPrice = vxxItem.TryGetValue("Last", out vxxLastPriceStr);
                if (!isVXXHasLastPrice)
                {
                    if (!Utils.IsInRegularUsaTradingHoursNow(TimeSpan.FromDays(3)))
                    {
                        // if it is premarket (specially on Monday), we would 
                        // accept if it does'nt have Last price "'[{"Symbol":"VXX"},{"Symbol":"^VIX"},""
                        return true;
                    }
                }
                if (String.IsNullOrEmpty(vxxLastPriceStr))  // "Last":21.97
                    return false;   // 3. if it's Last price is empty. 
                double vxxLastPrice;
                if (!Double.TryParse(vxxLastPriceStr, out vxxLastPrice))
                    return false;   // 4. if it is not a number. For example if it is "N/A", we return error

            }
            catch (Exception ex)
            {
                Utils.Logger.Error(ex, "Exception in IsRtpsReplyOk()");
                return false;
            }

            return true;
        }
    }
}
