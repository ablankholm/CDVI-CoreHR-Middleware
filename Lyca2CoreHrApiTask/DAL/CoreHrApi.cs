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

namespace Lyca2CoreHrApiTask.DAL
{
    public class CoreHrApi
    {
        private static Logger       log                     = LogManager.GetCurrentClassLogger();
        private BearerTokenInfo     authToken               = new BearerTokenInfo();
        private Properties.Settings settings                = Properties.Settings.Default; //Convenience
        public LycaPolicyRegistry   Policies { get; set; }  = new LycaPolicyRegistry();



        //Depreciated on grounds of performance
        //public void PostClockingRecordBatch(ref List<ClockingEvent> batch, ref int lastSuccessfulRecord, int tokenExpiryTolerance)
        //{
        //    ServicePointManager.UseNagleAlgorithm = false; //Performance optimization
        //    List<ClockingEvent> pendingRecords = batch;
        //    try
        //    {
        //        authToken = Authenticate();

        //        int counter = 0;
        //        Stopwatch timer = new Stopwatch();
        //        timer.Start();
        //        foreach (ClockingEvent record in pendingRecords.OrderBy(r => r.EventID).ToList())
        //        {
        //            counter++;
        //            if (counter % 10 == 0) 
        //            {
        //                log.Debug($"Batch job: {counter.ToString()} records processed in {new TimeSpan(timer.ElapsedTicks).ToString()}");
        //            }
        //            if (authToken.WillExpireWithin(tokenExpiryTolerance))
        //            {
        //                log.Info($"Token expiring within {settings.CoreHrApiTokenExpiryTolerance} seconds, re-authenticating...");
        //                authToken = Authenticate();
        //            }
        //            try
        //            {
        //                Policies.Get<Policy>("apiRecordPostingPolicy").Execute(() => 
        //                {
        //                    //Post clocking record to API
        //                    PostClockingRecord(
        //                        GetClockingPayload(
        //                            record.UserID,
        //                            record.FieldTime,
        //                            record.RecordNameID.ToString()),
        //                        authToken.Token);
        //                });
        //                //If we got this far, post should have succeeded, update processing state and remove record from queue
        //                lastSuccessfulRecord = record.EventID;
        //                batch.Remove(record);
        //            }
        //            catch (Exception)
        //            {
        //                //Catch used here for resilence only, i.e. skip to next record if posting to API unsuccessful
        //                //Logging exceptions here would inflate the log, app state will retain unsucccesful records.
        //                continue;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        log.Fatal($"Failed to post records to API (exception encountered: {ex}).");
        //        throw;
        //    }
        //}


        //Depreciated  on grounds of performance
        //public ProcessingState PostClockingRecordBatchParallel(List<ClockingEvent> batch, int tokenExpiryTolerance)
        //{
        //    log.Info($"Attempting to post batch of {batch.Count.ToString()} clocking records to CoreHr API with an auth token expiry tolerance of {tokenExpiryTolerance} seconds");
        //    try
        //    {
        //        ProcessingState state = new ProcessingState() { PendingRecords = batch };
        //        List<ClockingEvent> successfulRecords = new List<ClockingEvent>();
        //        Stopwatch timer = new Stopwatch();

        //        ServicePointManager.UseNagleAlgorithm = false; //Performance optimization
        //        authToken = Authenticate();

        //        timer.Start();
        //        Parallel.ForEach(state.PendingRecords, pr =>
        //        {
        //            successfulRecords.Add(PostClockingRecord(pr, tokenExpiryTolerance));
        //        });
        //        //state.PendingRecords.ForEach(pr => 
        //        //{
        //        //    successfulRecords.Add(PostClockingRecord(pr, tokenExpiryTolerance));
        //        //});


        //        state.PendingRecords        = state.PendingRecords.Except(successfulRecords).ToList();
        //        state.LastSuccessfulRecord  = successfulRecords.OrderBy(r => r.EventID).Last().EventID;

        //        log.Info($"PostClockingRecordBatchParallel - Time elapsed: {new TimeSpan(timer.ElapsedTicks).ToString()}");
        //        return state;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.Fatal($"Failed to post records to API (exception encountered: {ex}).");
        //        throw;
        //    }
        //}



        public ProcessingState PostClockingRecordBatch(List<ClockingEvent> batch, int tokenExpiryTolerance)
        {
            log.Info($"Attempting to post batch of {batch.Count.ToString()} clocking records to CoreHr API with an auth token expiry tolerance of {tokenExpiryTolerance} seconds");
            try
            {
                ProcessingState state = new ProcessingState();
                state.LastSuccessfulRecord = 0;
                state.PendingRecords = batch;
                List<ClockingEvent> successfulRecords = new List<ClockingEvent>();
                Stopwatch timer = new Stopwatch();

                //Share client and request object for all requests
                var client = new RestClient("https://uatapi.corehr.com/ws/lycau/corehr/v1/clocking/user/");
                RestRequest request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("authorization", $"Bearer {authToken.Token.access_token}");
                request.AddHeader("content-type", "application/json");

                //Performance optimization
                ServicePointManager.UseNagleAlgorithm = false;
                ServicePointManager.MaxServicePointIdleTime = 1;

                authToken = Authenticate();
                timer.Start();
                Parallel.ForEach(batch, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount }, (record) => 
                {
                    if (authToken.WillExpireWithin(tokenExpiryTolerance))
                    {
                        log.Info($"Token expiring within {tokenExpiryTolerance}, re-authenticating...");
                        authToken = Authenticate();
                    }

                    request.AddParameter(
                        "application/json", 
                        GetClockingPayload(record),
                        ParameterType.RequestBody);
                    log.Debug($"Request body param: {request.Parameters.Where(p => p.Type == ParameterType.RequestBody).FirstOrDefault().Value}");
               
                    IRestResponse response = client.Execute(request);
                    //Cleanup for next request (only the first header entry is sent in RestSharp)
                    request.Parameters.Remove(request.Parameters.Where(p => p.Type == ParameterType.RequestBody).FirstOrDefault());

                    state.LastSuccessfulRecord = record.EventID;
                    successfulRecords.Add(record);
                });
                timer.Stop();
                log.Info($"Bacth post completed in: {new TimeSpan(timer.ElapsedTicks).ToString()}");
                

                state.PendingRecords = batch.Except(successfulRecords).ToList();
                return state;
            }
            catch (Exception ex)
            {
                log.Fatal($"Failed to post records to API (exception encountered: {ex}).");
                throw;
            }
        }



        public HttpStatusCode? PostClockingRecord(string data, BearerToken token)
        {
            HttpStatusCode? status;
            try
            {
                var client = new RestClient("https://uatapi.corehr.com/ws/lycau/corehr/v1/clocking/user/");
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("authorization", $"Bearer {token.access_token}");
                request.AddHeader("content-type", "application/json");
                request.AddParameter(
                    "application/json", 
                    data,
                    ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                return response.StatusCode;
            }
            catch (Exception ex)
            {
                status              = ex.GetStatus();
                string responseBody = ex.GetResponseBody();

                log.Fatal($"Encountered an error while posting to CoreHR API: {ex}. Response: {responseBody}.");
                throw;
            } 
        }



        public ClockingEvent PostClockingRecord(ClockingEvent record, int tokenExpiryTolerance)
        {
            HttpStatusCode? status;
            try
            {
                if (authToken.WillExpireWithin(tokenExpiryTolerance))
                {
                    log.Info($"Token expiring within {tokenExpiryTolerance}, re-authenticating...");
                    authToken = Authenticate();
                }
                var client = new RestClient("https://uatapi.corehr.com/ws/lycau/corehr/v1/clocking/user/");
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("authorization", $"Bearer {authToken.Token.access_token}");
                request.AddHeader("content-type", "application/json");
                request.AddParameter(
                    "application/json",
                    GetClockingPayload(record),
                    ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                return record;
            }
            catch (Exception ex)
            {
                status = ex.GetStatus();
                string responseBody = ex.GetResponseBody();

                log.Fatal($"Encountered an error while posting to CoreHR API: {ex}. Response: {responseBody}.");
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
    }
}
