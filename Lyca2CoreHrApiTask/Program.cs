using System;
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

namespace Lyca2CoreHrApiTask
{
    class Program
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static string appPath = Application.StartupPath;
        private static CDVIRepository CDVI = new CDVIRepository();

        static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "Lyca2CoreHrApiTask",
                Description = "Middleware that pulls together access control data from legacy systems and posts it to the CoreHR clocking API",
                FullName = "Lyca to CoreHR Clocking API Automated Middleware"
            };



            app.OnExecute(() => 
            {
                app.ShowHelp();
                Test(false);
                return 2;
            });



            //Retrieve configuration
            //@TODO

            //Retrieve data from CDVI DB
            //@TODO

            //Prepare data
            //@TODO

            //Post data to CoreHr API
            //@TODO



            app.Command("RunDevTest", c => {
                c.Description = "Runs the currently configured test method for iterative development";
                c.HelpOption("-?|-h|--help");

                var silentOption = c.Option("--silent", "Run without asking for user imput.", CommandOptionType.NoValue);

                c.OnExecute(() => {
                    log.Info($"Executing RunDevTest at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                    bool silentTesting = false;

                    if (silentOption.HasValue())
                    {
                        silentTesting = true;
                    }
                    Test(silentTesting);

                    return 0;
                });
            });




            return app.Execute(args);
        }



        public void PostClockingData()
        {
            //@TODO
        }



        //@TempTesting (refactor out to a dedicated testing module)
        static void Test(bool silent)
        {
            List<Event> el = new List<Event>();
            Event e = new Event();
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
            log.Debug($"Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserNameID: {e.UserNameID}]");
            el.Clear();


            el = CDVI.GetEvents(eventID, Cardinality.Ascending);
            e = el.FirstOrDefault();
            log.Debug($"Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserNameID: {e.UserNameID}]");
            el.Clear();


            el = CDVI.GetEvents(from, to, accessEventTypes);
            e = el.FirstOrDefault();
            log.Debug($"Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserNameID: {e.UserNameID}]");
            el.Clear();


            el = CDVI.GetEvents(from, to, accessEventTypes, userIDs);
            e = el.FirstOrDefault();
            log.Debug($"Count: {el.Count}, First Record[ EventID: {e.EventID}, EventType: {e.EventType}, FieldTime: {e.FieldTime}, UserNameID: {e.UserNameID}]");
            el.Clear();


            //Test file output
            List<Event> events = new List<Event>();
            events = CDVI.GetEvents(from, to, accessEventTypes, userIDs);
            e = events.FirstOrDefault();
            string eventOutput = $"UserID: {e.UserNameID.ToString()}" + $" Count: {events.Count.ToString()}" + $" TimeStamp: {e.FieldTime.ToString()}";
            Console.WriteLine(eventOutput);
            log.Info(eventOutput);
            if (silent == false)
            {
                Console.ReadLine();
            }


            string json = JsonConvert.SerializeObject(events.ToArray(), Formatting.Indented);
            System.IO.File.WriteAllText(appPath + @"\App_Data\test.txt", json);
        }
    }
}
