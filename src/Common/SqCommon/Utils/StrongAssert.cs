﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SqCommon
{
    public enum Severity
    {
        /// <summary> Debug.Fail() + Logger.Error() (to be sent in email) </summary>
        NoException,
        /// <summary> Debug.Fail() + Logger.Error() + throw exception </summary>
        ThrowException,
        /// <summary> Debug.Fail() + Logger.Error() + freeze (current implementation: throw exception) </summary>
        Freeze,
        /// <summary> Debug.Fail() + Logger.Error() (email immediately) + Environment.Exit() </summary>
        Halt
    }

    public class StrongAssertMessage
    {
        public Severity Severity { get; set; }
        /// <summary> Example: "StrongAssert failed (severity=={0}): {1}" </summary>
        public String Message { get; set; }
        //public StackTrace StackTrace { get; set; }
        public string StackTrace { get; set; }
    }

    // get keywords for method names from xUnit (or nUnit): https://xunit.github.io/docs/comparisons.html
    public class StrongAssert
    {
        public static event Action<StrongAssertMessage> g_strongAssertEvent;
        

        public static void True(bool p_condition, Severity p_severity = Severity.ThrowException)
        {
            if (!p_condition)
                Fail_core(p_severity, null, null);
        }

        /// <summary> Severity: Exception </summary>
        public static void True(bool p_condition, string p_message, params object[] p_args)
        {
            if (!p_condition)
                Fail_core(Severity.ThrowException, p_message, p_args);
        }

        public static void True(bool p_condition, Severity p_severity, string p_message, params object[] p_args)
        {
            if (!p_condition)
                Fail_core(p_severity, p_message, p_args);
        }

        public static void True(bool p_condition, Severity p_severity, Func<string> p_msg)
        {
            if (!p_condition)
                Fail_core(p_severity, p_msg == null ? (string)null : p_msg(), null);
        }

        public static void NotEmpty(string p_str, Severity p_severity, string p_message, params object[] p_args)
        {
            if (String.IsNullOrEmpty(p_str))
                Fail_core(p_severity, p_message, p_args);
        }

        // http://stackoverflow.com/questions/814878/c-sharp-difference-between-and-equals
        // When == is used on an expression of type object, it'll resolve to System.Object.ReferenceEquals.
        // Equals is just a virtual method and behaves as such, so the overridden version will be used(which, for string type compares the contents).
        public static void Equal<T>(T p_obj1, T p_obj2, Severity p_severity, string p_message, params object[] p_args)
        {
            if (!p_obj1.Equals(p_obj2))
                Fail_core(p_severity, p_message, p_args);
        }



        public static void Fail(Severity p_severity = Severity.ThrowException)
        {
            Fail_core(p_severity, null, null);
        }

        /// <summary> Severity: Exception </summary>
        public static void Fail(string p_message, params object[] p_args)
        {
            Fail_core(Severity.ThrowException, p_message, p_args);
        }

        public static void Fail(Severity p_severity, string p_message, params object[] p_args)
        {
            Fail_core(p_severity, p_message, p_args); // this is needed to add +1 level of stack trace
        }

        private static void Fail_core(Severity p_severity, string p_message, object[] p_args)
        {
            const string MSG = "StrongAssert failed (severity=={0})";
            string msg = String.Format(MSG, p_severity)
                + (p_message == null ? null : ": " + Utils.FormatInvCult(p_message, p_args));

            string sTrace = Environment.StackTrace;
            switch (p_severity)
            {
                //case Severity.NoException:        // not sure, it is safer if it is an Error, and HealthMonitor is always warned
                //    Utils.Logger.Warn("*** {1}{0}Stack trace:{0}{2}", Environment.NewLine, msg, sTrace);    // this will not send message to HealthMonitor, only log the Warning
                //    break;
                default:
                    Utils.Logger.Error("*** {1}{0}Stack trace:{0}{2}", Environment.NewLine, msg, sTrace);   // Errors will be sent to HealthMonitor
                    break;
            }
            
            //#if DNXCORE50 || DOTNET5_5
#if DNX451  || NET451
            Debug.Fail(msg);        // this will offer VisualStudio to capture the execution. But on DotNetCore Windows or Linux, it halts further execution, so eventListeners were not called
#endif
            Action<StrongAssertMessage> listeners = g_strongAssertEvent;
            if (listeners != null)
            {
                Utils.Logger.Info("Calling g_strongAssertEvent event... ");
                listeners(new StrongAssertMessage { Severity = p_severity, Message = msg, StackTrace = sTrace });
            }
            switch (p_severity)
            {
                case Severity.NoException:
                    break;
                case Severity.ThrowException:
                default:
                    throw new Exception(msg);
                case Severity.Freeze:
                //ApplicationState.SleepIfNotExiting(Timeout.Infinite); break;
                case Severity.Halt:
                    if (listeners == null)
                    {
                        //Trace.WriteLine(msg);
                        //Trace.Flush();
                    }
                    Environment.Exit(-1);
                    break;
            }
        }

    }
}
