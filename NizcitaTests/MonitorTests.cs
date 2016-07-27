using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nizcita;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nizcita.Tests {
    [TestClass()]
    public class MonitorTests {
        
        [TestMethod]
        public void EveryLogTest() {
            int callCounter = 0;
            Mock<ILimitedBuffer<Point>> monitorMock = new Mock<ILimitedBuffer<Point>>();

            monitorMock.Setup(x => x.Put(It.IsAny<Point>()));
            monitorMock.Setup(x => x.Read()).Returns(() => {
                return new Point[] { new Point { FailureType = FailureType.TimedOut } };
            });

            List<Func<Point[], bool>> reducers = new List<Func<Point[], bool>>();
            reducers.Add((points) => {
                callCounter++;
                return false;
            });

            Monitor m = new Monitor(monitorMock.Object, reducers,1);
            int n = 5;
            for (int i = 0; i < n; i++) {
                m.Log(new Point { FailureType = FailureType.TimedOut });
            }

            Assert.AreEqual<int>(n, callCounter);
        }


        [TestMethod]
        public void EveryNLogTest() {
            int callCounter = 0;
            Mock<ILimitedBuffer<Point>> monitorMock = new Mock<ILimitedBuffer<Point>>();

            monitorMock.Setup(x => x.Put(It.IsAny<Point>()));
            monitorMock.Setup(x => x.Read()).Returns(() => {
                return new Point[] { new Point { FailureType = FailureType.TimedOut } };
            });

            List<Func<Point[], bool>> reducers = new List<Func<Point[], bool>>();
            reducers.Add((points) => {
                callCounter++;
                return false;
            });
            int everyN = 2;
            Monitor m = new Monitor(monitorMock.Object, reducers, everyN);
            int n = 10;
            for (int i = 0; i < n; i++) {
                m.Log(new Point { FailureType = FailureType.TimedOut });
            }

            Assert.AreEqual<int>(n/everyN, callCounter);
        }


        [TestMethod]
        public void AlarmTest() {
            

            Mock<ILimitedBuffer<Point>> monitorMock = new Mock<ILimitedBuffer<Point>>();

            monitorMock.Setup(x => x.Put(It.IsAny<Point>()));
            monitorMock.Setup(x => x.Read()).Returns(() => {
                return new Point[] { new Point { FailureType = FailureType.TimedOut } };
            });

            List<Func<Point[], bool>> reducers = new List<Func<Point[], bool>>();
            reducers.Add((points) => {
                return true;
            });
            int everyN = 1;
            bool alarmRaised = false;
            Monitor m = new Monitor(monitorMock.Object, reducers, everyN);
            m.Listen((a) => {
                alarmRaised = true;
            });
            m.Log(new Point { FailureType = FailureType.TimedOut });

            Assert.IsTrue(alarmRaised);
        }
    }
}