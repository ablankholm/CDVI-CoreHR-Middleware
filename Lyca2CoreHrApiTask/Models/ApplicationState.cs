using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyca2CoreHrApiTask.Models
{
    public class ApplicationState
    {
        public ProcessingState ProcessingState { get; set; } = new ProcessingState();
    }
}
