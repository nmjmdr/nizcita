using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nizcita;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nizcita.Tests {
    [TestClass()]
    public class CircuitBreakerTests {
        
        [TestMethod]
        public void InvokedTest() {
            CircuitBreaker<int> cb = new CircuitBreaker<int>();

            int expected = 10;
            int x = cb.Invoke(() => {
                return expected;
            }).Result;

            Assert.AreEqual<int>(x, expected);
        }

        [TestMethod]
        public void InvokedConditionalReturnTest() {

            int original = 10;
            int conditionallyReturned = 20;
            CircuitBreaker<int> cb = new CircuitBreaker<int>().OnResult((r) => {
                return r == original;
            }).Return(() => {
                return conditionallyReturned;
            });

            
            int x = cb.Invoke(() => {
                return original;
            }).Result;

            Assert.AreEqual<int>(x, conditionallyReturned);
        }

        [TestMethod]
        public void OnExceptionHandledTest() {
                        
            int conditionallyReturned = 20;
            CircuitBreaker<int> cb = new CircuitBreaker<int>();

            int x = cb.Invoke(() => {
                throw new DivideByZeroException();
            }).Result;

            Assert.AreEqual<int>(x, conditionallyReturned);
        }


        [TestMethod]
        public void InvokedConditionalReturnChainTest() {

            int original = 10;
            int conditionallyReturned = 20;
            CircuitBreaker<int> cb = new CircuitBreaker<int>().OnResult((r) => {
                return r == 5;
            }).Return(() => {
                return 100;
            }).OnResult((r) => {
                return r == original;
            }).Return(() => {
                return conditionallyReturned;
            });


            int x = cb.Invoke(() => {
                return original;
            }).Result;

            Assert.AreEqual<int>(x, conditionallyReturned);
        }

        [TestMethod]
        public void AlternativeOnTimeoutTest() {
            int original = 10;
            int alternate = 100;
            CircuitBreaker<int> cb = new CircuitBreaker<int>().WithinTime(new TimeSpan(0, 0, 0, 0, 0)).Alternate(() => {
                return alternate;
            });


            int x = cb.Invoke(() => {
                System.Threading.Thread.Sleep(new TimeSpan(0, 0, 0, 2));
                return original;
            }).Result;

            Assert.AreEqual<int>(x, alternate);
        }
    }
}