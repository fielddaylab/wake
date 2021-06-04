using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Option
{
    public class OptionsTabToggle : MonoBehaviour, IKeyValuePair<OptionsMenu.PageId, OptionsTabToggle>
    {
        public OptionsMenu.PageId Page = default;
        public Toggle Toggle = null;

        OptionsMenu.PageId IKeyValuePair<OptionsMenu.PageId, OptionsTabToggle>.Key { get { return Page; } }
        OptionsTabToggle IKeyValuePair<OptionsMenu.PageId, OptionsTabToggle>.Value { get { return this; } }
    }
}