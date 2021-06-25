namespace Aqua
{
    public interface ITimeHandler
    {
        void OnTimeChanged(GTDate inGameTime);
        TimeEvent EventMask();
    }
}