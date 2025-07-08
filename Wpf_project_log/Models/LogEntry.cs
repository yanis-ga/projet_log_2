using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_project_log.Models
{
    public class LogEntry
    {
        public DateTime TimeStamp { get; set; }
        public string CallStack { get; set; }
        public string ErrorCallStack { get; set; }
        public string EventMessage { get; set; }
        public string TaskName { get; set; }
    }
}
