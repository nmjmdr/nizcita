using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nizcita {
    public class ConcurrentLimitedBuffer<T> : ILimitedBuffer<T> {
        private T[] buffer;
        private int last = 0;
        private int sz;
        private ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

        public ConcurrentLimitedBuffer(int sz) {
            // array index starts at 1
            this.sz = sz;
            buffer = new T[sz + 1];
        }

        public void Put(T item) {
            rwLock.EnterWriteLock();
            try {
                last++;
                last = last > sz ? 1 : last;
                buffer[last] = item;
            } finally {
                rwLock.ExitWriteLock();
            }
        }

        public T[] Read() {
            T[] arr = new T[sz];

            rwLock.EnterReadLock();
            try {
                for (int read = 0; read < sz; read++) {
                    int index = last - read;
                    index = index <= 0 ? (sz + index) : index;
                    if (buffer[index] != null) {
                        arr[read] = buffer[index];
                    } else {
                        break;
                    }
                }
            } finally {
                rwLock.ExitReadLock();
            }
            return arr;
        }
    }
}