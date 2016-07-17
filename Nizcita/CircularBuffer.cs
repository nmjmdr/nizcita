using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nizcita {
    public class CircularBuffer<T> {
        private T[] buffer;
        private int last = 0;

        public CircularBuffer(int sz) {
            buffer = new T[sz];
        }

        public IEnumerable<T> ReadAll() {

            return null;
        }

        public void Put(T item) {
            last++;

        }
    }
}
