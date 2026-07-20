using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogViewer
{
    public class LogEntry
    {
        public string Time { get; set; } = "";

        public string LogLevel { get; set; } = "";

        public string Message { get; set; } = "";

        public string OriginalText { get; set; } = "";

    }
}
