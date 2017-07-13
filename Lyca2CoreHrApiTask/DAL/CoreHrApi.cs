using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lyca2CoreHrApiTask.Models;
using System.Net;
using Lyca2CoreHrApiTask.Resilience;

namespace Lyca2CoreHrApiTask.DAL
{
    public class CoreHrApi
    {
        public LycaPolicyRegistry Policies { get; set; } = new LycaPolicyRegistry();
    }
}
