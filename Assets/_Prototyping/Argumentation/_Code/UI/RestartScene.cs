using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProtoAqua.Argumentation {
    public class RestartScene : MonoBehaviour {
        
        public void Restart() {
            SceneManager.LoadScene("ArgumentationScene");
        }
    }
}
