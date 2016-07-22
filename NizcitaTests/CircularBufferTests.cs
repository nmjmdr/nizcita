using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nizcita;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nizcita.Tests {
    [TestClass()]
    public class CircularBufferTests {

        private TestContext testContextInstance;

        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }


        [TestMethod()]
        public void PutWitinLimitsTest() {
            int sz = 6;
            ConcuurentCircularBuffer<int> buf = new ConcuurentCircularBuffer<int>(sz);
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
            ConcuurentCircularBuffer<int> buf = new ConcuurentCircularBuffer<int>(sz);

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
            ConcuurentCircularBuffer<int> buf = new ConcuurentCircularBuffer<int>(sz);

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


        [TestMethod()]
        public void TestParallelPut() {
            int sz = 10;
            ConcuurentCircularBuffer<int> buf = new ConcuurentCircularBuffer<int>(sz);

            Stopwatch watch = new Stopwatch();
            ManualResetEventSlim evt = new ManualResetEventSlim(false);

            int nTimes = 100000;

            Task[] tasks = new Task[nTimes];
            watch.Start();
            for (int i = 0; i < nTimes; i++) {
                tasks[i] = Task.Run(() => {
                    evt.Wait();
                    buf.Put(i);
                });
            }

            // signal all to proceed
            evt.Set();
            Task.WaitAll(tasks);

            watch.Stop();
            double timeMs = watch.Elapsed.TotalMilliseconds;
            TestContext.WriteLine("Test ParallelPut - Buffer size: {0}", sz);
            TestContext.WriteLine("Average time per put: {0} ms, total time taken for {1} parallel requests: {2} ms",timeMs/nTimes,nTimes,timeMs);
            
        }


        [TestMethod()]
        public void TestParallelPutAndRead() {
            int sz = 10;
            ConcuurentCircularBuffer<int> buf = new ConcuurentCircularBuffer<int>(sz);

            Stopwatch watch = new Stopwatch();
            ManualResetEventSlim evt = new ManualResetEventSlim(false);
            ManualResetEventSlim completeEvt = new ManualResetEventSlim(false);
            int nTimes = 100000;

            Task[] tasks = new Task[nTimes];
            watch.Start();
            for (int i = 0; i < nTimes; i++) {
                tasks[i] = Task.Run(() => {
                    evt.Wait();
                    buf.Put(i);
                });
            }

            // start reading, until told to stop
            Task.Run(() => {
                for(;;) {
                    if (completeEvt.IsSet) {
                        break;
                    } else {
                        int[] arr = buf.Read();
                        Assert.IsTrue(arr.Length == sz);
                    }
                }
            });

            // signal all to proceed
            evt.Set();
            Task.WaitAll(tasks);
            completeEvt.Set();

            watch.Stop();
            double timeMs = watch.Elapsed.TotalMilliseconds;
            TestContext.WriteLine("Test ParallelPut and Read - Buffer size: {0}", sz);
            TestContext.WriteLine("Average time per put: {0} ms, total time taken for {1} parallel requests: {2} ms", timeMs / nTimes, nTimes, timeMs);

        }



        [TestMethod()]
        public void TestPutAndNParallelAndRead() {
            int sz = 10;
            ConcuurentCircularBuffer<int> buf = new ConcuurentCircularBuffer<int>(sz);

            Stopwatch watch = new Stopwatch();
            ManualResetEventSlim evt = new ManualResetEventSlim(false);
            ManualResetEventSlim completeEvt = new ManualResetEventSlim(false);
            int nTimes = 1000000;
            int nTasks = 100;

            Task[] tasks = new Task[nTasks];
            watch.Start();
            for (int i = 0; i < nTasks; i++) {
                tasks[i] = Task.Run(() => {
                    evt.Wait();
                    for (int n = 0; n < nTimes / nTasks; n++) {
                        buf.Put(i);
                    }
                });
            }

            // start reading, until told to stop
            Task.Run(() => {
                for (;;) {
                    if (completeEvt.IsSet) {
                        break;
                    } else {
                        int[] arr = buf.Read();
                        Assert.IsTrue(arr.Length == sz);
                    }
                }
            });

            // signal all to proceed
            evt.Set();
            Task.WaitAll(tasks);
            completeEvt.Set();

            watch.Stop();
            double timeMs = watch.Elapsed.TotalMilliseconds;
            TestContext.WriteLine("Test Put and {0} Parallel Put and Read - Buffer size: {1}", nTasks,sz);
            TestContext.WriteLine("Average time per put: {0} ms, total time taken for {1} requests: {2} ms", timeMs / nTimes, nTimes, timeMs);

        }


        [TestMethod()]
        public void TestNParallelPutAndRead() {
            int sz = 10;
            ConcuurentCircularBuffer<int> buf = new ConcuurentCircularBuffer<int>(sz);

            Stopwatch watch = new Stopwatch();
            ManualResetEventSlim evt = new ManualResetEventSlim(false);
            ManualResetEventSlim completeEvt = new ManualResetEventSlim(false);
            int nTimes = 1000000;
            int nTasks = 100;

            Task[] tasks = new Task[nTasks];
            watch.Start();
            for (int i = 0; i < nTasks; i++) {
                tasks[i] = Task.Run(() => {
                    evt.Wait();
                    for (int n = 0; n < nTimes / nTasks; n++) {
                        buf.Put(i);
                    }
                });
            }

            int nParallelReads = 100000;
          
            // start reading, until told to stop
            for (int i= 0;i< nParallelReads; i++) {
                Task.Run(() => {
                    for (;;) {
                        if (completeEvt.IsSet) {
                            break;
                        } else {                           
                            int[] arr = buf.Read();
                            Assert.IsTrue(arr.Length == sz);
                            
                        }
                    }
                });
            }

            // signal all to proceed
            evt.Set();
            Task.WaitAll(tasks);
            completeEvt.Set();

            watch.Stop();
            double timeMs = watch.Elapsed.TotalMilliseconds;
            TestContext.WriteLine("Test NPut and Read in Parallel - Buffer size: {0}", sz);
            TestContext.WriteLine("Average time per put: {0} ms, total time taken for {1} requests: {2} ms", timeMs / nTimes, nTimes, timeMs);            

        }
    }
}