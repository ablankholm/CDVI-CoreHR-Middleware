using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lyca2CoreHrApiTask.Models;
using Newtonsoft.Json;
using NLog;
using System.Configuration;
using System.IO;

namespace Lyca2CoreHrApiTask.DAL
{
    public class IntegrationManager
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static string appPath = string.Empty;
        private Configuration configuration = ConfigurationManager.OpenExeConfiguration("/");
        private ApplicationState state = new ApplicationState();
        private static CDVIRepository CDVI = new CDVIRepository();
        private static CoreHrApi COREHR = new CoreHrApi();



        public IntegrationManager(string appStartupPath)
        {
            appPath = appStartupPath;
        }



        public void StartOrResume()
        {
            //Retrieve state from previous runs (or default state if running for the first time)
            LoadState();

            //@TODO: Handle first time run and resumption from previous runs

            //Handle shutdown before exiting main
            Stop();
        }



        private void Stop()
        {
            SaveState();
        }



        private void LoadState()
        {
            ApplicationState applicationState = JsonConvert.DeserializeObject<ApplicationState>(File.ReadAllText(appPath + @"\App_Data\state.txt"));
        }



        private void SaveState()
        {
            string json = JsonConvert.SerializeObject(state, Formatting.Indented);
            System.IO.File.WriteAllText(appPath + @"\App_Data\state.txt", json);
        }



        //@TempTesting (refactor out to a dedicated testing module)
        public static void Test(bool silent)
        {
            List<Event> el = new List<Event>();
            Event e = new Event();
            List<int> eventIDs = new List<int> { 9999, 12999 };
            int eventID = 1458000;
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

            /**
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
            **/


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
