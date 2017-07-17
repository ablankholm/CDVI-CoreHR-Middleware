using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyca2CoreHrApiTask.Models
{
    public class ClockingPayload
    {
        public string person { get; set; }          = string.Empty;
        public string badge_no { get; set; }        = string.Empty;
        public string clock_date_time { get; set; } = string.Empty;
        public string record_type { get; set; }     = string.Empty;
        public string function_code { get; set; }   = string.Empty;
        public string function_value { get; set; }  = string.Empty;
        public string device_id { get; set; }       = string.Empty;
    }
}
