using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nizcita {
    public class CircularBuffer<T> : ICircularBuffer<T> {
        private T[] buffer;
        private int last = 0;
        private int sz;
       

        public CircularBuffer(int sz) {
            // array index starts at 1
            this.sz = sz;
            buffer = new T[sz+1];
        }

        /// <summary>
        /// The values in the array could be null 
        /// it is responsiblity of the callee to check for null when the array is iterated
        /// </summary>
        /// <returns></returns>
        public T[] Read() {
            T[] arr = new T[sz];            

            int iterator = 0;            
            for(int read = 0;read < sz; read++) {
                int index = last - iterator;
                index = index <= 0 ? (sz + index) : index;
                if (buffer[index] != null) {
                    arr[iterator] = buffer[index];
                } else {
                    break;
                }
                iterator++;
            }

            return arr;           
        }

        public void Put(T item) {
            last++;
            last = last > sz ? 1 : last;
            buffer[last] = item;
        }
    }
}
