﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPoco;
using NLog;
using Newtonsoft.Json;
using Lyca2CoreHrApiTask.Models;
using System.IO;
using System.Data.Common;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Configuration;
using Lyca2CoreHrApiTask.DAL;
using Microsoft.Extensions.CommandLineUtils;
using System.Threading;
using System.Net.Http;
using ServiceStack.Text;
using ServiceStack;

namespace Lyca2CoreHrApiTask
{
    class Program
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static string appPath = Application.StartupPath;
        private ApplicationState state = new ApplicationState();
        private CDVIRepository CDVI = new CDVIRepository();

        static int Main(string[] args)
        {
            //Capture unhandled exceptions
            Application.ThreadException += new ThreadExceptionEventHandler(OnUnhandledThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);

            Program app = new Program();

            //Wrap specific exceptions
            try
            {
                var CLI = new CommandLineApplication
                {
                    Name = "Lyca2CoreHrApiTask",
                    Description = "Middleware that pulls together access control data from legacy systems and posts it to the CoreHR clocking API",
                    FullName = "Lyca to CoreHR Clocking API Automated Middleware"
                };



                CLI.OnExecute(() =>
                {
                    //@TODO: Default behaviour
                    return (int)ExitCode.Success;
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
                                case "TestDBWithLocalWrite":
                                    app.TestDBWithLocalWrite(silentTesting);
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
                                default:
                                    app.Test(silentTesting);
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
                log.Error($"Encountered exception: {ex.ToString()}.");
            }

            //If we haven't returned by this point, something went wrong: exit
            log.Error($"Exiting with code {ExitCode.GenericFailure} ({ExitCode.GenericFailure.ToString()})");
            return (int)ExitCode.GenericFailure;
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
        void Test(bool silent)
        {
            List<ClockingEvent> el = new List<ClockingEvent>();
            ClockingEvent e = new ClockingEvent();
            List<int> eventIDs = new List<int> { 9999, 12999 };
            int eventID = 1434381;
            DateTime today = DateTime.Today;
            DateTime from = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, 0);
            DateTime to = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59, 999);
            List<int> accessEventTypes = new List<int>() { 1280, 1288, 1313 };
            List<int> userIDs = new List<int>() { 176, 138 };


            //Test repository methods
            el = CDVI.GetEvents(eventIDs);
            e = el.FirstOrDefault();
            log.Debug($"Test - Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserNameID: {e.UserNameID}]");
            el.Clear();

            /**
            el = CDVI.GetEvents(eventID, Cardinality.Ascending);
            e = el.FirstOrDefault();
            log.Debug($"Test - Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserNameID: {e.UserNameID}]");
            el.Clear();


            el = CDVI.GetEvents(from, to, accessEventTypes);
            e = el.FirstOrDefault();
            log.Debug($"test - Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserNameID: {e.UserNameID}]");
            el.Clear();
            **/


            el = CDVI.GetEvents(from, to, accessEventTypes, userIDs);
            e = el.FirstOrDefault();
            log.Debug($"Test - Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserNameID: {e.UserNameID}]");
            el.Clear();


            //Test file output
            List<ClockingEvent> events = new List<ClockingEvent>();
            events = CDVI.GetEvents(from, to, accessEventTypes, userIDs);
            e = events.FirstOrDefault();
            string eventOutput = $"Test - UserID: {e.UserNameID.ToString()}" + $" Count: {events.Count.ToString()}" + $" TimeStamp: {e.FieldTime.ToString()}";
            Console.WriteLine(eventOutput);
            log.Info(eventOutput);
            if (silent == false)
            {
                Console.ReadLine();
            }


            string json = JsonConvert.SerializeObject(events.ToArray(), Formatting.Indented);
            System.IO.File.WriteAllText(appPath + @"\App_Data\test.txt", json);
        }

        //@TempTesting (refactor out to a dedicated testing module)
        void TestDB(bool silent)
        {
            List<ClockingEvent> el = new List<ClockingEvent>();
            ClockingEvent e = new ClockingEvent();
            List<int> eventIDs = new List<int> { 9999, 12999 };
            int eventID = 1434381;
            DateTime today = DateTime.Today;
            DateTime from = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, 0);
            DateTime to = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59, 999);
            List<int> accessEventTypes = new List<int>() { 1280, 1288, 1313 };
            List<int> userIDs = new List<int>() { 176, 138 };


            //Test repository methods
            el = CDVI.GetEvents(eventIDs);
            e = el.FirstOrDefault();
            log.Debug($"Test - Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserNameID: {e.UserNameID}]");
            el.Clear();

            /**
            el = CDVI.GetEvents(eventID, Cardinality.Ascending);
            e = el.FirstOrDefault();
            log.Debug($"Test - Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserNameID: {e.UserNameID}]");
            el.Clear();


            el = CDVI.GetEvents(from, to, accessEventTypes);
            e = el.FirstOrDefault();
            log.Debug($"test - Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserNameID: {e.UserNameID}]");
            el.Clear();
            **/


            el = CDVI.GetEvents(from, to, accessEventTypes, userIDs);
            e = el.FirstOrDefault();
            log.Debug($"Test - Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserNameID: {e.UserNameID}]");
            el.Clear();


            if (silent == false)
            {
                Console.ReadLine();
            }
        }

        //@TempTesting (refactor out to a dedicated testing module)
        void TestDBWithLocalWrite(bool silent)
        {
            List<ClockingEvent> el = new List<ClockingEvent>();
            ClockingEvent e = new ClockingEvent();
            List<int> eventIDs = new List<int> { 9999, 12999 };
            int eventID = 1434381;
            DateTime today = DateTime.Today;
            DateTime from = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, 0);
            DateTime to = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59, 999);
            List<int> accessEventTypes = new List<int>() { 1280, 1288, 1313 };
            List<int> userIDs = new List<int>() { 176, 138 };

            //Test file output
            List<ClockingEvent> events = new List<ClockingEvent>();
            events = CDVI.GetEvents(from, to, accessEventTypes, userIDs);
            e = events.FirstOrDefault();
            string eventOutput = $"Test - UserID: {e.UserNameID.ToString()}" + $" Count: {events.Count.ToString()}" + $" TimeStamp: {e.FieldTime.ToString()}";
            Console.WriteLine(eventOutput);
            log.Info(eventOutput);
            if (silent == false)
            {
                Console.ReadLine();
            }


            string json = JsonConvert.SerializeObject(events.ToArray(), Formatting.Indented);
            System.IO.File.WriteAllText(appPath + @"\App_Data\test.txt", json);
        }

        //@TempTesting (refactor out to a dedicated testing module)
        void TestAppState(bool silent)
        {
            string pathToStateFile = appPath + @"\App_Data\teststate.txt";
            string json = string.Empty;

            //Test load
            ApplicationState s = new ApplicationState();
            try
            {
                json = File.ReadAllText(pathToStateFile);
                s = JsonConvert.DeserializeObject<ApplicationState>(json);
                log.Debug($"Successfully loaded state from {pathToStateFile}" + Environment.NewLine
                            + $"{nameof(s.ProcessingState.LastSuccessfulRecord)}: {s.ProcessingState.LastSuccessfulRecord.ToString()}; " + Environment.NewLine
                            + $"{nameof(s.ProcessingState.UnsuccessfulRecords)} count: {s.ProcessingState.UnsuccessfulRecords.Count}; " + Environment.NewLine
                            + $"Unsuccessful records: {String.Join(",", s.ProcessingState.UnsuccessfulRecords.Select(x => x.EventID))}");
            }
            catch (Exception)
            {
                //Pass to caller
                throw;
            }


            //Test save
            s.ProcessingState.LastSuccessfulRecord = 1337;
            s.ProcessingState.UnsuccessfulRecords.AddRange(new List<ClockingEvent>() { new ClockingEvent() { EventID = 9001},
                                                                                        new ClockingEvent() { EventID = 9002},
                                                                                        new ClockingEvent() { EventID = 9003}
            });
            json = string.Empty;
            json = JsonConvert.SerializeObject(s, Formatting.Indented);
            log.Debug($"Serializing state ({json}) to {pathToStateFile}");
            File.WriteAllText(pathToStateFile, json);

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
                ClockingPayload cp = new ClockingPayload() { Person = "9001" };
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
    }
}
