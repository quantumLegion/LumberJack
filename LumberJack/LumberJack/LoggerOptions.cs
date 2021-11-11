using System;
using System.Collections.Generic;
using System.Text;

namespace BITS.Logger
{
    public class LoggerOptions
    {
        public string EnvironmentName { get; set; }
        public string LogLocation { get; set; }
        public bool EnableDiagnostics { get; set; }
        public string EventCollector { get; set; }
        public string EventCollectorToken { get; set; }
        public bool AddClaims { get; set; }
    }

}
