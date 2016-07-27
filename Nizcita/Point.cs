using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nizcita {
    public class Point {
        public TimeSpan TimeTaken { get; set; }
        public Exception Fault { get; set; }        
        public bool TimedOut { get; set; }
        public FailureType FailureType { get; set; }
    }
}
