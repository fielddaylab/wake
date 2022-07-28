using BeauUtil;
using UnityEngine;
using Leaf.Runtime;

namespace Aqua.Scripting {
    [AddComponentMenu("Aqualab/Scripting/Script Animator")]
    public class ScriptAnimator : ScriptComponent
    {
        [SerializeField, Required] private Animator m_Animator = null;

        [LeafMember]
        public void SetAnimInt(string paramName, int paramValue) {
            m_Animator.SetInteger(paramName, paramValue);
        }

        [LeafMember]
        public void SetAnimBool(string paramName, bool paramValue) {
            m_Animator.SetBool(paramName, paramValue);
        }

        [LeafMember]
        public void SetAnimFloat(string paramName, float paramValue) {
            m_Animator.SetFloat(paramName, paramValue);
        }

        [LeafMember]
        public void SetAnimTrigger(string paramName) {
            m_Animator.SetTrigger(paramName);
        }

        [LeafMember]
        public void ResetAnimTrigger(string paramName) {
            m_Animator.ResetTrigger(paramName);
        }

        [LeafMember]
        public void PlayAnim(string stateName) {
            m_Animator.Play(stateName);
        }

        [LeafMember]
        public void FFAnim() {
            var stateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
            m_Animator.Play(stateInfo.fullPathHash, 0, 1);
        }

        [LeafMember]
        public void RestartAnim() {
            var stateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
            m_Animator.Play(stateInfo.fullPathHash, 0, 0);
        }

        #region IScriptComponent

        public override void OnDeregister(ScriptObject inObject)
        {
            base.OnDeregister(inObject);
        }

        public override void OnRegister(ScriptObject inObject)
        {
            base.OnRegister(inObject);
        }

        #endregion // IScriptComponent
    }
}