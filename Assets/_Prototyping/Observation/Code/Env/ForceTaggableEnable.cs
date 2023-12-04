using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoAqua.Observation
{
    /// <summary>
    /// Forces taggable to register with taggable system, overriding other systems that might otherwise disable it
    /// </summary>
    public class ForceTaggableEnable : MonoBehaviour
    {
        [SerializeField] private TaggableCritter m_Taggable;

        private void OnEnable()
        {
            m_Taggable.enabled = true;
        }
    }
}