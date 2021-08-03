using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamColorMaterial : MonoBehaviour
{
    public Material mat;
    public Material inkMat;

    private void Start()
    {
        mat.SetColor("_BaseColor", inkMat.GetColor("_BaseColor"));
    }
}
