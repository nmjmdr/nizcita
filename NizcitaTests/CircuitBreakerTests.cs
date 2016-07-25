using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            CircuitBreaker<int> c = new CircuitBreaker<int>();
            int val = 10;

            int x = c.InvokeAysnc((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => { return val; });
            }).Result;

            Assert.AreEqual<int>(val, x);
        }


        [TestMethod]
        public void OnExceptionAlternateTest() {
            int val = 10;
            int alternate = 5;

            CircuitBreaker<int> c = new CircuitBreaker<int>().Alternate((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => { return alternate; });
            });


            int x = c.InvokeAysnc((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => {
                    // deleberate divide by zero
                    return val/0; });
            }).Result;

            Assert.AreEqual<int>(alternate, x);
        }


        [TestMethod]
        public void OnExceptionInterceptInvokeTest() {
            int val = 10;
            int alternate = 5;

            Exception exp = null;

            CircuitBreaker<int> c = new CircuitBreaker<int>().Alternate((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => { return alternate; });
            }).InterceptException((e) => {
                exp = e;
            });


            int x = c.InvokeAysnc((token) => {
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

            CircuitBreaker<int> c = new CircuitBreaker<int>().Alternate((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => { return alternate; });
            }).WithinTime(new TimeSpan(0, 0, 0, 0, 10));


            int x = c.InvokeAysnc((token) => {
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
        }


        [TestMethod]
        public void OnCalleeCancelDefaultTest() {
            int val = 10;
            int alternate = 5;

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(2);

            CircuitBreaker<int> c = new CircuitBreaker<int>().Alternate((token) => {
                // just to test - wrap the return in a Task run, in real world we would use a async method
                return Task.Run(() => { return alternate; });
            }).Cancellation(cts.Token);


            int x = c.InvokeAysnc((token) => {
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
    }
}