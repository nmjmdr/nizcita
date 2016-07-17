using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nizcita;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nizcita.Tests {
    [TestClass()]
    public class CircularBufferTests {
              

        [TestMethod()]
        public void PutWitinLimitsTest() {
            int sz = 6;
            CircularBuffer<int> buf = new CircularBuffer<int>(sz);
            for (int i = 1; i <= sz; i++) {
                buf.Put(i);
            }

            IEnumerable<int> items = buf.Read();

            Assert.IsNotNull(items);
            Assert.AreEqual<int>(sz, items.Count());

            int actualI = sz;
            foreach(int item in items) {
                Assert.AreEqual<int>(actualI, item);
                actualI--;
            }
        }


        [TestMethod()]
        public void PutOverLimitsTest() {
            int sz = 6;
            CircularBuffer<int> buf = new CircularBuffer<int>(sz);

            for (int i = 1; i <= sz+2; i++) {
                buf.Put(i);
            }

            IEnumerable<int> items = buf.Read();

            Assert.IsNotNull(items);
            Assert.AreEqual<int>(sz, items.Count());

            int actualI = sz+2;
            foreach (int item in items) {
                Assert.AreEqual<int>(actualI, item);
                actualI--;
            }
        }


        [TestMethod()]
        public void PutCompleteOverwriteTest() {
            int sz = 6;
            CircularBuffer<int> buf = new CircularBuffer<int>(sz);

            for (int i = 1; i <= sz; i++) {
                buf.Put(i);
            }

            for (int i = 10; i <= (sz+10+2); i++) {
                buf.Put(i);
            }

            IEnumerable<int> items = buf.Read();

            Assert.IsNotNull(items);
            Assert.AreEqual<int>(sz, items.Count());

            int actualI = (sz + 10+2);
            foreach (int item in items) {
                Assert.AreEqual<int>(actualI, item);
                actualI--;
            }
        }
    }
}