using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeColorScheme : MonoBehaviour

{

    public int colorScheme;
    public float totalSchemes = 16;
    Renderer rend;

    // Start is called before the first frame update
    void Start()
    {
        float schemeFraction = (1 / totalSchemes)*colorScheme;
        rend = GetComponent<Renderer>();
        rend.material.SetTextureOffset("_MainTex", new Vector2(schemeFraction, 0));

    }

}
