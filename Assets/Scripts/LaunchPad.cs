using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using DG.Tweening;

public class LaunchPad : MonoBehaviour
{
    public Transform player, blob, blob2;
    public ParticleSystem particles;
    public CinemachineDollyCart cart;
    public CinemachineSmoothPath track;
    public ToWall movementScript;
    public float flightSpeed = 0.25f;
    public Animator inklingAnim;

    private bool jumping = false;

    private void Start()
    {
        var emission = particles.colorOverLifetime;
        var gradient = new Gradient();

        var alphas = new GradientAlphaKey[4];
        alphas[0].alpha = 0;
        alphas[0].time = 0;
        alphas[1].alpha = 1;
        alphas[1].time = 0.395f;
        alphas[2].alpha = 1;
        alphas[2].time = 0.812f;
        alphas[3].alpha = 0;
        alphas[3].time = 1;

        var colors = new GradientColorKey[2];
        colors[0].color = movementScript.theColor;
        colors[0].time = 0;
        colors[1].color = movementScript.theColor;
        colors[1].time = 1;

        gradient.SetKeys(colors, alphas);
        emission.color = gradient;
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.gameObject.tag == "Player" && Input.GetButton("Fire2") && !jumping)
        {
            jumping = true;
            StartCoroutine(startJumping());
            movementScript.launcher = this;
        }
    }

    private IEnumerator startJumping()
    {
        movementScript.GetComponent<ShootingSystem>().canShoot = false;
        Vector3 lookDir = track.m_Waypoints[track.m_Waypoints.Length - 1].position;
        lookDir.y = 180;
        blob.gameObject.SetActive(true);
        yield return null;
        //movementScript.enabled = false;
        yield return new WaitForSeconds(0.05f);
        player.parent = cart.transform;
        for(float i = 0; i < 1; i += 0.25f)
        {
            player.position = Vector3.Lerp(player.position, transform.position, i);
            yield return null;
            yield return null;
        }
        blob.rotation = Quaternion.Euler(lookDir);
        blob2.rotation = Quaternion.Euler(lookDir + new Vector3(-90, 180, 0));
        movementScript.squid.SetActive(false);
        movementScript.outfit.SetActive(false);
        movementScript.gun.SetActive(false);
        yield return new WaitForSeconds(1);
        blob.gameObject.SetActive(false);
        //blob2.gameObject.SetActive(true);
        blob2.GetComponentInChildren<MeshRenderer>().enabled = true;
        while (cart.m_Position < 0.99f)
        {
            if (cart.m_Position > 0.5f)
            {
                //blob2.gameObject.SetActive(false);
                blob2.GetComponentInChildren<MeshRenderer>().enabled = false;
                movementScript.model.SetActive(true);
                movementScript.gun.SetActive(true);
                movementScript.outfit.SetActive(true);
                movementScript.InkMeUpBaby(true);
            }
            cart.m_Position += flightSpeed * Time.deltaTime;
            yield return null;
        }
        jumping = false;
        //movementScript.enabled = true;
        player.parent = null;
        player.rotation = Quaternion.Euler(Vector3.up);
        cart.m_Position = 0;
        //blob2.gameObject.SetActive(false);
        blob2.GetComponentInChildren<MeshRenderer>().enabled = false;
        movementScript.launcher = null;
        movementScript.GetComponent<ShootingSystem>().canShoot = true;
    }
}
