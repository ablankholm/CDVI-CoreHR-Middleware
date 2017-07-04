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
        private static CDVIDatabase db = new CDVIDatabase();

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
                Test();
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

            //Iterative testing during dev

            app.Command("RunDevTest", c => {

                c.Description = "Runs the currently configured test method for iterative development";

                c.HelpOption("-?|-h|--help");

                c.OnExecute(() => {

                    Test();

                    return 0;
                });

            });

            return app.Execute(args);
        }



        //@TempTesting
        static void Test()
        {
            List<Event> events = new List<Event>();
            DateTime today = DateTime.Today;
            DateTime from = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, 0);
            DateTime to = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59, 999);
            List<int> accessEventTypes = new List<int>() { 1280, 1288, 1313 };
            List<int> userIDs = new List<int>() { 176, 138 };
            events = CDVIDatabase.GetUserEvents(from, to, accessEventTypes, userIDs);

            Event t = events.FirstOrDefault();
            string eventOutput = $"UserID: {t.UserNameID.ToString()}" + $" Count: {events.Count.ToString()}" + $" TimeStamp: {t.FieldTime.ToString()}";
            Console.WriteLine(eventOutput);
            log.Info(eventOutput);
            Console.ReadLine();

            string json = JsonConvert.SerializeObject(events.ToArray(), Formatting.Indented);

            System.IO.File.WriteAllText(appPath + @"\App_Data\test.txt", json);
        }



        public void PostClockingData()
        {
            //@TODO
        }
    }
}
