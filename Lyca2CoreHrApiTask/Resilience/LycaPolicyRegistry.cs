using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Polly.Retry;

namespace Lyca2CoreHrApiTask.Resilience
{
    public class LycaPolicyRegistry : PolicyRegistry
    {
        public LycaPolicyRegistry()
        {
            /**Resilience policy for app state serialization:
             * 
             * On any exception, retry up to 3 times with an escalating wait timer between each retry.
             * 
             * Notes: Exceptions during state (de)serialization are expected to involve file system access or related 
             * network micro-faults / stutter in a distributed file system, however given that there is nothing meaningful 
             * we can do about such (or any) exceptions here, we will optimistically retry a few times before bubbling 
             * exceptions up to the caller. We deliberately do not take any compensating action in the event of a failure 
             * to avoid serializing an inconsistent state.
            **/
            this["stateSerializationPolicy"] = Policy.Handle<Exception>()
                                                        .WaitAndRetry(new[]
                                                        {
                                                                TimeSpan.FromSeconds(1),
                                                                TimeSpan.FromSeconds(3),
                                                                TimeSpan.FromSeconds(30)
                                                        });
        }
    }
}
