using BeauUtil;
using BeauUtil.Tags;

namespace Aqua
{
    /// <summary>
    /// Record of displayed dialog.
    /// </summary>
    public struct DialogRecord
    {
        public StringHash32 CharacterId;
        public string Name;
        public string Text;
        public bool IsBoundary;

        static public DialogRecord FromTag(TagString inTag, StringHash32 inDefaultCharacterId, string inDefaultName, bool inbBoundary)
        {
            DialogRecord record;
            if (!ScriptingService.TryFindCharacter(inTag, out record.CharacterId, out record.Name))
            {
                record.CharacterId = inDefaultCharacterId;
                record.Name = inDefaultName;
            }
            record.Text = inTag.RichText;
            record.IsBoundary = inbBoundary;
            return record;
        }

        public string ToDebugString()
        {
            if (CharacterId.IsEmpty)
            {
                if (string.IsNullOrEmpty(Name))
                    return Text;
                return string.Format("{0}: {1}", Name, Text);
            }
            return string.Format("@{0} / {1}: {2}", CharacterId.ToDebugString(), Name, Text);
        }
    }
}