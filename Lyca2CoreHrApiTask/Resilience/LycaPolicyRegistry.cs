using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Polly.Retry;
using Polly.Timeout;
using Lyca2CoreHrApiTask.Properties;

namespace Lyca2CoreHrApiTask.Resilience
{
    public class LycaPolicyRegistry : PolicyRegistry
    {
        public LycaPolicyRegistry()
        {
            /**Resilience (retry) policy for app state serialization:
             * 
             * On any exception, retry up to 3 times with an escalating wait timer between each retry.
             * 
             * Notes: Exceptions during state (de)serialization are expected to involve file system access or related 
             * network micro-faults / stutter in a distributed file system, however given that there is nothing meaningful 
             * we can do about such (or any) exceptions here, we will optimistically retry a few times before bubbling 
             * exceptions up to the caller. We deliberately do not take any compensating action in the event of a failure 
             * to avoid serializing an inconsistent state.
            **/
            Policy stateSerializationPolicy = Policy.Handle<Exception>()
                                                        .WaitAndRetry(new[]
                                                        {
                                                            TimeSpan.FromSeconds(1),
                                                            TimeSpan.FromSeconds(3),
                                                            TimeSpan.FromSeconds(30)
                                                        });

            /** Resilience (retry) policiy for contacting the CDVi database:
             * 
             * On any exception, retry up to 3 times with an escalating wait timer between each retry. 
             * 
             * Notes: We catch all exceptions since there are no specific exceptions (typically SQL / connection related exceptions)
             * that we can meaningfully handle.
            **/
            Policy cdviDbRetryPolicy = Policy.Handle<Exception>()
                                                .WaitAndRetry(new[]
                                                {
                                                    TimeSpan.FromSeconds(1),
                                                    TimeSpan.FromSeconds(5),
                                                    TimeSpan.FromSeconds(10)
                                                });

            /** Resilience (timeout) policiy for contacting the CDVi database:
             * 
             * If excution time exceeds the threshold, terminate and throw an exception
             * 
             * Notes: This policy will mostly be used when qurying the Events table in the CDVI database which holds a large number
             * of records, to prevent a query from taking up too many resources on the server. The primary purpose of the server that 
             * houses the database is running the CDVI application, so caution is needed to avoid taxing the shared resources on the machine.
            **/
            Policy cdviDbTimeoutPolicy = Policy.Timeout(Settings.Default.CdviDbTimeout);

            /** Resilience policiy for contacting the CDVi database:
             * 
             * Combines the CDVI database retry and timeout policies into a single policy for convenience.
            **/
            Policy cdviDbPolicy = Policy.Wrap(cdviDbTimeoutPolicy, cdviDbRetryPolicy);

            /** Resilience policiy for contacting the CDVi database:
             * 
             * Combines the CDVI database retry and timeout policies into a single policy for convenience.
            **/
            Policy apiRecordPostingPolicy = Policy.Handle<Exception>().Retry(3);


            //Register policies
            this["stateSerializationPolicy"]    = stateSerializationPolicy;
            this["cdviDbRetryPolicy"]           = cdviDbRetryPolicy;
            this["cdviDbTimeoutPolicy"]         = cdviDbTimeoutPolicy;
            this["cdviDbPolicy"]                = cdviDbPolicy;
            this["apiRecordPostingPolicy"]      = apiRecordPostingPolicy;
        }
    }
}
