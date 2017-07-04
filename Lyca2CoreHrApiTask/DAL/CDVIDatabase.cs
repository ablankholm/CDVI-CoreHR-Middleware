using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lyca2CoreHrApiTask.Models;
using System.Data;
using NPoco;
using NLog;
using System.Data.SqlClient;
using System.Configuration;
using System.Linq.Expressions;

namespace Lyca2CoreHrApiTask.DAL
{
    public class CDVIDatabase
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static string connectionString = ConfigurationManager.AppSettings["CDVI:ConnectionString"];



        public static List<Event> GetAccessEvents(DateTime from, DateTime to)
        {
            List<Event> events = new List<Event>();
            string From = from.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string To   = to.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Console.WriteLine($"from : {From} / {from}, to: {To} / {to}");
            log.Info($"from : {From} / {from}, to: {To} / {to}");
            string query = $"SELECT  [Event ID],[Event Type],[Field Time],[Logged Time],[Operator ID],[Card Holder ID],[Record Name ID],[Site Name ID],[UserNameID]"
                + $"  FROM [Centaur3Events].[dbo].[Events]"
                + $"  WHERE ([Field Time] BETWEEN CONVERT(datetime, '{From}') AND CONVERT(datetime, '{To}'))" 
                + $"      AND [Event Type] IN (1280,1288,1313)";
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



        public static List<Event> GetEvents(DateTime from, DateTime to, List<int> eventTypes)
        {
            List<Event> events = new List<Event>();
            string From = from.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string To = to.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string EventTypes = String.Join(",", eventTypes);
            Console.WriteLine($"from : {From} / {from}, to: {To} / {to}");
            log.Info($"from : {From} / {from}, to: {To} / {to}");
            Console.WriteLine($"types: {EventTypes}");
            log.Info($"types: {EventTypes}");
            string query = $"SELECT  [Event ID],[Event Type],[Field Time],[Logged Time],[Operator ID],[Card Holder ID],[Record Name ID],[Site Name ID],[UserNameID]"
                + $"  FROM [Centaur3Events].[dbo].[Events]"
                + $"  WHERE ([Field Time] BETWEEN CONVERT(datetime, '{From}') AND CONVERT(datetime, '{To}'))"
                + $"      AND [Event Type] IN ({EventTypes})";
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



        public static List<Event> GetEventsAfter(DateTime pointInTime, List<int> eventTypes)
        {
            List<Event> events = new List<Event>();
            string PointInTime = pointInTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string EventTypes = String.Join(",", eventTypes);
            string query = $"SELECT  [Event ID],[Event Type],[Field Time],[Logged Time],[Operator ID],[Card Holder ID],[Record Name ID],[Site Name ID],[UserNameID]"
                + $"  FROM [Centaur3Events].[dbo].[Events]"
                + $"  WHERE ([Field Time] > CONVERT(datetime, '{PointInTime}'))"
                + $"      AND [Event Type] IN ({EventTypes})";
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



        public static List<Event> GetEventsAfter(uint eventId, List<int> eventTypes)
        {
            List<Event> events = new List<Event>();
            string EventTypes = String.Join(",", eventTypes);
            string query = $"SELECT  [Event ID],[Event Type],[Field Time],[Logged Time],[Operator ID],[Card Holder ID],[Record Name ID],[Site Name ID],[UserNameID]"
                + $"  FROM [Centaur3Events].[dbo].[Events]"
                + $"  WHERE ([Event ID] > {eventId})"
                + $"      AND [Event Type] IN ({EventTypes})";
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



        public static List<Event> GetEventsAfterInclusive(DateTime pointInTime, List<int> eventTypes)
        {
            List<Event> events = new List<Event>();
            string PointInTime = pointInTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string EventTypes = String.Join(",", eventTypes);
            string query = $"SELECT  [Event ID],[Event Type],[Field Time],[Logged Time],[Operator ID],[Card Holder ID],[Record Name ID],[Site Name ID],[UserNameID]"
                + $"  FROM [Centaur3Events].[dbo].[Events]"
                + $"  WHERE ([Field Time] >= CONVERT(datetime, '{PointInTime}'))"
                + $"      AND [Event Type] IN ({EventTypes})";
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



        public static List<Event> GetEventsAfterInclusive(uint eventId, List<int> eventTypes)
        {
            List<Event> events = new List<Event>();
            string EventTypes = String.Join(",", eventTypes);
            string query = $"SELECT  [Event ID],[Event Type],[Field Time],[Logged Time],[Operator ID],[Card Holder ID],[Record Name ID],[Site Name ID],[UserNameID]"
                + $"  FROM [Centaur3Events].[dbo].[Events]"
                + $"  WHERE ([Event ID] >= {eventId})"
                + $"      AND [Event Type] IN ({EventTypes})";
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



        public static List<Event> GetUserEvents(DateTime from, DateTime to, List<int> eventTypes, List<int> userIDs)
        {
            List<Event> events = new List<Event>();
            string From = from.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string To = to.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string EventTypes = String.Join(",", eventTypes);
            string UserIDs = String.Join(",", userIDs);
            Console.WriteLine($"User id: {UserIDs}");
            log.Info($"User id: {UserIDs}");
            string query = $"SELECT  [Event ID],[Event Type],[Field Time],[Logged Time],[Operator ID],[Card Holder ID],[Record Name ID],[Site Name ID],[UserNameID]"
                + $"  FROM [Centaur3Events].[dbo].[Events]"
                + $"  WHERE ([Field Time] BETWEEN CONVERT(datetime, '{From}') AND CONVERT(datetime, '{To}'))"
                + $"      AND [Event Type] IN ({EventTypes})" 
                + $"      AND [UserNameID] IN ({UserIDs})";
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



        public static List<Event> GetYesterdaysEvents()
        {
            List<Event> events = new List<Event>();
            string query = "SELECT  [Event ID],[Event Type],[Field Time],[Logged Time],[Operator ID],[Card Holder ID],[Record Name ID],[Site Name ID],[UserNameID]"
                            + "  FROM [Centaur3Events].[dbo].[Events]"
                            + "  WHERE CAST([Field Time] as date) = DATEADD(day, -1, convert(date, GETDATE()))";

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
    }
}
