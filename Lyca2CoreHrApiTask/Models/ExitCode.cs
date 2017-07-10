using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyca2CoreHrApiTask.Models
{
    [Flags]
    public enum ExitCode
    {
        Unknown = -1,
        Success = 0,
        GenericFailure = 1,
        ExceptionEncountered = 2,
        UnhandledException = 3,
        UnhandledThreadException = 4
    }
}