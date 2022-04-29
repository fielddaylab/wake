using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class v1ctorCursorFollow : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject v1ctorHead;
    public GameObject sprite;
    public Vector2 spriteOffsetLimit;
    public float lookSpeed = 5f;
    private Material spriteMaterial;
    private Vector2 spriteOffset;
    private Vector3 lookatPostion;

    void Start()
    {
        spriteMaterial = sprite.GetComponent<SpriteRenderer>().material;
    }

    void Update()
    {
        spriteMaterial.SetVector("_spriteOffset", spriteOffset);

        Vector3 mousePos = -Input.mousePosition;
        Vector2 rawOffset = new Vector2(mousePos.x / Screen.width + 0.5f, mousePos.y / Screen.height + 0.5f);
        Vector2 targetOffset = rawOffset * spriteOffsetLimit;

        spriteOffset = Vector2.Lerp(spriteOffset, targetOffset, Time.deltaTime * lookSpeed);

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit))
        {
            v1ctorHead.transform.LookAt(raycastHit.point);
        }

    }
}
