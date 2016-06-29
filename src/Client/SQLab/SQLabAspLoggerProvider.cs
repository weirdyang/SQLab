﻿using System;
using Microsoft.Extensions.Logging;
using SqCommon;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace SQLab
{
    internal class SQLabAspLoggerProvider : ILoggerProvider
    {
        private bool m_disposed = false;

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string p_categoryName)
        {
            Microsoft.Extensions.Logging.ILogger aspLogger = new SQLabCommonAspLogger(p_categoryName);
            return aspLogger;
        }

        //official Disposable pattern. The finalizer should call your dispose method explicitly.
        //Note:!! The finalizer isn't guaranteed to be called if your application hard crashes.
        //Checked: when I shutdown the Webserver, by typing Ctrl-C, saying Initiate Webserver shutdown, this Dispose was called by an External code; maybe Webserver, maybe the GC. 
        // So, it means that at official Webserver shutdown (change of web.config, shutting down AzureVM, etc.), this Dispose is called.
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool p_disposing)
        {
            if (p_disposing)
            {
                // dispose managed resources
                if (!m_disposed)
                {
                    m_disposed = true;
                    //m_nLogFactory.Flush();  // for sending all the Logs to TableStorage or to the logger EmailBufferingWrapper
                    //m_nLogFactory.Dispose();
                }
            }
            // dispose unmanaged resources
        }

        ~SQLabAspLoggerProvider()
        {
            this.Dispose(false);
        }

        private class SQLabCommonAspLogger : Microsoft.Extensions.Logging.ILogger, IDisposable
        {
            private string p_categoryName;
            object m_scope;

            public SQLabCommonAspLogger(string p_categoryName)
            {
                this.p_categoryName = p_categoryName;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                if (state == null)
                {
                    throw new ArgumentNullException(nameof(state));
                }
                m_scope = state;
                return this;
            }

            // Gets a value indicating whether logging is enabled for the specified level.
            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
            {
                switch (logLevel)
                {
                    case LogLevel.Critical: return true;
                    case LogLevel.Error: return true;
                    case LogLevel.Warning: return true;
                    case LogLevel.Information: return true;
                    case LogLevel.Trace: return false;
                    default: return false;
                }
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }
                var message = string.Empty;
                if (formatter != null)
                {
                    message = formatter(state, exception);

                    //if (exception != null)  // formatter function doesn't put the Exception into the message, so add it.
                    //{
                    //    message += Environment.NewLine + exception;
                    //}
                }
                else
                {
                    if (state != null)
                    {
                        message += state;
                    }
                    if (exception != null)
                    {
                        message += Environment.NewLine + exception;
                    }
                }
                if (!string.IsNullOrEmpty(message))
                {
                    switch (logLevel)
                    {
                        case LogLevel.Critical:
                            if (exception == null)
                                Utils.Logger.Fatal(message);
                            else
                                Utils.Logger.Fatal(exception, message);
                            break;
                        case LogLevel.Error:
                            if (exception == null)
                                Utils.Logger.Error(message);
                            else
                                Utils.Logger.Error(exception, message);
                            break;
                        case LogLevel.Warning:
                            if (exception == null)
                                Utils.Logger.Warn(message);
                            else
                                Utils.Logger.Warn(exception, message);
                            break;
                        case LogLevel.Information:
                            Utils.Logger.Info(message);
                            break;
                        case LogLevel.Trace:
                            Utils.Logger.Debug(message);
                            break;
                        default:
                            Utils.Logger.Debug(message);
                            break;
                    }
                    

                    //_traceSource.TraceEvent(GetEventType(logLevel), eventId.Id, message);
                }

                if (exception != null && IsSendableToHealthMonitorForEmailing(exception))
                    HealthMonitorMessage.SendException("Website.C#.AspLogger", exception, HealthMonitorMessageID.ReportErrorFromSQLabWebsite);

            }

            private bool IsSendableToHealthMonitorForEmailing(Exception p_exception)
            {
                // anonymous people sometimes connect and we have SSL or authentication errors
                // also we are not interested in Kestrel Exception. Some of these exceptions are not bugs, but correctSSL or Authentication fails.
                // we only interested in our bugs our Controller C# code
                string fullExceptionStr = p_exception.ToString();
                if (fullExceptionStr.IndexOf("SSL Handshake failed with OpenSSL error") != -1)
                    return false;
                if (fullExceptionStr.IndexOf("ECONNRESET connection reset by peer") != -1)
                    return false;
                if (fullExceptionStr.IndexOf("The handshake failed due to an unexpected packet format") != -1)
                    return false;
                if (fullExceptionStr.IndexOf("ENOTCONN socket is not connected") != -1)
                    return false;
                if (fullExceptionStr.IndexOf("Authentication failed because the remote party has closed the transport stream") != -1)
                    return false;
                if (fullExceptionStr.IndexOf(@"The path in 'value' must start with '/'") != -1)
                    return false;
                if (fullExceptionStr.IndexOf(@"System.Threading.Tasks.TaskCanceledException: The request was aborted") != -1)
                    return false;
                if (fullExceptionStr.IndexOf(@"The decryption operation failed, see inner exception") != -1)
                    return false;

                return true;
            }

            public void Dispose()
            {
            }
        }

    }
}