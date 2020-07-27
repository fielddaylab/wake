using UnityEngine;

namespace ProtoAqua
{
    public class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Input.multiTouchEnabled = false;
            Services.AutoSetup(gameObject);
        }
    }
}