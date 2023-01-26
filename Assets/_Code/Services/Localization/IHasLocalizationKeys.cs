using BeauUtil;
using System.Collections.Generic;

namespace Aqua
{
    public interface IHasLocalizationKeys {
        IEnumerable<KeyValuePair<StringHash32, string>> GetStrings();
    }
}