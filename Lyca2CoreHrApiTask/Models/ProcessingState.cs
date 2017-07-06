using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyca2CoreHrApiTask.Models
{
    public class ProcessingState
    {
        public int LastSuccessfulRecordId { get; set; } = 0;
        public List<Event> UnsuccessfulRecords { get; set; } = new List<Event>();
    }
}
