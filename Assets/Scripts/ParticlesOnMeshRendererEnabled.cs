using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlesOnMeshRendererEnabled : MonoBehaviour
{
    public ParticleSystem inkTrail;

    private void Update()
    {
        var emi = inkTrail.emission;
        ParticleSystem[] emiChildren = inkTrail.gameObject.GetComponentsInChildren<ParticleSystem>();
        if (!GetComponent<Renderer>().enabled)
        {
            emi.rateOverDistance = 0;
            for(int i = 0; i < emiChildren.Length; i++)
            {
                var newEmi = emiChildren[i].emission;
                newEmi.rateOverDistance = 0;
            }
        }
        else
        {
            emi.rateOverDistance = 50;
            for (int i = 0; i < emiChildren.Length; i++)
            {
                var newEmi = emiChildren[i].emission;
                newEmi.rateOverDistance = 50;
            }
        }
    }
}
