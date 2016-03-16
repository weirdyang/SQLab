﻿using SqCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualBroker
{



    public class BrokerScheduler
    {
        internal void Init()
        {
            Utils.Logger.Info("****BrokerScheduler:Init()");
            Task tcpListenerTask = Task.Factory.StartNew(SchedulerThreadRun, TaskCreationOptions.LongRunning);  // a separate thread. Not on ThreadPool
        }

        private void SchedulerThreadRun()
        {
            try
            {
                Thread.CurrentThread.Name = "Broker scheduler";

                // maybe loop is not required.
                // in the past we try to get UsaMarketOpenOrCloseTime() every 30 minutes. It was determined from YFinance intrady. "sleep 30 min for DetermineUsaMarketOpenOrCloseTime()"
                // however, it may be a good idea that the Scheduler periodically wakes up and check Tasks
                while (true) 
                {
                    bool isMarketTradingDay;
                    DateTime marketOpenTimeUtc, marketCloseTimeUtc;
                    bool isTradingHoursOK = Utils.DetermineUsaMarketTradingHours(DateTime.UtcNow, out isMarketTradingDay, out marketOpenTimeUtc, out marketCloseTimeUtc, TimeSpan.FromDays(3));
                    if (!isTradingHoursOK)
                    {
                        Utils.Logger.Error("DetermineUsaMarketTradingHours() was not ok.");
                    }
                    else
                    {
                        foreach (BrokerTaskSchema taskSchema in Controller.g_taskSchemas)
                        {
                            foreach (Trigger trigger in taskSchema.Triggers)
                            {
                                ScheduleTrigger(trigger, isMarketTradingDay, marketOpenTimeUtc, marketCloseTimeUtc);
                            }
                        }
                    }

                    Thread.Sleep(TimeSpan.FromMinutes(30));     // try reschedulement in 30 minutes
                }
            }
            catch (Exception e)
            {
                HealthMonitorMessage.SendException("BrokerScheduler.RecreateTasksAndLoop Thread", e, HealthMonitorMessageID.ReportErrorFromVirtualBroker);
            }
        }

        internal void ScheduleTrigger(Trigger p_trigger, bool p_isMarketTradingDay, DateTime p_marketOpenTimeUtc, DateTime p_marketCloseTimeUtc)
        {
            DateTime? proposedTime = CalcNextTriggerTime(p_trigger, p_isMarketTradingDay, p_marketOpenTimeUtc, p_marketCloseTimeUtc);
            if (proposedTime != null)
            {
                bool doSetTimer = true;
                if (p_trigger.NextScheduleTimeUtc != null)
                {
                    TimeSpan timeSpan = ((DateTime)p_trigger.NextScheduleTimeUtc > (DateTime)proposedTime) ? (DateTime)p_trigger.NextScheduleTimeUtc - (DateTime)proposedTime : (DateTime)proposedTime - (DateTime)p_trigger.NextScheduleTimeUtc;
                    if (timeSpan.TotalMilliseconds < 1000.0)    // if the proposedTime is not significantly different that the scheduledTime
                        doSetTimer = false;
                }
                if (doSetTimer)
                {
                    p_trigger.NextScheduleTimeUtc = proposedTime;

                    StrongAssert.True((DateTime)p_trigger.NextScheduleTimeUtc > DateTime.UtcNow, Severity.ThrowException, "nextScheduleTimeUtc > DateTime.UtcNow");
                    p_trigger.Timer.Change((DateTime)p_trigger.NextScheduleTimeUtc - DateTime.UtcNow, TimeSpan.FromMilliseconds(-1.0));
                }
            }
            // Warn() temporarily to show it on Console
            Utils.Logger.Warn("ScheduleTrigger() '" + p_trigger.BrokerTaskSchema.Name + "': " + ((p_trigger.NextScheduleTimeUtc != null) ? p_trigger.NextScheduleTimeUtc.ToString() : "null"));
        }

        private DateTime? CalcNextTriggerTime(Trigger p_trigger, bool p_isMarketTradingDay, DateTime p_marketOpenTimeUtc, DateTime p_marketCloseTimeUtc)
        {
            if (!p_isMarketTradingDay)  // in this case market open and close times are not given
                return null;

            // if it is scheduled 5 seconds from now, just forget it (1 seconds was not enough)
            // once the timer was set to ellapse at 20:30:00, but it ellapsed at 20:29:58sec.5, so the trade was scheduled again, because it was later than 1 sec
            DateTime tresholdNowTime = DateTime.UtcNow.AddSeconds(5);

            if (p_trigger.StartTimeBase == StartTimeBase.BaseOnUsaMarketOpen)
            {
                DateTime proposedTime = p_marketOpenTimeUtc + p_trigger.StartTimeOffset;
                if (proposedTime > tresholdNowTime)
                    return proposedTime;
            }

            if (p_trigger.StartTimeBase == StartTimeBase.BaseOnUsaMarketClose)
            {
                DateTime proposedTime = p_marketCloseTimeUtc + p_trigger.StartTimeOffset;
                if (proposedTime > tresholdNowTime)
                    return proposedTime;
            }
            return null;
        }

        internal void Exit()
        {
            
        }
    }
}