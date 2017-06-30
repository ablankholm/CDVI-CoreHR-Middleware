using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPoco;
using Newtonsoft.Json;
using Lyca2CoreHrApiTask.Models;
using System.IO;
using System.Data.Common;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Configuration;

namespace Lyca2CoreHrApiTask
{
    class Program
    {
        static void Main(string[] args)
        {
            string      connectionString    = ConfigurationManager.AppSettings["CDVI:ConnectionString"];
            string      appPath             = Application.StartupPath;
            List<Event> Events              = new List<Event>();

            //Retrieve configuration
            //@TODO

            //Retrieve data from CDVI DB
            Events = GetYesterdaysEvents(connectionString);

            //Prepare data
            //@TODO

            //Post data to CoreHr API
            //@TODO

            //Conduct testing
            Test(appPath, Events);

        }



        //@Testing
        static void Test(string path, List<Event> events)
        {
            string eventOutput = $"UserID: {events.FirstOrDefault().UserNameID.ToString()}" + $"Count: {events.Count.ToString()}";
            Console.WriteLine(eventOutput);
            Console.ReadLine();

            string json = JsonConvert.SerializeObject(events.ToArray(), Formatting.Indented);

            System.IO.File.WriteAllText(path + @"\test.txt", json);
        }



        static List<Event> GetAccessEvents(DateTime from, DateTime to)
        {
            List<Event> events = new List<Event>();
            //@TODO:
            return events;
        }



        static List<Event> GetYesterdaysEvents(string connectionString)
        {
            string query = @"SELECT  [Event ID],[Event Type],[Field Time],[Logged Time],[Operator ID],[Card Holder ID],[Record Name ID],[Site Name ID],[UserNameID]"
                            + @"  FROM [Centaur3Events].[dbo].[Events]"
                            + @"  WHERE CAST([Field Time] as date) = DATEADD(day, -1, convert(date, GETDATE()))";
            List<Event> events = new List<Event>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (IDatabase db = new Database(conn))
                {
                    var result = db.Fetch<Event>(query);
                    events = result;
                }
                conn.Close();
            }
            return events;
        }



        public void PostClockingData()
        {
            //@TODO
        }
    }
}
