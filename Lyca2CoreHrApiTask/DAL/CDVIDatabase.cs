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



        public enum Cardinality
        {
            Descending = -1,
            None = 0,
            Ascending = 1
        }



        public static List<Event> GetEvents(List<int> eventIDs)
        {
            List<Event> events = new List<Event>();
            string EventIDs = String.Join(",", eventIDs);
            string query = $"SELECT  [Event ID],[Event Type],[Field Time],[Logged Time],[Operator ID],[Card Holder ID],[Record Name ID],[Site Name ID],[UserNameID]"
                + $"  FROM [Centaur3Events].[dbo].[Events]"
                + $"  WHERE [Event ID] IN ({EventIDs})";
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



        public static List<Event> GetEvents(int fromEventId, Cardinality toEndOfTable = Cardinality.Ascending)
        {
            List<Event> events = new List<Event>();
            string opStr = @">=";
            if (toEndOfTable == Cardinality.Descending)
            {
                opStr = @"<=";
            }
            string query = $"SELECT  [Event ID],[Event Type],[Field Time],[Logged Time],[Operator ID],[Card Holder ID],[Record Name ID],[Site Name ID],[UserNameID]"
                            + $"  FROM [Centaur3Events].[dbo].[Events]"
                            + $"  WHERE [Event ID] {opStr} {fromEventId}";
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



        public static List<Event> GetEvents(DateTime from, DateTime to, List<int> eventTypes, List<uint> userIDs)
        {
            List<Event> events = new List<Event>();
            string From = from.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string To = to.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string EventTypes = String.Join(",", eventTypes);
            string UserIDs = String.Join(",", userIDs);
            Console.WriteLine($"from : {From} / {from}, to: {To} / {to}");
            log.Info($"from : {From} / {from}, to: {To} / {to}");
            Console.WriteLine($"types: {EventTypes}");
            log.Info($"types: {EventTypes}");
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



        public static List<Event> GetAccessEvents(DateTime from, DateTime to)
        {
            return GetEvents(from, to, new List<int> { 1280, 1288, 1313 });
        }



        public static List<Event> GetEventsByUsers(DateTime from, DateTime to, List<int> eventTypes, List<uint> userIDs)
        {
            return GetEvents(from, to, eventTypes, userIDs);
        }
    }
}
