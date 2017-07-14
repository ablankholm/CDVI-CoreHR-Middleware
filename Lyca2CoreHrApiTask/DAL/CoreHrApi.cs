using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lyca2CoreHrApiTask.Models;
using System.Net;
using Lyca2CoreHrApiTask.Resilience;
using NLog;

namespace Lyca2CoreHrApiTask.DAL
{
    public class CoreHrApi
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        public LycaPolicyRegistry Policies { get; set; } = new LycaPolicyRegistry();

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


        public void PostClockingRecord(ClockingEvent record)
        {

        }
    }
}
