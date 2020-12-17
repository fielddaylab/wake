using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoAqua.Ship
{
    public class RoomManager : MonoBehaviour
    {
         public new Camera camera;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            // if (Input.GetMouseButtonDown(0))
            // {
            //     Debug.Log("Mouse is down");

            //     RaycastHit hitInfo;
            //     bool hit = Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out hitInfo);
            //     if (hit)
            //     {
            //         Debug.Log("Hit " + hitInfo.transform.gameObject.name);
            //         if (hitInfo.transform.gameObject.tag == "Construction")
            //         {
            //             Debug.Log("It's working!");
            //         }
            //         else
            //         {
            //             Debug.Log("nopz");
            //         }
            //     }
            //     else
            //     {
            //         Debug.Log("No hit");
            //     }
            //     Debug.Log("Mouse is down");
            // }
        }
    }
}