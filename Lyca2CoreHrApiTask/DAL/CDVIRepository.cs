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
using Lyca2CoreHrApiTask.Resilience;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Lyca2CoreHrApiTask.DAL
{
    public enum Cardinality
    {
        Descending = -1,
        None = 0,
        Ascending = 1
    }



    public class CDVIRepository
    {
        private static Logger log                   = LogManager.GetCurrentClassLogger();
        private static string connectionString      = ConfigurationManager.AppSettings["CDVI:ConnectionString"];
        private static string eventsBaseQuery = @"SELECT  [Event ID]
                                                            ,[Event Type]
                                                            ,[Field Time]
                                                            ,[Logged Time]
                                                            ,[Operator ID]
                                                            ,[Card Holder ID]
                                                            ,[Record Name ID]
                                                            ,[Site Name ID]
                                                            ,[Centaur3Events].[dbo].[UserNames].[UserID] AS UserNameID 
                                                FROM[Centaur3Events].[dbo].[Events]
                                                JOIN[Centaur3Events].[dbo].[UserNames]
                                                ON[Centaur3Events].[dbo].[Events].[UserNameID] = [Centaur3Events].[dbo].[UserNames].[UserNameID] ";
        public LycaPolicyRegistry Policies { get; set; } = new LycaPolicyRegistry();



        public List<ClockingEvent> GetEvents(List<int> eventIDs)
        {
            try
            {
                List<ClockingEvent> events = new List<ClockingEvent>();
                string EventIDs = String.Join(",", eventIDs);
                string query    = eventsBaseQuery + $"WHERE [Event ID] IN ({EventIDs}) ";



                log.Info($"GetEvents(eventIDs: {EventIDs}) executing query : {query}");
                Policies.Get<Policy>("cdviDbPolicy").Execute(() => 
                {
                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        using (IDatabase db = new Database(conn))
                        {
                            var result = db.Fetch<ClockingEvent>(query);
                            events = result;
                        }
                        conn.Close();
                    }
                });



                return events;
            }
            catch (Exception ex)
            {
                log.Fatal($"Failed to retrieve records from database (exception encountered: {ex}).");
                throw;
            }
        }



        public List<ClockingEvent> GetEvents(int fromEventId)
        {
            try
            {
                List<ClockingEvent> events = new List<ClockingEvent>();
                string query = eventsBaseQuery + $"WHERE [Event ID] > {fromEventId} ";



                log.Info($"GetEvents(fromEventId: {fromEventId}) executing query : {query}");
                Policies.Get<Policy>("cdviDbPolicy").Execute(() =>
                {
                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        using (IDatabase db = new Database(conn))
                        {
                            var result = db.Fetch<ClockingEvent>(query);
                            events = result;
                        }
                        conn.Close();
                    }
                });



                return events;
            }
            catch (Exception ex)
            {
                log.Fatal($"Failed to retrieve records from database (exception encountered: {ex}).");
                throw;
            }
        }



        public List<ClockingEvent> GetEvents(int fromEventId, List<int> eventTypes)
        {
            try
            {
                List<ClockingEvent> events = new List<ClockingEvent>();
                string EventTypes = String.Join(",", eventTypes);
                string query = eventsBaseQuery + $"WHERE [Event ID] > {fromEventId} "
                                                + $"AND [Event Type] IN ({EventTypes}) ";



                log.Info($"GetEvents(fromEventId: {fromEventId}, eventTypes: {EventTypes}) executing query : {query}");
                Policies.Get<Policy>("cdviDbPolicy").Execute(() =>
                {
                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        using (IDatabase db = new Database(conn))
                        {
                            var result = db.Fetch<ClockingEvent>(query);
                            events = result;
                        }
                        conn.Close();
                    }
                });



                return events;
            }
            catch (Exception ex)
            {
                log.Fatal($"Failed to retrieve records from database (exception encountered: {ex}).");
                throw;
            }
        }


        /** Depreciated
        public List<ClockingEvent> GetEvents(DateTime from, DateTime to, List<int> eventTypes)
        {
            try
            {
                List<ClockingEvent> events      = new List<ClockingEvent>();
                string              From        = from.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string              To          = to.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string              EventTypes  = String.Join(",", eventTypes);
                string              query       = eventsBaseQuery
                                                    + $"WHERE ([Field Time] BETWEEN CONVERT(datetime, '{From}') AND CONVERT(datetime, '{To}')) "
                                                    + $"AND [Event Type] IN ({EventTypes}) ";



                log.Info($"GetEvents(from: {From}, to: {To}, eventTypes: {EventTypes}) executing query : {query}");
                Policies.Get<Policy>("cdviDbPolicy").Execute(() => 
                {
                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        using (IDatabase db = new Database(conn))
                        {
                            var result = db.Fetch<ClockingEvent>(query);
                            events = result;
                        }
                        conn.Close();
                    }
                });



                return events;
            }
            catch (Exception ex)
            {
                log.Fatal($"Failed to retrieve records from database (exception encountered: {ex}).");
                throw;
            }
        }

        public List<ClockingEvent> GetEvents(DateTime from, DateTime to, List<int> eventTypes, List<int> userIDs)
        {
            try
            {
                List<ClockingEvent> events      = new List<ClockingEvent>();
                string              From        = from.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string              To          = to.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string              EventTypes  = String.Join(",", eventTypes);
                string              UserIDs     = String.Join(",", userIDs);
                string              query       = eventsBaseQuery
                                                    + $"WHERE ([Field Time] BETWEEN CONVERT(datetime, '{From}') AND CONVERT(datetime, '{To}')) "
                                                    + $"AND [Event Type] IN ({EventTypes}) "
                                                    + $"AND [UserNameID] IN ({UserIDs}) ";



                log.Info($"GetEvents(from: {From}, to: {To}, eventTypes: {EventTypes}, userIDs: {UserIDs}) executing query : {query}");
                Policies.Get<Policy>("cdviDbPolicy").Execute(() => 
                {
                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        using (IDatabase db = new Database(conn))
                        {
                            var result = db.Fetch<ClockingEvent>(query);
                            events = result;
                        }
                        conn.Close();
                    }
                });



                return events;
            }
            catch (Exception ex)
            {
                log.Fatal($"Failed to retrieve records from database (exception encountered: {ex}).");
                throw;
            }
        }

        public List<ClockingEvent> GetAccessEvents(DateTime from, DateTime to)
        {
            return GetEvents(from, to, new List<int> { 1280, 1288, 1313 });
        }

        public List<ClockingEvent> GetEventsByUsers(DateTime from, DateTime to, List<int> eventTypes, List<int> userIDs)
        {
            return GetEvents(from, to, eventTypes, userIDs);
        }
        **/
    }
}
