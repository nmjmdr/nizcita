using System.Collections.Generic;

namespace Nizcita {
    public interface ICircularBuffer<T> {
        void Put(T item);
        T[] Read();
    }
}