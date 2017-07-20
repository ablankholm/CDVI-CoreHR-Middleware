﻿using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Newtonsoft.Json;
using Lyca2CoreHrApiTask.Models;
using System.IO;
using System.Windows.Forms;
using Lyca2CoreHrApiTask.DAL;
using Microsoft.Extensions.CommandLineUtils;
using System.Threading;
using ServiceStack;
using Polly;
using Polly.Retry;
using Polly.Registry;
using Lyca2CoreHrApiTask.Resilience;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using RestSharp;
using System.Net;

namespace Lyca2CoreHrApiTask
{
    class Program
    {
        private static Logger       log         = LogManager.GetCurrentClassLogger();
        private static string       appPath     = Application.StartupPath;
        private ApplicationState    state       = new ApplicationState();
        private LycaPolicyRegistry  policies    = new LycaPolicyRegistry();
        private CDVIRepository      CDVI        = new CDVIRepository();
        private CoreHrApi           CoreAPI     = new CoreHrApi();


        public Program()
        {
            CDVI.Policies       = policies;
            CoreAPI.Policies    = policies;
        }


        static int Main(string[] args)
        {
            Application.ThreadException                 += new ThreadExceptionEventHandler(OnUnhandledThreadException);
            AppDomain.CurrentDomain.UnhandledException  += new UnhandledExceptionEventHandler(OnUnhandledException);
            Program app = new Program();
            //ServicePointManager.UseNagleAlgorithm = false; Uncomment if http request performance becoems an issue

            //Wrap specific exceptions
            try
            {
                //Setup CLI
                var CLI = new CommandLineApplication
                {
                    Name = "Lyca2CoreHrApiTask",
                    Description = "Middleware that pulls together access control data from legacy systems and posts it to the CoreHR clocking API",
                    FullName = "Lyca to CoreHR Clocking API Automated Middleware"
                };



                CLI.OnExecute(() =>
                {
                    app.StartOrResume();

                    //If we got this far, something went wrong -> Exit
                    log.Error($"Exiting with code {ExitCode.StarOrResumeFailed} ({ExitCode.StarOrResumeFailed.ToString()})");
                    return (int)ExitCode.StarOrResumeFailed;
                });



                CLI.Command("RunDevTest", c =>
                {
                    c.Description = "Runs the currently configured test method for iterative development";
                    c.HelpOption("-?|-h|--help");

                    var silentOption = c.Option("--silent", "Run without asking for user imput.", CommandOptionType.NoValue);

                    var testName = c.Option("-n|--name", "Specify the test to run by its name", CommandOptionType.SingleValue);

                    c.OnExecute(() =>
                    {
                        log.Info($"Executing RunDevTest at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                        bool silentTesting = false;

                        if (silentOption.HasValue())
                        {
                            silentTesting = true;
                        }

                        if (testName.HasValue())
                        {
                            switch (testName.Value())
                            {
                                case "TestDB":
                                    app.TestDB(silentTesting);
                                    break;
                                case "TestAppState":
                                    app.TestAppState(silentTesting);
                                    break;
                                case "TestHttpGet":
                                    app.TestHttpGet(silentTesting);
                                    break;
                                case "TestHttpPost":
                                    app.TestHttpPost(silentTesting);
                                    break;
                                case "TestApiAuthentication":
                                    app.TestApiAuthentication(silentTesting);
                                    break;
                                case "TestApiPost":
                                    app.TestApiPost(silentTesting);
                                    break;
                                default:
                                    LogExit(ExitCode.InvalidTestName, Models.LogLevel.Debug);
                                    break;
                            }
                        }

                        return (int)ExitCode.Success;
                    });
                });



                return CLI.Execute(args);
            }
            //Handle specific exceptions
            catch (Exception ex)
            {
                //Any future handling of specific exceptions goes here
                //...

                //If no specific exception handling, handle generically
                log.Error($"Encountered exception: {ex.ToString()}.");
                return LogExit(ExitCode.ExceptionEncountered, Models.LogLevel.Error);
            }

            //This point should be unreachable - leaving this in as a safety net
            //in the event that future refactoring fails to take this into account:
            //If we haven't returned by this point, something went wrong -> Exit
            return LogExit(ExitCode.GenericFailure, Models.LogLevel.Error);
        }



        private void StartOrResume()
        {
            log.Info($"Starting / resuming...");
            try
            {
                //Restore from previous runs
                LoadState(appPath + @"\App_Data\state.txt");
                //Add new records to any records left over from previous runs
                List<int> eventTypes = new List<int> { 1280, 1288, 1313 };
                state.ProcessingState.PendingRecords.AddRange(
                    CDVI.GetEvents(
                        state.ProcessingState.LastSuccessfulRecord, 
                        eventTypes));
                //Post all pending records
                CoreAPI.PostClockingRecordBatch(ref state.ProcessingState.PendingRecords, ref state.ProcessingState.LastSuccessfulRecord);
            }
            catch (Exception ex)
            {
                log.Error($"Failed to start / resume (exception encountered: {ex}).");
                Exit(ExitCode.StarOrResumeFailed, Models.LogLevel.Error);
            }
            finally
            {
                //Make things ready for next run or recovery
                CleanupAndExit();
            }
        }

        private void CleanupAndExit()
        {
            log.Info($"Performing cleanup...");
            try
            {
                SaveState(appPath + @"\App_Data\state.txt");

                //All is well -> Exit
                Exit(ExitCode.Success);
            }
            catch (Exception ex)
            {
                log.Error($"Failed to perform cleanup (exception encountered: {ex}).");
                //If we reached this point, exit without cleanup
                Exit(ExitCode.CleanupAndExitFailed, Models.LogLevel.Error);
            }
        }

        private void LoadState(string path)
        {
            log.Info($"Loading state...");
            try
            {
                policies.Get<RetryPolicy>("stateSerializationPolicy").Execute(() =>
                    {
                        state = JsonConvert.DeserializeObject<ApplicationState>(File.ReadAllText(path));
                    }
                );
            }
            catch (Exception ex)
            {
                log.Error($"Failed to load state (exception encountered: {ex}).");
                throw;
            }
            log.Info($"State loaded.");
        }

        private void SaveState(string path)
        {
            log.Info($"Saving state...");
            try
            {
                policies.Get<RetryPolicy>("stateSerializationPolicy").Execute(
                    () =>
                    {
                        File.WriteAllText(path, JsonConvert.SerializeObject(state, Formatting.Indented));
                    }
                );
            }
            catch (Exception ex)
            {
                log.Error($"Failed to save state (exception encountered: {ex}).");
                throw;
            }
            log.Info($"State saved.");
        }

        //Convenience function for structured shutdown outside of main
        private void Exit(  ExitCode exitCode, 
                            Models.LogLevel logLevel    = Models.LogLevel.Info, 
                            string logMessage           = "")
        {
            string message = $"Exiting with code {(int)exitCode} ({exitCode.ToString()})";
            if (!(logMessage == ""))
            {
                message += ": " + logMessage;
            }

            switch (logLevel)
            {
                case Models.LogLevel.Off:
                    break;
                case Models.LogLevel.Trace:
                    log.Trace(message);
                    break;
                case Models.LogLevel.Debug:
                    log.Debug(message);
                    break;
                case Models.LogLevel.Info:
                    log.Info(message);
                    break;
                case Models.LogLevel.Warn:
                    log.Warn(message);
                    break;
                case Models.LogLevel.Error:
                    log.Error(message);
                    break;
                case Models.LogLevel.Fatal:
                    log.Fatal(message);
                    break;
                default:
                    log.Info(message);
                    break;
            }
            Environment.Exit((int)exitCode);
        }

        //Convenience function for keeping return calls and associated logging dry
        private static int LogExit(ExitCode exitCode, Models.LogLevel logLevel)
        {
            string message = $"Exiting with code {(int)exitCode} ({exitCode.ToString()})";

            switch (logLevel)
            {
                case Models.LogLevel.Off:
                    break;
                case Models.LogLevel.Trace:
                    log.Trace(message);
                    break;
                case Models.LogLevel.Debug:
                    log.Debug(message);
                    break;
                case Models.LogLevel.Info:
                    log.Info(message);
                    break;
                case Models.LogLevel.Warn:
                    log.Warn(message);
                    break;
                case Models.LogLevel.Error:
                    log.Error(message);
                    break;
                case Models.LogLevel.Fatal:
                    log.Fatal(message);
                    break;
                default:
                    log.Info(message);
                    break;
            }
            return((int)exitCode);
        }

        //Catch-all for unhandled exceptions
        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ExitCode exitCode = ExitCode.UnhandledException;
            log.Error($"Encountered unhandled exception: {e.ExceptionObject.ToString()}. Exiting with code {exitCode} ({exitCode.ToString()})...");
            Environment.Exit((int)exitCode);
        }

        //Catch-all for unhandled thread exceptions
        static void OnUnhandledThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ExitCode exitCode = ExitCode.UnhandledThreadException;
            log.Error($"Encountered unhandled thread exception: {e.Exception.ToString()}. Exiting with code {exitCode} ({exitCode.ToString()})...");
            Environment.Exit((int)exitCode);
        }


        //@TempTesting (refactor out to a dedicated testing module)
        void TestDB(bool silent)
        {
            List<ClockingEvent> el = new List<ClockingEvent>();
            ClockingEvent e = new ClockingEvent();
            List<int> eventIDs = new List<int> { 996868, 2998 };
            int eventID = 1434381;
            DateTime today = DateTime.Today;
            DateTime from = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, 0);
            DateTime to = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59, 999);
            List<int> accessEventTypes = new List<int>() { 1280, 1288, 1313 };
            List<int> userIDs = new List<int>() { 1192 };


            //Test repository methods
            el = CDVI.GetEvents(eventIDs);
            e = el.FirstOrDefault();
            log.Debug($"Test - Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserID: {e.UserID} ]");
            el.Clear();

            el = CDVI.GetEventsByUser(userIDs.First(), accessEventTypes);
            e = el.FirstOrDefault();
            log.Debug($"Test - Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserID: {e.UserID} ]");
            el.Clear();

            el = CDVI.GetEventsByDate(from, accessEventTypes);
            e = el.FirstOrDefault();
            log.Debug($"Test - Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserID: {e.UserID} ]");
            el.Clear();


            if (silent == false)
            {
                Console.ReadLine();
            }
        }

        //@TempTesting (refactor out to a dedicated testing module)
        void TestAppState(bool silent)
        {
            string pathToStateFile = appPath + @"\App_Data\teststate.txt";
            string json = string.Empty;

            //Test load
            LoadState(pathToStateFile);
            ApplicationState s = new ApplicationState();
            s = state;
            log.Debug($"Successfully loaded state from {pathToStateFile}" + Environment.NewLine
                        + $"{nameof(s.ProcessingState.LastSuccessfulRecord)}: {s.ProcessingState.LastSuccessfulRecord.ToString()}; " + Environment.NewLine
                        + $"{nameof(s.ProcessingState.PendingRecords)} count: {s.ProcessingState.PendingRecords.Count}; " + Environment.NewLine
                        + $"Unsuccessful records: {String.Join(",", s.ProcessingState.PendingRecords.Select(x => x.EventID))}");

            //Test save
            s.ProcessingState.LastSuccessfulRecord = 1337;
            s.ProcessingState.PendingRecords.AddRange(new List<ClockingEvent>() { new ClockingEvent() { EventID = 9001},
                                                                                    new ClockingEvent() { EventID = 9002},
                                                                                    new ClockingEvent() { EventID = 9003}
            });
            state = s;
            SaveState(pathToStateFile);

            //Note policy
            Policy p = policies.Get<RetryPolicy>("stateSerializationPolicy");
            log.Debug($"State serialization policy held in registry = {p.PolicyKey}; Policies in registry = {policies.Count}");

            if (silent == false)
            {
                Console.ReadLine();
            }
        }

        //@TempTesting (refactor out to a dedicated testing module)
        void TestHttpGet(bool silent)
        {
            string result = @"http://httpbin.org/get".GetStringFromUrl();
            log.Debug($"TestHttpGet: Response = {result}");

            if (silent == false)
            {
                Console.ReadLine();
            }
        }

        //@TempTesting (refactor out to a dedicated testing module)
        void TestHttpPost(bool silent)
        {
            try
            {
                ClockingEvent cp = new ClockingEvent() { UserID = 9001 };
                string payload = JsonConvert.SerializeObject(cp);
                string result = @"http://httpbin.org/post".PostJsonToUrl(payload);
                log.Debug($"TestHttpPost: Response = {result}");
            }
            catch (Exception ex)
            {
                log.Debug($"TestHttpPost encountered an exception: {ex.ToString()}");
                if (silent == false)
                {
                    Console.ReadLine();
                }
                throw;
            }
            if (silent == false)
            {
                Console.ReadLine();
            }
        }

        //@TempTesting (refactor out to a dedicated testing module)
        async void TestApiAuthentication(bool silent)
        {
            try
            {
                log.Debug($"TestApiAuthentication - API Response: {JsonConvert.SerializeObject(CoreAPI.Authenticate().Token, Formatting.Indented)}");
            }
            catch (Exception ex)
            {
                log.Debug($"TestApiAuthentication encountered an exception: {ex.ToString()}");
                if (silent == false)
                {
                    Console.ReadLine();
                }
                throw;
            }
            if (silent == false)
            {
                Console.ReadLine();
            }
        }

        //@TempTesting (refactor out to a dedicated testing module)
        async void TestApiPost(bool silent)
        {
            try
            {
                ClockingEvent ce = new ClockingEvent() { UserID = 1192, FieldTime = DateTime.Now };
                string apiResponse = string.Empty;
                apiResponse = JsonConvert.SerializeObject(
                    CoreAPI.PostClockingRecord(CoreAPI.GetClockingPayload(ce), CoreAPI.Authenticate().Token), 
                    Formatting.Indented);
                log.Debug($"TestApiPost - API Response: {apiResponse}");
            }
            catch (Exception ex)
            {
                log.Debug($"TestApiPost encountered an exception: {ex.ToString()}");
                if (silent == false)
                {
                    Console.ReadLine();
                }
                throw;
            }
            if (silent == false)
            {
                Console.ReadLine();
            }
        }
    }
}
