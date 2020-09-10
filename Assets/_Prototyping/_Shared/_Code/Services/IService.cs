using BeauData;

namespace ProtoAqua
{
    public interface IService
    {
        FourCC ServiceId();
        bool IsLoading();
        void OnRegisterService();
        void OnDeregisterService();
    }
}