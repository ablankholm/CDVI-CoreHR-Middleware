using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyca2CoreHrApiTask.Models
{
    public enum RecordScope
    {
        Invalid                     = -1,
        None                        = 0,
        Yesterday                   = 1,
        FromEventID                 = 2,
        SpecificDate                = 3,
        SpecificUser                = 4,
        LastXHours                  = 5
    }
}
