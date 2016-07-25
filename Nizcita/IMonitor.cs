namespace Nizcita {
    public interface IMonitor {
        void Listen(AlarmHandler alarmHandler);
        void Log(Point p);
    }
}