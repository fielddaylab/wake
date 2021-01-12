using System.Collections;
using System.Collections.Generic;
using Aqua;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProtoAqua.Argumentation {
    public class RestartScene : MonoBehaviour {
        
        public void Restart() {
            Services.State.ReloadCurrentScene();
        }
    }
}
