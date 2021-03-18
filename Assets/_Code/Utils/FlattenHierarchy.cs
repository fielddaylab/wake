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

            if (DestroyGameObject)
                Destroy(gameObject);
            else
                Destroy(this);
        }
    }
}