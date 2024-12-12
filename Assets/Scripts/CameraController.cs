using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform pov;
    private Vector3 target;

    private void Update()
    {
        target = pov.position;
    }

    private void FixedUpdate()
    {
        transform.position = target;
        transform.forward = pov.forward;
        
    }
}
