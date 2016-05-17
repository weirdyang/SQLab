﻿//using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Json;
using System.Net.Sockets;
using System.Runtime.Serialization;

namespace SqCommon
{
    public enum Platform
    {
        Windows,
        Linux,
        Mac
    }

    public interface IConfigurationSection : IConfiguration
    {
        string Key { get; }
        string Path { get; }
        string Value { get; set; }
    }

    public interface IConfiguration
    {
        string this[string key] { get; set; }

        IEnumerable<IConfigurationSection> GetChildren();
        IConfigurationSection GetSection(string key);
    }

    public interface IConfigurationRoot : IConfiguration
    {
        void Reload();  // reloading config file Runtime is a good idea
    }

    class ConfigurationRoot : IConfigurationRoot
    {
        Dictionary<string, string> m_configDict;
        public ConfigurationRoot()
        {
            m_configDict = new Dictionary<string, string>();
        }

        public ConfigurationRoot(Dictionary<string, string> p_configDict)
        {
            m_configDict = p_configDict;
        }

        public string this[string key]
        {
            get
            {
                string val;
                if (m_configDict.TryGetValue(key, out val))
                {
                    return val;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                string val;
                if (m_configDict.TryGetValue(key, out val))
                {
                    // yay, value exists!
                    m_configDict[key] = value;
                }
                else
                {
                    // darn, lets add the value
                    m_configDict.Add(key, value);
                }
            }
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            throw new NotImplementedException();
        }

        public IConfigurationSection GetSection(string key)
        {
            throw new NotImplementedException();
        }

        public void Reload()
        {
            throw new NotImplementedException();
        }
    }



    public static partial class Utils
    {
        public static readonly System.Globalization.CultureInfo InvCult = System.Globalization.CultureInfo.InvariantCulture;

        public static ILogger Logger = null;
        public static IConfigurationRoot Configuration = null;
        public static ManualResetEventSlim MainThreadIsExiting = null;  // some Tools, Apps do not require this, so don't initiate this for them automatically

        public static bool IsShowingDatePart { get; set; } = true;

        // see discussion here in CoreCLR (they are not ready) https://github.com/dotnet/corefx/issues/1017
        public static Platform RunningPlatform()
        {
            switch (Environment.NewLine)
            {
                case "\n":
                    return Platform.Linux;

                case "\r\n":
                    return Platform.Windows;

                default:
                    throw new Exception("RunningPlatform() not recognized");
            }
        }

        //VT100 codes, http://www.cplusplus.com/forum/unices/36461/
        //this Linux handling should be temporary only until it is fixed in DotNetCore in Linux
        // this works too in the Terminal. Type this "printf '\e[38;5;196m Foreground color: red\n'"
        public static string GetLinuxVT100ForeColorCodes(ConsoleColor p_color)
        {
            switch (p_color)
            {
                case ConsoleColor.Black:
                    return (char)27 + "[1;30m";
                case ConsoleColor.White:
                    return (char)27 + "[1;37m";     // Gray

                case ConsoleColor.DarkBlue:
                    return (char)27 + "[2;34m";     // "2" means darker but not bold
                case ConsoleColor.DarkGreen:
                    return (char)27 + "[2;32m";
                case ConsoleColor.DarkCyan:
                    return (char)27 + "[2;36m";
                case ConsoleColor.DarkRed:
                    return (char)27 + "[2;31m";
                case ConsoleColor.DarkMagenta:
                    return (char)27 + "[2;35m";
                case ConsoleColor.DarkYellow:
                    return (char)27 + "[2;33m";
                case ConsoleColor.DarkGray:
                    return (char)27 + "[2;37m";

                case ConsoleColor.Blue:
                    return (char)27 + "[1;34m";     // "1" means bold
                case ConsoleColor.Green:
                    return (char)27 + "[1;32m";
                case ConsoleColor.Cyan:
                    return (char)27 + "[1;36m";
                case ConsoleColor.Red:
                    return (char)27 + "[1;31m";
                case ConsoleColor.Magenta:
                    return (char)27 + "[1;35m";
                case ConsoleColor.Yellow:
                    return (char)27 + "[1;33m";     // this is brown, because there is no Yellow  in Linux VT100
                case ConsoleColor.Gray:
                    return (char)27 + "[1;37m";         
                default:
                    string LinuxDefaultConsoleColor = (char)27 + "[0m";  //VT100 codes, http://www.cplusplus.com/forum/unices/36461/
                    return LinuxDefaultConsoleColor;
            }
        }

        public static Tuple<ConsoleColor?, ConsoleColor?> ConsoleColorBegin(ConsoleColor? p_foregroundColor, ConsoleColor? p_backgroundColor)
        {
            ConsoleColor? previousForeColor = null;
            ConsoleColor? previousBackColor = null;
            if (p_foregroundColor != null) {
                previousForeColor = Console.ForegroundColor;

                if (Utils.RunningPlatform() == Platform.Linux)
                {
                    Console.Write(GetLinuxVT100ForeColorCodes((ConsoleColor)p_foregroundColor));
                }
                else
                {
                    Console.ForegroundColor = (ConsoleColor)p_foregroundColor;
                }
            }

            if (p_backgroundColor != null) {
                if (Utils.RunningPlatform() == Platform.Linux)
                {
                    Utils.Logger.Trace("Linux background colour is not yet implemented. The whole Linux implementation is temporary anyway, until DotNetCore is fixed on Linux.");
                }
                previousBackColor = Console.BackgroundColor;
                Console.BackgroundColor = (ConsoleColor)p_backgroundColor;
            }
            return new Tuple<ConsoleColor?, ConsoleColor?>(previousForeColor, previousBackColor);
        }

        public static void ConsoleColorRestore(Tuple<ConsoleColor?, ConsoleColor?> p_previousColors)
        {
            // Console.ResetColor(); is one option, but it is not that good than going back to previous
            if (p_previousColors.Item1 != null)
            {
                if (Utils.RunningPlatform() == Platform.Linux)
                {
                    Console.Write(GetLinuxVT100ForeColorCodes((ConsoleColor)p_previousColors.Item1));
                }
                else
                {
                    Console.ForegroundColor = (ConsoleColor)p_previousColors.Item1;
                }
            }
            if (p_previousColors.Item2 != null)
                Console.BackgroundColor = (ConsoleColor)p_previousColors.Item2;
        }

        public static void ConsoleWriteLine(ConsoleColor? p_foreColor, string p_value) // static objects like Console cannot have Extensions methods with the 'this' keyword.
        {
            ConsoleWrite(p_foreColor, null, true, p_value);
        }

        public static void ConsoleWriteLine(ConsoleColor? p_foreColor, ConsoleColor? p_backColor, string p_value) // static objects like Console cannot have Extensions methods with the 'this' keyword.
        {
            ConsoleWrite(p_foreColor, p_backColor, true, p_value);
        }

        public static void ConsoleWrite(ConsoleColor? p_foreColor, string p_value) // static objects like Console cannot have Extensions methods with the 'this' keyword.
        {
            ConsoleWrite(p_foreColor, null, false, p_value);
        }

        public static void ConsoleWrite(ConsoleColor? p_foreColor, ConsoleColor? p_backColor, bool p_useWriteLine, string p_value) // static objects like Console cannot have Extensions methods with the 'this' keyword.
        {
            var colors = Utils.ConsoleColorBegin(p_foreColor, p_backColor);
            if (p_useWriteLine)
                Console.WriteLine(p_value);
            else
                Console.Write(p_value);
            Utils.ConsoleColorRestore(colors);
        }

        //public static Platform RunningPlatform()
        //{
        //    if (Environment.NewLine == "\n")
        //        return Platform.Linux;
        //    else
        //        return Platform.Windows;   // "\r\n" for non-Unix platforms

        //switch (Environment.OSVersion.Platform)     // Environment.OSVersion doesn't exist in DotNetCore
        //{
        //    case PlatformID.Unix:
        //        // Well, there are chances MacOSX is reported as Unix instead of MacOSX.
        //        // Instead of platform check, we'll do a feature checks (Mac specific root folders)
        //        if (Directory.Exists("/Applications")
        //            & Directory.Exists("/System")
        //            & Directory.Exists("/Users")
        //            & Directory.Exists("/Volumes"))
        //            return Platform.Mac;
        //        else
        //            return Platform.Linux;

        //    case PlatformID.MacOSX:
        //        return Platform.Mac;

        //    default:
        //        return Platform.Windows;
        //}
        //}

        public static void TcpClientDispose(TcpClient p_tcpClient)
        {
            if (p_tcpClient == null)
                return;
#if DNX451 || NET451
            p_tcpClient.Close();
#else
            p_tcpClient.Dispose();
#endif
        }

        public static bool InitDefaultLogger(string p_filenameWithoutExt)
        {
            try
            {
                var currDir = Directory.GetCurrentDirectory();  // where the project.json is G:\work\Archi-data\GitHubRepos\SQLab\src\Server\HealthMonitor
                var currProject = Path.Combine(currDir, "project.json"); // we can open it and read its contents later
                if (!File.Exists(currProject))
                {
                    Console.WriteLine($"!Error. We assume Current Directory is where project.json is. Cannot find: {currProject}");
                    return false;
                }
                string logDir = Path.Combine(currDir, "..", "..", "..", "logs");
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);
                string logFilePath = Path.Combine(logDir, p_filenameWithoutExt + "." + DateTime.UtcNow.ToString("yyyy-MM-dd") + ".sqlog"); // the extension is *.sqlog so easy to find it in the file system
                Console.WriteLine("Log file: " + logFilePath);
                Logger = new SQLogger(logFilePath);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"!Error. Exception: {e.Message}");
                return false;
            }
        }

        public static string FormatInvCult(this string p_fmt, params object[] p_args)
        {
            if (p_fmt == null || p_args == null || p_args.Length == 0)
                return p_fmt;
            return String.Format(InvCult, p_fmt, p_args);
        }

        /// <summary> Formats the string using InvariantCulture and inserts a 
        /// "time[#threadID]" prefix at the beginning. </summary>
        public static string FormatMessageWithTimestamp(string p_fmt, params object[] p_args)
        {
            return String.Format("{0}#{1:d2} {2}", FormatNow(), Thread.CurrentThread.ManagedThreadId, Utils.FormatInvCult(p_fmt, p_args));
        }
        public static string FormatNow()
        {
            return FormatDateTime(DateTime.UtcNow);
        }
        public static string FormatDateTime(DateTime p_timeUtc)
        {
            return IsShowingDatePart ? String.Format("{1:x}{0:dd}{2}{0:HH':'mm':'ss.fff}", p_timeUtc, p_timeUtc.Month, p_timeUtc.DayOfWeek.ToString().Substring(0, 2))
                : p_timeUtc.ToString("HH\\:mm\\:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary> Tip: use src\Server\Azure\maintenance\viewst.cmd a Tcl/Tk GUI tool to read such gzip+base64 encoded stack traces,
        /// or src/Tools/decodeB64*.cmd is a Powershell command-line tool for the same </summary>
        public static string DbgAbbrev(string p_stackTrace/*, uint p_minLines = 8*/)
        {
            return p_stackTrace;
            //string result = MarkMsgLogged(g_abbreviateStackTraces ? FirstFewLinesThenGz(p_stackTrace, p_minLines, 768) : p_stackTrace, "strace");
            //return String.IsNullOrEmpty(result) ? "(empty)" : result;
        }

        public static bool DownloadStringWithRetry(out string p_webpage, string p_url, int p_nRetry, TimeSpan p_sleepBetweenRetries, bool p_throwExceptionIfUnsuccesfull)
        {
            p_webpage = String.Empty;
            int nDownload = 0;
            do
            {
                try
                {
                    nDownload++;
                    p_webpage = new HttpClient().GetStringAsync(p_url).Result;
                    Utils.Logger.Debug(String.Format("DownloadStringWithRetry()__{0}, nDownload-{1}, Length of reply:{2}", p_url, nDownload, p_webpage.Length));
                    return true;
                }
                catch (Exception ex)
                {
                    // it is quite expected that sometimes (once per month), there is a problem:
                    // "The operation has timed out " or "Unable to connect to the remote server" exceptions
                    // Don't raise Logger.Error() after the first attempt, because it is not really Exceptional, and an Error email will be sent
                    Utils.Logger.Info(ex, "Exception in DownloadStringWithRetry()" + p_url + ":" + nDownload + ": " + ex.Message);
                    Thread.Sleep(p_sleepBetweenRetries);
                    if ((nDownload >= p_nRetry) && p_throwExceptionIfUnsuccesfull)
                        throw;  // if exception still persist after many tries, rethrow it to caller
                }
            } while (nDownload < p_nRetry);

            return false;
        }

        public static T LoadFromJSON<T>(string p_str)
        {
            try
            {
                //don't use FileStream directly to serializer
                //using (FileStream stream = File.OpenRead(filePath))
                // Encountered unexpected character 'ï', because
                // "Please note that DataContractJsonSerializer only supports the following encodings: UTF-8"
                // see http://blogs.msdn.com/b/cie/archive/2014/03/19/encountered-unexpected-character-239-error-serializing-json.aspx

                //string p_str = System.IO.File.ReadAllText(p_filePath);
                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(p_str));
                DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings() { DateTimeFormat = new DateTimeFormat("yyyy-MM-dd'T'HH:mm:ssZ") };    // the 'T' is used by Javascript in HealthMonitor website. 'Z' shows that it is UTC (Zero TimeZone).  That is the reason of the format.
                settings.UseSimpleDictionaryFormat = true;
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T), settings);
                T contract = (T)serializer.ReadObject(ms);
                return contract;
                //}
            }
            catch (System.Exception ex)
            {
                Utils.Logger.Info(ex, "Cannot deserialize json " + p_str);
                throw;
            }
        }

        public static string SaveToJSON<T>(T p_obj)
        {
            try
            {
                DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings() { DateTimeFormat = new DateTimeFormat("yyyy-MM-dd'T'HH:mm:ssZ") };
                settings.UseSimpleDictionaryFormat = true;
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T), settings);

                using (MemoryStream ms = new MemoryStream())
                {
                    serializer.WriteObject(ms, p_obj);
                    ms.Position = 0;
                    StreamReader sr = new StreamReader(ms);
                    return sr.ReadToEnd();

//                    return Encoding.Unicode.GetString(ms.ToArray());  //UTF-16 is used for in-memory strings because it is faster per character to parse and maps directly to unicode character class and other tables. All string functions in Windows use UTF-16 and have for years.
                }
            }
            catch (System.Exception ex)
            {
                Utils.Logger.Info(ex, "Cannot serialize object " + p_obj.ToString());
                throw;
            }
        }

        public static IConfigurationRoot InitConfigurationAndInitUtils(string p_configJsonPathWin, string p_configJsonPathLinux)
        {
            // "Microsoft.Extensions.Configuration": "1.0.0-rc2-16054", is based on ASP.NET. It is updated less frequently, it consumes more memory. 
            // there was a problem that "Microsoft.Extensions.Configuration" uses Newtonsoft.Json 8.0.2, that doesn't support "dotnet55" or "dotnet54", it only supports DNXCORE50
            // so better to stick with a low level JSON solution, which is platform independent in the DotNetCore, which is the System.Runtime.Serialization
            //I would suggest using System.Runtime.Serialization.Json that is part of .NET 4.5.

            Dictionary<string, string> configDict = null;
            if (File.Exists(p_configJsonPathWin))
            {
                configDict = LoadFromJSON<Dictionary<string, string>>(System.IO.File.ReadAllText(p_configJsonPathWin));
            }
            else if (File.Exists(p_configJsonPathLinux))
            {
                configDict = LoadFromJSON<Dictionary<string, string>>(System.IO.File.ReadAllText(p_configJsonPathLinux));
            }
            else
            {
                Utils.Logger.Info("Error! No config files found: " + p_configJsonPathWin + " --- " + p_configJsonPathLinux);
                return null;
            }

            ConfigurationRoot configuration = new ConfigurationRoot();
            // Decode all from Base64 encodings, so later the code don't have to do it all the time
            foreach (var item in configDict)
            {
                configuration[item.Key] = Encoding.UTF8.GetString(Convert.FromBase64String(item.Value));
            }

            //Utils.Logger.Info(@"Test from config.json after decoding, configuration[""EmailGyantal""]: " + configuration["EmailGyantal"]);
            if (String.IsNullOrEmpty(configuration["EmailGyantal"]))
            {
                Utils.Logger.Info("ERROR!!!: Test item from *.json config was not found.");
                Console.WriteLine("ERROR!!!: Test item from *.json config was not found.");
            }

            Email.SenderName = configuration["EmailHQServer"];
            Email.SenderPwd = configuration["EmailHQServerPwd"];

            PhoneCall.PhoneNumbers[Caller.Gyantal] = configuration["PhoneNumberGyantal"];
            PhoneCall.PhoneNumbers[Caller.Robin] = configuration["PhoneNumberRobin"];
            PhoneCall.PhoneNumbers[Caller.RobinLL] = configuration["PhoneNumberRobinLL"];
            PhoneCall.PhoneNumbers[Caller.Charmat0] = configuration["PhoneNumberCharmat0"];
            PhoneCall.TwilioSid = configuration["TwilioSid"];
            PhoneCall.TwilioToken = configuration["TwilioToken"];
            return configuration;

            //// Microsoft.Framework.* has been renamed to Microsoft.Extensions.*
            //var builder = new ConfigurationBuilder()
            //   //.AddJsonFile("appsettings.json")
            //   //.AddJsonFile("../../../SQHealthMonitorNoGitHubConfig.json", optional: true)       // that file will not go to GIT source control
            //   //.AddJsonFile("../SQOvermindNoGitHubConfig.json", optional: true)    // for the Production server
            //   .AddJsonFile(p_configJsonPathLinux, optional: true)    // for the Production server
            //   .AddJsonFile(p_configJsonPathWin, optional: true); // for the development PC
            //// AddEnvironmentVariables();           // adds settings from the Azure WebSite config, needed package : Microsoft.Extensions.Configuration.EnvironmentVariables
            //IConfigurationRoot configuration = builder.Build();
        }
    }
}
