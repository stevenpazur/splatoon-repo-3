using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailContactPoints : MonoBehaviour
{
    private Transform[] points;

    private void Start()
    {
        points = new Transform[transform.childCount];
        for(int i = 0; i < points.Length; i++)
        {
            points[i] = transform.GetChild(i);
            points[i].GetComponent<RideTheRail>().tParam = (0.975f * i) / (float)(transform.childCount - 1);
        }
    }
}
