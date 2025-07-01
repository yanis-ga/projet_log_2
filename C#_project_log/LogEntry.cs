using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C__project_log
{
    internal class LogEntry
    {
        public DateTime TimeStamp;
        public string CallStack;
        public string ErrorCallStack;
        public string EventMessage;
        public string TaskName;
    }
}
