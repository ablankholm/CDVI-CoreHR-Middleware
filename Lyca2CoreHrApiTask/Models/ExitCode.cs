using System;

namespace Lyca2CoreHrApiTask.Models
{
    [Flags]
    public enum ExitCode
    {
        Unknown                     = -1,
        Success                     = 0,
        GenericFailure              = 1,
        ExceptionEncountered        = 2,
        UnhandledException          = 3,
        UnhandledThreadException    = 4,
        CleanupAndExitFailed        = 5,
        StartOrResumeFailed         = 6,
        InvalidTestName             = 7,
        InvalidRecordScope          = 8,
        InvalidDateScope            = 9,
        InvalidUserScope            = 10,
        InvalidXHoursScope          = 11
    }
}