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
        public void InvokeTest() {
            CircuitBreaker<int> cb = new CircuitBreaker<int>().Alternate(() => { return 20; });
            int expected = 10;

            int x = cb.Invoke(() => {
                return expected;
            }).Result;

            Assert.AreEqual<int>(expected, x);
        }


        [TestMethod]
        public void OnExceptionAlternativeTest() {
            int alternate = 20;
            CircuitBreaker<int> cb = new CircuitBreaker<int>().Alternate(() => {
                return alternate;
            });

            int x = cb.Invoke(() => {
                throw new DivideByZeroException();
            }).Result;

            Assert.AreEqual<int>(alternate, x);
        }

        [TestMethod]
        public void OnExceptionHandlerCallTest() {
            int alternate = 20;

            bool handledException = false;

            CircuitBreaker<int> cb = new CircuitBreaker<int>().Alternate(() => {
                return alternate;
            }).OnExceptions((ie) => {
                foreach (Exception exp in ie) {
                    if (exp is DivideByZeroException) {
                        handledException = true;
                        break;
                    }
                }
            });

            int x = cb.Invoke(() => {
                throw new DivideByZeroException();
            }).Result;

            Assert.IsTrue(handledException);
        }

        [TestMethod]
        public void OnCloseAlternateTest() {
            int alternate = 20;
            CircuitBreaker<int> cb = new CircuitBreaker<int>().Alternate(() => {
                return alternate;
            });

            cb.Close();

            int x = cb.Invoke(() => {
                return 10;
            }).Result;

            Assert.AreEqual<int>(alternate, x);
        }

        [TestMethod]
        public void OnTimeoutAlternateTest() {
            int alternate = 20;
            CircuitBreaker<int> cb = new CircuitBreaker<int>().Alternate(() => {
                return alternate;
            }).WithinTime(new TimeSpan(0, 0, 0, 0));
                        
            int x = cb.Invoke(() => {
                Thread.Sleep(new TimeSpan(0, 0, 0, 2));
                return 10;
            }).Result;

            Assert.AreEqual<int>(alternate, x);
        }
    }
}