using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableOnCollision : MonoBehaviour
{
    public GameObject[] objectsToEnable;
    public GameObject[] objectsToDisable;


    private void OnCollisionEnter2D(Collision2D collision)
    {
        foreach (var enableObj in objectsToEnable)
        {
            enableObj.SetActive(true);
        }

        foreach (var disableObj in objectsToDisable)
        {
            disableObj.SetActive(false);
        }
    }

}
