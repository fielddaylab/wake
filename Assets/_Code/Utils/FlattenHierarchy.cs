using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public class FlattenHierarchy : MonoBehaviour
    {
        [SerializeField] public bool Recursive = false;
        [SerializeField] public bool DestroyGameObject = false;

        public void Flatten()
        {
            transform.FlattenHierarchy(Recursive);

            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (DestroyGameObject)
                    DestroyImmediate(gameObject);
                else
                    DestroyImmediate(this);
                return;
            }
            #endif // UNITY_EDITOR
            
            if (DestroyGameObject)
                Destroy(gameObject);
            else
                Destroy(this);
        }
    }
}