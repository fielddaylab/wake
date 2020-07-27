using BeauData;

namespace ProtoAqua
{
    public interface IService
    {
        FourCC ServiceId();
        void OnRegisterService();
        void OnDeregisterService();
    }
}