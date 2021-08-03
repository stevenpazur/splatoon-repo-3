using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquidAnims : MonoBehaviour
{
    public Animator anim;
    public float animSpeedMultiplier = 2f;
    
    // Update is called once per frame
    void Update()
    {
        float speed = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).sqrMagnitude;
        anim.SetFloat("Speed", speed);
        if (speed < 0.4f)
            anim.SetFloat("animSpeed", 1);
        else
            anim.SetFloat("animSpeed", speed * animSpeedMultiplier);
    }
}
