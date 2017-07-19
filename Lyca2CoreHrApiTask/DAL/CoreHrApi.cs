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

namespace Lyca2CoreHrApiTask.DAL
{
    public class CoreHrApi
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        public LycaPolicyRegistry Policies { get; set; } = new LycaPolicyRegistry();
        private BearerTokenInfo authToken = new BearerTokenInfo();



        public void PostClockingRecordBatch(ref List<ClockingEvent> batch, ref int lastSuccessfulRecord)
        {
            var settings = Properties.Settings.Default;
            try
            {
                authToken = Authenticate();


                foreach (var record in batch)
                {
                    if (authToken.WillExpireWithin(settings.CoreHrApiTokenExpiryTolerance))
                    {
                        log.Info($"Token expiring within {settings.CoreHrApiTokenExpiryTolerance} seconds, re-authenticating...");
                        authToken = Authenticate();
                    }
                    try
                    {
                        Policies.Get<Policy>("apiRecordPostingPolicy").Execute(() => 
                        {
                            //Post clocking record to API
                            PostClockingRecord(GetClockingPayload(record), authToken.Token);
                        });
                        //If we got this far, post should have succeeded, update processing state and remove record from queue
                        lastSuccessfulRecord = record.EventID;
                        batch.Remove(record);
                    }
                    catch (Exception)
                    {
                        //Catch used here for resilence only, i.e. skip to next record if posting to API unsuccessful
                        //Logging exceptions here would inflate the log, app state will retain unsucccesful records.
                        continue;
                    }
                }
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
                //@TODO: fill in actual data if using inline data construction
                request.AddParameter(
                    "application/json", 
                    //"{\r\n\"person\" : \"\", \r\n\"badge_no\": \"1198\", \r\n\"clock_date_time\" : \"2017-07-18 08:56 +00:00\",\r\n\"record_type\"     : \"B0\", \r\n\"function_code\"   : \"\", \r\n\"function_value\"  : \"\",  \r\n\"device_id\"       : \"\"\r\n}", 
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



        public BearerTokenInfo Authenticate()
        {
            log.Info($"Attempting to acquire bearer token from CoreHR API.");
            BearerTokenInfo tokenInfo;
            try
            {
                //Get token
                var settings    = Properties.Settings.Default;
                var client      = new RestClient(settings.CoreHrApiOAuthTokenEndpoint);
                var request     = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddHeader("authorization", $"Basic {settings.CoreHrApiBase64EncodedAppCredentials}");
                request.AddParameter("application/x-www-form-urlencoded", "grant_type=client_credentials", ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                //Store token
                tokenInfo = new BearerTokenInfo(JsonConvert.DeserializeObject<BearerToken>(response.Content), DateTime.UtcNow);

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
                badge_no = clockingEvent.UserNameID.ToString(),
                clock_date_time = clockingEvent.FieldTime.ToString($"yyyy-MM-dd HH:mm {timeZoneOffset}"),
                record_type = "B0", 
                device_id = clockingEvent.OperatorID.ToString()
            };

            return JsonConvert.SerializeObject(cp, Formatting.Indented);
        }
    }
}
