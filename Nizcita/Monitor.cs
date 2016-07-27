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
        private IEnumerable<Func<Point[], bool>> reducers;
        private int processEveryN;
        private object lockObject = new object();
        private volatile int counter = 0;

        public Monitor(ILimitedBuffer<Point> limitedBuffer,IEnumerable<Func<Point[],bool>> reducers,int processEveryN = 1) {
            this.limitedBuffer = limitedBuffer;
            this.reducers = reducers;
            this.processEveryN = processEveryN;
        }              

        public void Log(Point p) {
            limitedBuffer.Put(p);

            counter++;

            bool process = false;
            lock (lockObject) {
                if (counter >= processEveryN) {
                    counter = 0;
                    process = true;
                }
            }

            Point[] points = limitedBuffer.Read();
            if (!process) {
                return;
            }
            runReducers(points);            
        }

        private void runReducers(Point[] points) {
            foreach (Func<Point[], bool> reducer in reducers) {
                if (reducer(points)) {
                    alarmEvt?.Invoke(new Alarm());
                    break;
                }
            }
        }

        public void Listen(AlarmHandler alarmHandler) {
            alarmEvt += alarmHandler;
        }
    }
}
