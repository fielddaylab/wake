using BeauData;
using BeauUtil;

namespace ProtoAqua.Energy
{
    public interface ISimType<T> where T : ISimType<T>
    {
        FourCC Id();
        string ScriptName();
        PropertyBlock ExtraData();

        void Hook(SimTypeDatabase<T> inDatabase);
        void Unhook(SimTypeDatabase<T> inDatabase);
    }
}