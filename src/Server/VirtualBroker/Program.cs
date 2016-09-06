﻿using System;
using RxCommon;
using System.Threading;
using SqCommon;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string runtimeConfig = "Unknown";
#if RELEASE
            runtimeConfig = "RELEASE";
# elif DEBUG
            runtimeConfig = "DEBUG";
#endif
            Console.WriteLine($"Hello VirtualBroker, v1.0.13 ({runtimeConfig}, ThId-{Thread.CurrentThread.ManagedThreadId})");
            Console.Title = "VirtualBroker v1.0.12";
            if (!RxUtils.InitDefaultLogger(typeof(Program).Namespace))
                return; // if we cannot create logger, terminate app
            Utils.Logger.Info($"****** Main() START ({runtimeConfig}, ThId-{Thread.CurrentThread.ManagedThreadId})");

            if (!Controller.IsRunningAsLocalDevelopment())
            {
                // Should we start VBroker at the weekend? Decision: Yes.  (because we need VBroker Realtime data on the weekend or we may want to trade Futures that are traded on the weekend)
                // Against it:                 // if it is Saturday, Sunday, we can think about not starting VBroker, because often IB makes maintenance on the weekends and IBGateway cannot connect, therefore VBroker will raise an error anyway.
                // However, if it is true that IB gives many false maintenance errors, we should find a solution there. Maybe report error only if it is weekdays, not weekends
                // however, don't consider USA holidays. It is better to start VBroker, because it may not be a UK, German, French holiday, so maybe a DAX trader would like to trade on that day   
                //DateTime dateNowInET = Utils.ConvertTimeFromUtcToEt(DateTime.UtcNow);
                //if (dateNowInET.DayOfWeek == DayOfWeek.Saturday || dateNowInET.DayOfWeek == DayOfWeek.Sunday)
                //{
                //    Utils.Logger.Info($"Assuming morning restart of VBroker every day!!!, if today is the weekend, there is no reason to start. Ending execution now.");
                //    return;
                //}
            }

            Utils.Configuration = Utils.InitConfigurationAndInitUtils("g:/agy/Google Drive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.VirtualBroker.NoGitHub.json", "/home/ubuntu/SQ/Server/VirtualBroker/SQLab.VirtualBroker.NoGitHub.json");
            Utils.MainThreadIsExiting = new ManualResetEventSlim(false);
            HealthMonitorMessage.InitGlobals(ServerIp.HealthMonitorPublicIp, HealthMonitorMessage.DefaultHealthMonitorServerPort);       // until HealthMonitor runs on the same Server, "localhost" is OK
            StrongAssert.g_strongAssertEvent += StrongAssertMessageSendingEventHandler;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException; // Occurs when a faulted task's unobserved exception is about to trigger exception which, by default, would terminate the process.

            try
            {
                Controller.g_controller.Init();

                string userInput = String.Empty;
                do
                {

                    userInput = DisplayMenu();
                    switch (userInput)
                    {
                        case "1":
                            Console.WriteLine("Hello. I am not crashed yet! :)");
                            Utils.Logger.Info("Hello. I am not crashed yet! :)");
                            break;
                        case "2":
                            Controller.g_controller.TestVbGatewayConnection();
                            break;
                        case "3":
                            Controller.g_controller.TestHealthMonitorListenerBySendingErrorFromVirtualBroker();
                            break;
                        case "4":
                            //Controller.g_controller.TestHardCrash();
                            Controller.g_controller.TestRealtimePriceService();
                            break;
                        case "5":
                            //Controller.g_controller.TestHardCrash();
                            Controller.g_controller.EncogXORHelloWorld();
                            break;
                        case "6":
                            Controller.g_controller.TestElapseFirstTriggerWithSimulation(0);
                            break;
                        case "7":
                            Controller.g_controller.TestElapseFirstTriggerWithSimulation(1);
                            break;
                    }

                } while (userInput != "8" && userInput != "ConsoleIsForcedToShutDown");

                Utils.Logger.Info("****** Main() END");
                Utils.MainThreadIsExiting.Set();
                Controller.g_controller.Exit();
                Utils.Logger.Exit();
            }
            catch (Exception e)
            {
                HealthMonitorMessage.SendException("VBroker Main Thread", e, HealthMonitorMessageID.ReportErrorFromVirtualBroker);
            }

        }

        // Occurs when a faulted task's unobserved exception is about to trigger exception which, by default, would terminate the process.
        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HealthMonitorMessage.SendException("VBroker TaskScheduler_UnobservedTaskException", e.Exception, HealthMonitorMessageID.ReportErrorFromVirtualBroker);
        }

        //If there is a Crash or Error, Catch and hadle it. AggregateIBGateway should Report it. HealthMonitor should know about it,
        //so send a message to HealthMonitor2, that calls my Phone and sends email.
        // "I shouldn't receive 20 email per day about 'Vbroker X was OK'; but I should receive 1 email only if there is a problem."
        internal static void StrongAssertMessageSendingEventHandler(StrongAssertMessage p_msg)
        {
            Utils.Logger.Info("StrongAssertEmailSendingEventHandler()");
            HealthMonitorMessage.SendStrongAssert("VirtualBroker", p_msg, HealthMonitorMessageID.ReportErrorFromSQLabWebsite);
        }


        static bool gHasBeenCalled = false;
        static public string DisplayMenu()
        {
            if (gHasBeenCalled)
            {
                Console.WriteLine();
            }
            gHasBeenCalled = true;
            
            Utils.ConsoleWriteLine(ConsoleColor.Magenta, "------- VirtualBroker (type and press Enter) ------- ");
            Console.WriteLine("1. Say Hello. Don't do anything. Check responsivenes");
            Console.WriteLine("2. Test IbGateway Connection");
            Console.WriteLine("3. Test HealthMonitor by sending ErrorFromVirtualBroker");
            Console.WriteLine("4. Test Realtime price service");
            Console.WriteLine("5. Test Encog");
            Console.WriteLine("6. Elapse first TaskShema (UberVxx) First Simulation Trigger");
            Console.WriteLine("7. Elapse second TaskShema (NeuralSniffer1) First Simulation Trigger");
            Console.WriteLine("8. Exit gracefully (Avoid Ctrl-^C).");
            string result = null;
            try
            {
                result = Console.ReadLine();
            }
            catch (Exception e) // on Linux, if somebody closed the Terminal Window, Console.Readline() will throw an Exception with Message "Input/output error"
            {
                Utils.Logger.Info($"Console.ReadLine() Exception. Somebody closed the Terminal Window. Exception message: {e.Message}");
                return "ConsoleIsForcedToShutDown";
            }
            return result;
            //return Convert.ToInt32(result);
        }
    }
}
