using BeauData;

namespace Aqua
{
    public interface IService
    {
        FourCC ServiceId();
        bool IsLoading();
        void OnRegisterService();
        void AfterRegisterService();
        void OnDeregisterService();
    }
}