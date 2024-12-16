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
        DisableVaultButton();
    }
    
    private void OnDisable() {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
}