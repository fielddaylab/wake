using BeauUtil;
using UnityEngine;
using System.Collections;
using Leaf.Runtime;
using BeauRoutine;
using UnityEngine.UI;
using UnityEngine.Scripting;
using System.Collections.Generic;

namespace Aqua.Scripting
{
    [AddComponentMenu("Aqualab/Scripting/Script Group Id")]
    public class ScriptGroupId : ScriptComponent
    {
        #region Inspector

        [SerializeField] private SerializedHash32 m_GroupId = null;

        #endregion // Inspector

        public StringHash32 GroupId {
            get { return m_GroupId; }
        }

        private void OnEnable() {
            AddToGroup(m_GroupId);
        }

        private void OnDisable() {
            RemoveFromGroup(m_GroupId);
        }

        #region Tracking

        static private readonly Dictionary<StringHash32, int> s_Counts = new Dictionary<StringHash32, int>(16);

        static private void AddToGroup(StringHash32 groupId) {
            s_Counts.TryGetValue(groupId, out int current);
            s_Counts[groupId] = current + 1;
        }

        static private void RemoveFromGroup(StringHash32 groupId) {
            if (s_Counts.TryGetValue(groupId, out int current)) {
                s_Counts[groupId] = current - 1;
            }
        }

        #endregion // Tracking

        #region Leaf

        [LeafMember("GroupOf"), Preserve]
        static public StringHash32 GetGroup(ScriptObject scriptObj) {
            ScriptGroupId groupId = scriptObj.GetComponent<ScriptGroupId>();
            if (groupId != null) {
                return groupId.GroupId;
            }
            return StringHash32.Null;
        }

        [LeafMember("IsGroup"), Preserve]
        static public bool IsGroup(ScriptObject scriptObj, StringHash32 targetId) {
            ScriptGroupId groupId = scriptObj.GetComponent<ScriptGroupId>();
            if (groupId != null) {
                return groupId.GroupId == targetId;
            }
            return false;
        }

        [LeafMember("GroupCount"), Preserve]
        static public int GroupCount(StringHash32 targetId) {
            s_Counts.TryGetValue(targetId, out int current);
            return current;
        }

        #endregion // Leaf
    }
}