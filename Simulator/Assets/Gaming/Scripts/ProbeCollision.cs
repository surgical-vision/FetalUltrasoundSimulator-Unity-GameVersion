using System.Collections;

using System.Collections.Generic;

using UnityEngine;

 

public class ProbeCollision : MonoBehaviour
{
    public float force = 1f;
    public float collisionThreshold;
    private SocketClient socket;
    public float forceOffset = 0.1f;

    void Start()
    {
        socket = GameObject.FindGameObjectWithTag("Client").GetComponent<SocketClient>();
    }

    void Update()
    {
        CheckCollision();
    }


    void CheckCollision()
    {
        Vector3 rayOrigin = transform.position;
        // set offset from the origin point
        rayOrigin.y += 0.08f;   // originally 0.08
        rayOrigin.z += 0.05f;
        //collisionThreshold = (transform.localScale.y / 2f) + (float)0.01;

        RaycastHit hit; // made more explicit CHANGE IF BROKEN
        Ray landingRay = new Ray(rayOrigin, -transform.forward);

        Debug.DrawRay(rayOrigin, -transform.forward * collisionThreshold, Color.red);

        if (Physics.Raycast(landingRay, out hit, collisionThreshold))
        {
            if (hit.collider.gameObject.tag == "Abdominal")
            {
                float dist = collisionThreshold - Vector3.Distance(rayOrigin, hit.point);
                float hapticDist = (collisionThreshold - hit.distance);
                socket.SendData(hapticDist);
                socket.touchBelly = true;
                //Debug.Log("dist = " + hapticDist);
            }
            else
            {
                socket.SendData(0);
            }
        }
        else
        {
            socket.SendData(0);
            socket.touchBelly = false;

        }

    }

}