using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nizcita {
    public class CircularBuffer<T> {
        private T[] buffer;
        private int last = 0;
        private int sz;
        private int count = 0;

        public CircularBuffer(int sz) {
            // array index starts at 1
            this.sz = sz;
            buffer = new T[sz+1];
        }

        public IEnumerable<T> Read() {

            int iterator = 0;            
            for(int read = 0;read < count; read++) {
                int index = last - iterator;
                index = index <= 0 ? (count + index) : index;                
                yield return buffer[index];
                iterator++;
            }           
        }

        public void Put(T item) {
            last++;
            last = last > sz ? 1 : last;
            buffer[last] = item;

            count++;
            count = count > sz ? sz : count;
        }
    }
}
