namespace Nizcita {
    public interface ILimitedBuffer<T> {
        T[] Read();
        void Put(T item);
    }
}