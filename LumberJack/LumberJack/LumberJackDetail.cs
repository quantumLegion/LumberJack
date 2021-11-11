using System;
using System.Collections.Generic;

namespace BITS.Logger
{
    public class LumberJackDetail
    {
        //public LumberJackDetail()
        //{
        //    Timestamp = DateTime.Now;
        //}
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }

        public string LogType { get; set; }
        // WHERE
        public string Product { get; set; }
        public string Layer { get; set; }
        public string Location { get; set; }
        public string Hostname { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public string EnvironmentName { get; set; }

        // WHO
        public int? PartId { get; set; }
        public string UserName { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        // EVERYTHING ELSE
        public long? ElapsedMilliseconds { get; set; }  // only for performance entries
        public Exception Exception { get; set; }  // the exception for error logging
        public LumberJackException LumberJackException { get; set; }
        public string CorrelationId { get; set; } // exception shielding from server to client
        public Dictionary<string, object> AdditionalInfo { get; set; }  // everything else

    }
}
