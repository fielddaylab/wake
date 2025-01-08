using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;

public class VaultDropdownToggle : MonoBehaviour {
    public string sceneToDisplay = "Title";

    [DllImport("__Internal")]
    private static extern void DisableVaultButton();

    private void OnEnable() {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene current) {
        if(current.name != sceneToDisplay) return;
        Debug.Log("[VaultDropdownToggle] remove vault button");
#if UNITY_WEBGL && !UNITY_EDITOR
        DisableVaultButton();
#endif
    }
    
    private void OnDisable() {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
}
