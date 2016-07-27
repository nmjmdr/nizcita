using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nizcita;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nizcita.Tests {
    [TestClass()]
    public class CircuitBreakerTests {

        

        [TestMethod]
        public void SimpleInvokeTest() {

            Mock<IMonitor> monitorMock = new Mock<IMonitor>();
            monitorMock.Setup(m => m.Log(It.IsAny<Point>()));

            CircuitBreaker<int> c = new CircuitBreaker<int>(monitorMock.Object);
            int val = 10;

            int x = c.InvokeAsync((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => { return val; });
            }).Result;

            Assert.AreEqual<int>(val, x);
        }

        [TestMethod]
        public void OnExceptionAlternateTest() {
            int val = 10;
            int alternate = 5;

            Point point = null;
            Mock<IMonitor> monitorMock = new Mock<IMonitor>();
            monitorMock.Setup(m => m.Log(It.IsAny<Point>())).Callback<Point>((p) => { point = p; });

            CircuitBreaker<int> c = new CircuitBreaker<int>(monitorMock.Object).Alternate((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => { return alternate; });
            });


            int x = c.InvokeAsync((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => {
                    // deleberate divide by zero
                    return val/0; });
            }).Result;

            Assert.AreEqual<int>(alternate, x);
            Assert.IsNotNull(point);
            Assert.IsTrue(point.Fault is DivideByZeroException);
            Assert.IsTrue(point.FailureType == FailureType.Fault);
        }

        [TestMethod]
        public void OnCloseAlternateTest() {
            int val = 10;
            int alternate = 5;

            Mock<IMonitor> monitorMock = new Mock<IMonitor>();
            monitorMock.Setup(m => m.Log(It.IsAny<Point>()));

            CircuitBreaker<int> c = new CircuitBreaker<int>(monitorMock.Object).Alternate((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => { return alternate; });
            });

            c.Close();

            int x = c.InvokeAsync((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => {                   
                    return val;
                });
            }).Result;

            Assert.AreEqual<int>(alternate, x);
        }


        [TestMethod]
        public void OnInvalidResultAlternateTest() {
            int val = 10;
            int alternate = 5;

            Point point = null;
            Mock<IMonitor> monitorMock = new Mock<IMonitor>();
            monitorMock.Setup(m => m.Log(It.IsAny<Point>())).Callback<Point>((p) => { point = p; });

            CircuitBreaker<int> c = new CircuitBreaker<int>(monitorMock.Object).Alternate((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => { return alternate; });
            }).CheckResult((r) => {
                if (r == val) {
                    return false;
                }
                return true;
            });           

            int x = c.InvokeAsync((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => {
                    return val;
                });
            }).Result;

            Assert.AreEqual<int>(alternate, x);
            Assert.IsNotNull(point);
            Assert.IsTrue(point.FailureType == FailureType.InvalidResult);
        }


        [TestMethod]
        public void OnValidResultInvokeTest() {
            int val = 10;
            int alternate = 5;

            Mock<IMonitor> monitorMock = new Mock<IMonitor>();
            monitorMock.Setup(m => m.Log(It.IsAny<Point>()));

            CircuitBreaker<int> c = new CircuitBreaker<int>(monitorMock.Object).Alternate((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => { return alternate; });
            }).CheckResult((r) => {
                if (r == val) {
                    return true;
                }
                return false;
            });

            int x = c.InvokeAsync((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => {
                    return val;
                });
            }).Result;

            Assert.AreEqual<int>(val, x);
        }


        [TestMethod]
        public void OnExceptionInterceptInvokeTest() {
            int val = 10;
            int alternate = 5;

            Exception exp = null;

            Mock<IMonitor> monitorMock = new Mock<IMonitor>();
            monitorMock.Setup(m => m.Log(It.IsAny<Point>()));

            CircuitBreaker<int> c = new CircuitBreaker<int>(monitorMock.Object).Alternate((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => { return alternate; });
            }).InterceptException((e) => {
                exp = e;
            });


            int x = c.InvokeAsync((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => {
                    // deleberate divide by zero
                    return val / 0;
                });
            }).Result;

            Assert.AreEqual<int>(alternate, x);
            Assert.IsTrue(exp is DivideByZeroException);

        }



        [TestMethod]
        public void OnTimeoutAlternateTest() {
            int val = 10;
            int alternate = 5;

            Point point = null;
            Mock<IMonitor> monitorMock = new Mock<IMonitor>();
            monitorMock.Setup(m => m.Log(It.IsAny<Point>())).Callback<Point>((p) => { point = p; });

            CircuitBreaker<int> c = new CircuitBreaker<int>(monitorMock.Object).Alternate((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => { return alternate; });
            }).WithinTime(new TimeSpan(0, 0, 0, 0, 10));


            int x = c.InvokeAsync((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => {
                    for(int i=0;i<10;i++) {
                        if(token.IsCancellationRequested) {
                            throw new TaskCanceledException();
                        }
                        Thread.Sleep(5);
                    }
                    return val;
                });
            }).Result;

            Assert.AreEqual<int>(alternate, x);
            Assert.IsNotNull(point);
            Assert.IsTrue(point.FailureType == FailureType.TimedOut);
            Assert.IsNotNull(point.TimeTaken);
        }

        [TestMethod]
        public void OnTimeoutWithin0AlternateTest() {
            int val = 10;
            int alternate = 5;

            Point point = null;
            Mock<IMonitor> monitorMock = new Mock<IMonitor>();
            monitorMock.Setup(m => m.Log(It.IsAny<Point>())).Callback<Point>((p) => { point = p; });

            CircuitBreaker<int> c = new CircuitBreaker<int>(monitorMock.Object).Alternate((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => { return alternate; });
            }).WithinTime(new TimeSpan(0, 0, 0, 0, 0));


            int x = c.InvokeAsync((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => {
                    for (int i = 0; i < 10; i++) {
                        if (token.IsCancellationRequested) {
                            throw new TaskCanceledException();
                        }
                        Thread.Sleep(5);
                    }
                    return val;
                });
            }).Result;

            Assert.AreEqual<int>(alternate, x);
            Assert.IsNotNull(point);
            Assert.IsTrue(point.FailureType == FailureType.TimedOut);
            Assert.IsNotNull(point.TimeTaken);
        }


        [TestMethod]
        public void OnCalleeCancelDefaultTest() {
            int val = 10;
            int alternate = 5;

            Mock<IMonitor> monitorMock = new Mock<IMonitor>();
            monitorMock.Setup(m => m.Log(It.IsAny<Point>()));

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(2);

            CircuitBreaker<int> c = new CircuitBreaker<int>(monitorMock.Object).Alternate((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => { return alternate; });
            }).Cancellation(cts.Token);


            int x = c.InvokeAsync((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => {
                    for (int i = 0; i < 10; i++) {
                        Thread.Sleep(2);
                        if (token.IsCancellationRequested) {
                            throw new TaskCanceledException();
                        }                        
                    }
                    return val;
                });
            }).Result;

            Assert.AreEqual<int>(default(int),x);
        }

        [TestMethod]
        public void OnAlarmCloseTest() {

            Mock<IMonitor> monitorMock = new Mock<IMonitor>();

            AlarmHandler ah = null;
            monitorMock.Setup(m => m.Listen(It.IsAny<AlarmHandler>())).Callback<AlarmHandler> ((a) => {
                ah = a;
            });

            monitorMock.Setup(m => m.Log(It.IsAny<Point>()));

            CircuitBreaker<int> c = new CircuitBreaker<int>(monitorMock.Object);

            ah(new Alarm());

            Assert.IsFalse(c.IsOpen);
        }
    }
}