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

namespace Lyca2CoreHrApiTask.DAL
{
    public class CoreHrApi
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        public LycaPolicyRegistry Policies { get; set; } = new LycaPolicyRegistry();
        private string bearerToken = string.Empty;


        public void PostClockingRecordBatch(ref List<ClockingEvent> batch)
        {
            try
            {
                Queue<ClockingEvent> PostingQueue = new Queue<ClockingEvent>(batch);
                
            }
            catch (Exception ex)
            {
                log.Fatal($"Failed to post records to API (exception encountered: {ex}).");
                throw;
            }
        }


        public HttpStatusCode? PostClockingRecord(string data)
        {
            HttpStatusCode? status;
            try
            {
                //@TODO: Change to actual API endpoint
                string endpoint = @"http://httpbin.org/get";

                return endpoint.PostJsonToUrl(data).GetResponseStatus();
            }
            catch (Exception ex)
            {
                status              = ex.GetStatus();
                string responseBody = ex.GetResponseBody();

                log.Fatal($"Encountered an error while posting to CoreHR API: {ex}. Response: {responseBody}.");

                return status;
            } 
        }


        public HttpStatusCode? Authenticate()
        {
            HttpStatusCode? status;
            try
            {
                //@TODO: Change to actual API endpoint
                string endpoint = @"http://httpbin.org/get";


            }
            catch (Exception ex)
            {
                status = ex.GetStatus();
                string responseBody = ex.GetResponseBody();

                log.Fatal($"Encountered an error while authenticating to CoreHR API: {ex}. Response: {responseBody}.");

                return status;
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
