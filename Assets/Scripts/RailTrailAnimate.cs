using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailTrailAnimate : MonoBehaviour
{
    public Transform trail;
    public float rotateSpeed = 5f;

    // Update is called once per frame
    void Update()
    {
        trail.RotateAround(transform.position, transform.up, rotateSpeed);
    }
}
