using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Lyca2CoreHrApiTask.Models
{
    public class ProcessingState
    {
        public List<ClockingEvent> PendingRecords = new List<ClockingEvent>();
        public int LastSuccessfulRecord = 0;
    }
}
