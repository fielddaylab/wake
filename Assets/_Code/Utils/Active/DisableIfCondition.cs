using System;
using BeauRoutine;
using BeauUtil.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class DisableIfCondition : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private string m_Condition = null;
        
        #endregion // Inspector

        private void Awake()
        {
            if (string.IsNullOrEmpty(m_Condition)) {
                return;
            }

            if (Services.Data.CheckConditions(m_Condition)) {
                gameObject.SetActive(false);
            }
        }
    }
}