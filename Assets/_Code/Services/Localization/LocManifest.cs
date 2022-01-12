using BeauData;
using UnityEngine;

namespace Aqua
{
    public class LocManifest : ScriptableObject
    {
        #region Inspector

        public FourCC CharacterCode;
        public LocPackage[] Packages;

        #endregion // Inspector
    }
}