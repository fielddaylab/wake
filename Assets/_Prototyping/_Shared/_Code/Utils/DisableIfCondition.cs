using System;
using BeauRoutine;
using BeauUtil.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua
{
    public class DisableIfCondition : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private string m_Condition;
        
        #endregion // Inspector

        private void Awake()
        {
            if (!string.IsNullOrEmpty(m_Condition))
                return;
            
            if (Services.Data.VariableResolver.TryEvaluate(null, m_Condition))
                gameObject.SetActive(false);
        }
    }
}