using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lyca2CoreHrApiTask.Models;
using System.Net;
using Lyca2CoreHrApiTask.Resilience;
using NLog;
using ServiceStack;
using Newtonsoft.Json;
using Polly;
using Polly.Registry;
using Polly.Retry;
using RestSharp;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Lyca2CoreHrApiTask.DAL
{
    public class CoreHrApi
    {
        private static Logger       log                     = LogManager.GetCurrentClassLogger();
        private BearerTokenInfo     authToken               = new BearerTokenInfo();
        private Properties.Settings settings                = Properties.Settings.Default; //Convenience
        public LycaPolicyRegistry   Policies { get; set; }  = new LycaPolicyRegistry();
        private HttpClient          client                  = new HttpClient();


        //Deprecated on grounds of performance
        //public ProcessingState PostClockingRecordBatch(List<ClockingEvent> batch, int tokenExpiryTolerance)
        //{
        //    log.Info($"Attempting to post batch of {batch.Count.ToString()} clocking records to CoreHr API with an auth token expiry tolerance of {tokenExpiryTolerance} seconds");
        //    try
        //    {
        //        ProcessingState state = new ProcessingState();
        //        state.LastSuccessfulRecord = 0;
        //        state.PendingRecords = new List<ClockingEvent>();
        //        Stopwatch timer = new Stopwatch();

        //        //Share client and request object with all post requests (to reduce in-loop object creation and related performance overhead )
        //        var client = new RestClient("https://uatapi.corehr.com/ws/lycau/corehr/v1/clocking/user/");
        //        var request = new RestRequest(Method.POST);
        //        request.AddHeader("cache-control", "no-cache");
        //        request.AddHeader("content-type", "application/json");

        //        //Performance optimization related to RestSharp
        //        ServicePointManager.UseNagleAlgorithm = false;
        //        ServicePointManager.MaxServicePointIdleTime = 1;

        //        authToken = Authenticate();
        //        request.AddHeader("authorization", $"Bearer {authToken.Token.access_token}");
        //        string timeZoneOffset = "+00:00";
        //        timer.Start();
        //        foreach (var record in batch)
        //        {
        //            //Make sure we're authenticated before posting
        //            if (authToken.WillExpireWithin(tokenExpiryTolerance))
        //            {
        //                log.Info($"Token expiring within {tokenExpiryTolerance}, re-authenticating...");
        //                authToken = Authenticate();
        //                request = new RestRequest(Method.POST);
        //                request.AddHeader("cache-control", "no-cache");
        //                request.AddHeader("content-type", "application/json");
        //                request.AddHeader("authorization", $"Bearer {authToken.Token.access_token}");
        //            }

        //            //Generate request body
        //            request.AddParameter(
        //                "application/json",
        //                "{\r\n\"person\" : \"\", \r\n\"badge_no\": \""
        //                    + $"{record.UserID.ToString()}"
        //                    + "\", \r\n\"clock_date_time\" : \""
        //                    + $"{record.FieldTime.ToString($"yyyy-MM-dd HH:mm")} {timeZoneOffset}"
        //                    + "\",\r\n\"record_type\"     : \"B0\", \r\n\"function_code\"   : \"\",\r\n\"function_value\"  : \"\",  \r\n\"device_id\"       : \""
        //                    + $"{record.RecordNameID.ToString()}"
        //                    + "\"\r\n}",
        //                ParameterType.RequestBody);

        //            //Post request
        //            IRestResponse response = client.Execute(request);
        //            log.Debug($"Record {record.EventID.ToString()}: {response.StatusCode.ToString()}");

        //            //Update state
        //            if (response.StatusCode == HttpStatusCode.Created)
        //            {
        //                state.LastSuccessfulRecord = record.EventID;
        //            }
        //            else
        //            {
        //                state.PendingRecords.Add(record);
        //            }

        //            //Cleanup body param for next iteration (RestSharp ignores multiple body params beyond the first)
        //            request.Parameters.Remove(request.Parameters.Where(p => p.Type == ParameterType.RequestBody).FirstOrDefault());
        //        }
        //        timer.Stop();
        //        log.Info($"Batch post completed in: {new TimeSpan(timer.ElapsedTicks).ToString()}");
        //        log.Info($"Pending records: {state.PendingRecords.Count}, Last successful record: {state.LastSuccessfulRecord}");

        //        return state;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.Fatal($"Failed to post records to API (exception encountered: {ex}).");
        //        throw;
        //    }
        //}


        //Deprecated on grounds of performance
        //public ProcessingState PostClockingRecordBatch2(List<ClockingEvent> batch, int tokenExpiryTolerance)
        //{
        //    log.Info($"Attempting to post batch of {batch.Count.ToString()} clocking records to CoreHr API with an auth token expiry tolerance of {tokenExpiryTolerance} seconds");
        //    try
        //    {
        //        ProcessingState state = new ProcessingState();
        //        state.LastSuccessfulRecord = 0;
        //        state.PendingRecords = new List<ClockingEvent>();
        //        Stopwatch timer = new Stopwatch();
        //        string timeZoneOffset = "+00:00";


        //        timer.Start();
        //        authToken = Authenticate();

        //        //Shared client object
        //        HttpClient client = new HttpClient();
        //        client.BaseAddress = new Uri("https://uatapi.corehr.com/");
        //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.Token.access_token);
        //        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue() { NoCache = true };

        //        ServicePointManager.DnsRefreshTimeout = (int)TimeSpan.FromMinutes(1).TotalMilliseconds;


        //        foreach (var record in batch)
        //        {
        //            //Make sure we're authenticated before posting
        //            if (authToken.WillExpireWithin(tokenExpiryTolerance))
        //            {
        //                log.Info($"Token expiring within {tokenExpiryTolerance}, re-authenticating...");
        //                authToken = Authenticate();
        //                client.DefaultRequestHeaders.Clear();
        //                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.Token.access_token);
        //                client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue() { NoCache = true };
        //            }

        //            //Generate request content
        //            StringContent content = new StringContent(
        //                "{\r\n\"person\" : \"\", \r\n\"badge_no\": \""
        //                    + $"{record.UserID.ToString()}"
        //                    + "\", \r\n\"clock_date_time\" : \""
        //                    + $"{record.FieldTime.ToString($"yyyy-MM-dd HH:mm")} {timeZoneOffset}"
        //                    + "\",\r\n\"record_type\"     : \"B0\", \r\n\"function_code\"   : \"\",\r\n\"function_value\"  : \"\",  \r\n\"device_id\"       : \""
        //                    + $"{record.RecordNameID.ToString()}"
        //                    + "\"\r\n}", 
        //                Encoding.UTF8);
        //            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        //            var response = client.PostAsync("ws/lycau/corehr/v1/clocking/user/", content).Result;
        //            log.Debug($"Record {record.EventID.ToString()}: {response.StatusCode.ToString()}");

        //            //Update state
        //            if (response.StatusCode == HttpStatusCode.Created)
        //            {
        //                state.LastSuccessfulRecord = record.EventID;
        //            }
        //            else
        //            {
        //                state.PendingRecords.Add(record);
        //            }
        //        }


        //        timer.Stop();
        //        log.Info($"Batch post completed in: {new TimeSpan(timer.ElapsedTicks).ToString()}");
        //        log.Info($"Pending records: {state.PendingRecords.Count}, Last successful record: {state.LastSuccessfulRecord}");

        //        return state;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.Fatal($"Failed to post records to API (exception encountered: {ex}).");
        //        throw;
        //    }
        //}


        //Deprecated on grounds of performance
        //public async Task<ProcessingState> PostClockingRecordBatch3(List<ClockingEvent> batch, int tokenExpiryTolerance)
        //{
        //    log.Info($"Attempting to post batch of {batch.Count.ToString()} clocking records to CoreHr API with an auth token expiry tolerance of {tokenExpiryTolerance} seconds");
        //    try
        //    {
        //        ProcessingState state = new ProcessingState();
        //        state.LastSuccessfulRecord = 0;
        //        state.PendingRecords = new List<ClockingEvent>();
        //        Stopwatch timer = new Stopwatch();
        //        string timeZoneOffset = "+00:00";


        //        timer.Start();
        //        authToken = Authenticate();

        //        //Configure Shared client object
        //        client.BaseAddress = new Uri("https://uatapi.corehr.com/");
        //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.Token.access_token);
        //        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue() { NoCache = true };

        //        //Needed for large batches
        //        ServicePointManager.DnsRefreshTimeout = (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

        //        //Generate request bodies
        //        List<StringContent> requestContentList = new List<StringContent>();
        //        StringContent sc;
        //        foreach (var record in batch)
        //        {
        //            sc = new StringContent(
        //                    "{\r\n\"person\" : \"\", \r\n\"badge_no\": \""
        //                    + $"{record.UserID.ToString()}"
        //                    + "\", \r\n\"clock_date_time\" : \""
        //                    + $"{record.FieldTime.ToString($"yyyy-MM-dd HH:mm")} {timeZoneOffset}"
        //                    + "\",\r\n\"record_type\"     : \"B0\", \r\n\"function_code\"   : \"\",\r\n\"function_value\"  : \"\",  \r\n\"device_id\"       : \""
        //                    + $"{record.RecordNameID.ToString()}"
        //                    + "\"\r\n}",
        //                Encoding.UTF8);
        //            sc.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        //            requestContentList.Add(sc);
        //        }

        //        //Generate requests
        //        var taskList = new List<Task<HttpResponseMessage>>();
        //        foreach (var content in requestContentList)
        //        {
        //            taskList.Add(client.PostAsync("ws/lycau/corehr/v1/clocking/user/", content));
        //        }

        //        //Execute all requests
        //        var results = await Task.WhenAll(taskList.ToArray());

        //        timer.Stop();
        //        log.Info($"Batch post completed in: {new TimeSpan(timer.ElapsedTicks).ToString()}");
        //        log.Info($"Pending records: {state.PendingRecords.Count}, Last successful record: {state.LastSuccessfulRecord}");

        //        return state;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.Fatal($"Failed to post records to API (exception encountered: {ex}).");
        //        throw;
        //    }
        //}



        public async Task<ProcessingState> PostClockingRecordBatch(List<ClockingEvent> batch, int tokenExpiryTolerance)
        {
            log.Info($"Attempting to post batch of {batch.Count.ToString()} clocking records to CoreHr API with an auth token expiry tolerance of {tokenExpiryTolerance} seconds");
            try
            {
                ProcessingState state = new ProcessingState();
                state.LastSuccessfulRecord = 0;
                state.PendingRecords = new List<ClockingEvent>();
                Stopwatch timer = new Stopwatch();
                string timeZoneOffset = "+00:00";


                timer.Start();
                //Pre-Authenticate
                authToken = Authenticate();

                //Configure Shared client object
                client.BaseAddress = new Uri("https://uatapi.corehr.com/");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.Token.access_token);
                client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue() { NoCache = true };

                //Needed for large batches
                ServicePointManager.DnsRefreshTimeout = (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

                //Generate requests
                var requestTasks = new List<Task<KeyValuePair<ClockingEvent, HttpResponseMessage>>>();
                foreach (var record in batch)
                {
                    requestTasks.Add(PostClockingRecord(record, tokenExpiryTolerance, timeZoneOffset));
                }

                //Execute all requests
                var responses = await Task.WhenAll(requestTasks.ToArray());

                //Update state
                foreach (var response in responses)
                {
                    if (response.Value.IsSuccessStatusCode)
                    {
                        state.LastSuccessfulRecord = response.Key.EventID;
                    }
                    else
                    {
                        state.PendingRecords.Add(response.Key);
                    }
                }

                timer.Stop();
                log.Info($"Batch post completed in: {new TimeSpan(timer.ElapsedTicks).ToString()}");
                log.Info($"Pending records: {state.PendingRecords.Count}, Last successful record: {state.LastSuccessfulRecord}");

                return state;
            }
            catch (Exception ex)
            {
                log.Fatal($"Failed to post records to API (exception encountered: {ex}).");
                throw;
            }
        }



        public async Task<KeyValuePair<ClockingEvent, HttpResponseMessage>> PostClockingRecord(ClockingEvent record, int tokenExpiryTolerance, string timeZoneOffset = "+00:00")
        {
            try
            {
                //Make sure we're authenticated before posting
                if (authToken.WillExpireWithin(tokenExpiryTolerance))
                {
                    log.Info($"Token expiring within {tokenExpiryTolerance}, re-authenticating...");
                    authToken = Authenticate();
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.Token.access_token);
                    client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                }

                //Generate content body
                StringContent content = new StringContent(
                                "{\r\n\"person\" : \"\", \r\n\"badge_no\": \""
                                    + $"{record.UserID.ToString()}"
                                    + "\", \r\n\"clock_date_time\" : \""
                                    + $"{record.FieldTime.ToString($"yyyy-MM-dd HH:mm")} {timeZoneOffset}"
                                    + "\",\r\n\"record_type\"     : \"B0\", \r\n\"function_code\"   : \"\",\r\n\"function_value\"  : \"\",  \r\n\"device_id\"       : \""
                                    + $"{record.RecordNameID.ToString()}"
                                    + "\"\r\n}",
                                Encoding.UTF8);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                //Execute request 
                var response = await client.PostAsync("ws/lycau/corehr/v1/clocking/user/", content);
                log.Debug($"Record {record.EventID.ToString()}: {response.StatusCode.ToString()}");


                return new KeyValuePair<ClockingEvent, HttpResponseMessage>(record, response);
            }
            catch (Exception ex)
            {
                log.Fatal($"Failed to post record with id {record.EventID} to API (exception encountered: {ex}).");
                throw;
            }
        }



        public BearerTokenInfo Authenticate()
        {
            log.Info($"Attempting to acquire bearer token from CoreHR API.");
            BearerTokenInfo tokenInfo;
            try
            {
                //Get token
                var client      = new RestClient(settings.CoreHrApiOAuthTokenEndpoint);
                var request     = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddHeader("authorization", $"Basic {settings.CoreHrApiBase64EncodedAppCredentials}");
                request.AddParameter("application/x-www-form-urlencoded", "grant_type=client_credentials", ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                //Store token
                tokenInfo = new BearerTokenInfo(JsonConvert.DeserializeObject<BearerToken>(response.Content), DateTime.Now);

                log.Info($"Authentication response: {response.Content}");
                return tokenInfo; 
            }
            catch (Exception ex)
            {
                HttpStatusCode? status          = ex.GetStatus();
                string          responseBody    = ex.GetResponseBody();

                log.Fatal($"Encountered an error while authenticating to CoreHR API: {ex}. Response: {responseBody}.");
                throw;
            }
        }



        #region Helpers

        public string GetClockingPayload(ClockingEvent clockingEvent)
        {
            /**The API requires the 'clock_date_time' to arrive in YYYY-MM-DD HH24:MI TZH:TZM format, i.e "2017-02-15 08:56 +00:00" 
             * We're leaving time zone offset as avariable here in case more special treatment is requried in the future
             * (see https://documenter.getpostman.com/view/920731/corehr-web-services/2LRaEJ#305a7b9b-314f-26f9-0f4d-f8a22af3e7bc for details)
             * 
             * Since our clocking system does not implement recording of 'out' and 'in' (clocking direction) consistently, the API 
             * has been configured to expect record_type = B0 (undefined) for all records.
            **/
            string timeZoneOffset = "+00:00";
            ClockingPayload cp = new ClockingPayload()
            {
                badge_no = clockingEvent.UserID.ToString(),
                clock_date_time = clockingEvent.FieldTime.ToString($"yyyy-MM-dd HH:mm {timeZoneOffset}"),
                record_type = "B0",
                device_id = clockingEvent.RecordNameID.ToString()
            };

            return JsonConvert.SerializeObject(cp, Formatting.Indented);
        }



        public string GetClockingPayload(int badgeNumber, DateTime clockDateTime, String deviceId, string timeZoneOffset = "+00:00")
        {
            /**The API requires the 'clock_date_time' to arrive in YYYY-MM-DD HH24:MI TZH:TZM format, i.e "2017-02-15 08:56 +00:00" 
             * We're leaving time zone offset as avariable here in case more special treatment is requried in the future
             * (see https://documenter.getpostman.com/view/920731/corehr-web-services/2LRaEJ#305a7b9b-314f-26f9-0f4d-f8a22af3e7bc for details)
             * 
             * Since our clocking system does not implement recording of 'out' and 'in' (clocking direction) consistently, the API 
             * has been configured to expect record_type = B0 (undefined) for all records.
            **/

            //Ad hoc json string creation
            //This is to avoid overhead from object creation, serialization and asociated reflection
            StringWriter sw = new StringWriter();
            JsonTextWriter writer = new JsonTextWriter(sw);

            writer.WriteStartObject(); // {

            //Deliberately blank (badge_id used to identify the employee)
            writer.WritePropertyName("person");
            writer.WriteValue("");

            writer.WritePropertyName("badge_no");
            writer.WriteValue(badgeNumber.ToString());

            writer.WritePropertyName("clock_date_time");
            writer.WriteValue(clockDateTime.ToString($"yyyy-MM-dd HH:mm {timeZoneOffset}"));

            //Always B0 (undefined)
            writer.WritePropertyName("record_type");
            writer.WriteValue("B0");

            //Not used but required for schema validation on API end
            writer.WritePropertyName("function_code");
            writer.WriteValue("");

            //Not used but required for schema validation on API end
            writer.WritePropertyName("function_value");
            writer.WriteValue("");

            writer.WritePropertyName("device_id");
            writer.WriteValue(deviceId);

            writer.WriteEndObject(); // }

            return sw.ToString();
        }
        #endregion


    }
}
