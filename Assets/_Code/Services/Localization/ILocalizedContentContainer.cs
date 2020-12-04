using System.Collections.Generic;

namespace Aqua
{
    public interface ILocalizedContentContainer
    {
        IEnumerable<KeyValuePair<string, string>> ExportContent();
    }
}