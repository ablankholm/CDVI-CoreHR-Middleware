using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace Lyca2CoreHrApiTask.Models
{
    /// <summary>
    /// Used to model a users clocking record as required by the CoreHR API
    ///  
    /// </summary>
    class ClockingPayload
    {
        public string Person { get; set; }
    }
}
