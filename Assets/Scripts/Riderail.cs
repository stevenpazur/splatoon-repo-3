using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Riderail : MonoBehaviour
{
    public Transform player;
    public Animator animPropeller;
    public GameObject railEnd;
    public GameObject obj;
    public Shader newShader;
    public LineRenderer rail;
    public Material fadedDot;

    private void Start()
    {
        railEnd.SetActive(false);
        rail.GetComponent<MeshCollider>().enabled = false;
        rail.enabled = false;
        animPropeller.enabled = false;
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.GetComponent<ParticlesController>() != null)
        {
            animPropeller.enabled = true;
            var mat = obj.GetComponent<Renderer>().materials[0];
            mat.SetColor("Color_c0976ba64e934aa7a7955d7c042dc531", FindObjectOfType<ToWall>().theColor);
            mat.SetFloat("Boolean_de07954018ae46028aadd5627cbe4d0c", 1);
            railEnd.SetActive(true);
            rail.enabled = true;
            rail.GetComponent<MeshCollider>().enabled = true;
            rail.transform.GetComponent<Renderer>().material.SetColor("_BaseColor", player.GetComponent<ToWall>().theColor);
            var intensity = Mathf.Pow(2, -1);
            Color playerColor = player.GetComponent<ToWall>().theColor;
            Color hdrColor = new Color(playerColor.r * intensity, playerColor.g * intensity, playerColor.b * intensity, 1);
            rail.transform.GetComponent<Renderer>().material.SetColor("_EmissionColor", hdrColor);
            var trails = rail.GetComponentsInChildren<RailTrailAnimate>();
            for(int i = 0; i < trails.Length; i++)
            {
                var renderer = trails[i].GetComponentInChildren<ParticleSystemRenderer>();
                renderer.material = fadedDot;
            }
        }
    }
}
