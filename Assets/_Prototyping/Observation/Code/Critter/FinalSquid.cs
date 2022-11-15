using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinalSquid : MonoBehaviour
{
    private Transform playerPos;
    private GameObject[] player;
    public float chaseSpeed;
    public float nearDistance = 1;
    public float farDistance;
    public bool chasePlayer = false;
    private Vector3 chaseMove;

    // This is a temp script for working on squid animations and gameplay timing  

    void Start()
    {
        player = GameObject.FindGameObjectsWithTag("Player");
        if (player[0])
        {
            playerPos = player[0].transform;
        }
    }


    void Update()
    {
        if (chasePlayer)
        {

 
            Vector3 moveDir = playerPos.position - transform.position;
            transform.LookAt(playerPos);
            Vector3 moveDirNormalized = moveDir;


            if (moveDir.magnitude > farDistance)
            {
                moveDirNormalized.Normalize();
                transform.position = transform.position + ((chaseSpeed *2) * Time.deltaTime * moveDirNormalized);
            }

            else if (moveDir.magnitude <= farDistance && moveDir.magnitude > nearDistance)
            {
                moveDirNormalized.Normalize();
                transform.position = transform.position + (chaseSpeed * Time.deltaTime * moveDirNormalized);
            }

        }

    }
}
