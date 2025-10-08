using System.Text;
using BeauUtil;
using BeauUtil.Tags;

namespace Aqua
{
    /// <summary>
    /// Record of displayed dialog.
    /// </summary>
    public sealed class DialogRecord : IDebugString
    {
        public StringHash32 CharacterId;
        public string Name;
        public string Text;
        public bool IsBoundary;
        public bool IsChoice;

        static public void FromTag(ref DialogRecord record, TagString inTag, StringHash32 inDefaultCharacterId, string inDefaultName, bool inbBoundary, bool inbChoice)
        {
            if (!ScriptingService.TryFindCharacter(inTag, out record.CharacterId, out record.Name))
            {
                record.CharacterId = inDefaultCharacterId;
                record.Name = inDefaultName;
            }
            record.Text = inTag.RichText;
            record.IsBoundary = inbBoundary;
            record.IsChoice = inbChoice;
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