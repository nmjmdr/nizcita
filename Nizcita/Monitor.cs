using Nizcita;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nizcita {

    public delegate void AlarmHandler(Alarm alarm);

    public class Monitor : IMonitor {

        private event AlarmHandler alarmEvt;
        private ILimitedBuffer<Point> limitedBuffer;

        public Monitor(ILimitedBuffer<Point> limitedBuffer) {
            this.limitedBuffer = limitedBuffer;
        }

        public void Log(Point p) {
            limitedBuffer.Put(p);
        }

        public void Listen(AlarmHandler alarmHandler) {
            alarmEvt += alarmHandler;
        }
    }
}
