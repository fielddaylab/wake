using BeauUtil.Variants;

namespace Aqua.Scripting
{
    public interface IScriptContext
    {
        ScriptObject Object { get; }
        VariantTable Vars { get; }
    }
}