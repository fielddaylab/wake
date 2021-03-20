#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using UnityEngine;

namespace Aqua
{
    public class ServiceInitializer : MonoBehaviour
    {
        private void Awake()
        {
            Services.AutoSetup(gameObject);
        }
    }
}