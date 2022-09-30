using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingZone : MonoBehaviour
{
    public Renderer rendForTextureSwap;
    public float transitionTime;
    public int materialSlot = 0;
    private bool transitionMaterials = false;
    private float elapsedTime;
    private float lerpValue;


    private void Start()
    {
        if (rendForTextureSwap)
        {
            rendForTextureSwap.sharedMaterials[materialSlot].SetFloat("textureBlend", 0);
        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (rendForTextureSwap)
            {
                transitionMaterials = true;
            }

        }
    }


    private void Update()
    {
        if (transitionMaterials)
        {

            if (elapsedTime < transitionTime)
            {
                lerpValue = Mathf.Lerp(0, 1, elapsedTime / transitionTime);
                elapsedTime += Time.deltaTime;
            }
            else
            {
                rendForTextureSwap.sharedMaterials[materialSlot].SetFloat("textureBlend", 1);
            }
            rendForTextureSwap.sharedMaterials[materialSlot].SetFloat("textureBlend", lerpValue);


        }
    }
}
