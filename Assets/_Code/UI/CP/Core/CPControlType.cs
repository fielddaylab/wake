using BeauData;

namespace ProtoCP
{
    public class CPControlType
    {
        // prevent instantiation
        private CPControlType() { }

        #region Shared Interface

        /// <summary>
        /// Registers an id as a service id.
        /// </summary>
        static public FourCC Register(string inId, string inName = null, string inDescription = null)
        {
            return FourCC.Register(typeof(CPControl), inId, inName, inDescription);
        }

        #endregion // Shared Interface

        static public readonly FourCC Enum = Register("ENUM", "Enum Spinner");
        static public readonly FourCC Header = Register("HEAD", "Group Header");
        static public readonly FourCC Spinner = Register("NSPN", "Numeric Spinner");
        static public readonly FourCC TextField = Register("TXTF", "Text Entry");
        static public readonly FourCC Toggle = Register("TOGL", "Bool Toggle");
        static public readonly FourCC Label = Register("LABL", "Label");
    }
}