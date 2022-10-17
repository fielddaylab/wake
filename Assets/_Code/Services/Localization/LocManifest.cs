using System.Collections;
using System.IO;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Localization Manifest")]
    public class LocManifest : ScriptableObject
    {
        #region Inspector

        public FourCC LanguageId;
        public LocPackage[] Packages;

        #endregion // Inspector
    }
}