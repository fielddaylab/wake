using UnityEngine;

namespace Aqua
{
    public class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Input.multiTouchEnabled = false;
        }
    }
}