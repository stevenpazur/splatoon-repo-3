using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangePaintColor : MonoBehaviour
{
    public Material paintStream;
    public ParticlesController particleObject;

    public void PaintColorChanger(Color color)
    {
        paintStream.SetColor("_BaseColor", color);
        particleObject.paintColor = color;
    }

    private void Start()
    {
        paintStream.SetColor("_BaseColor", new Color(1, 0, 0.25f, 1));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            PaintColorChanger(Color.green);
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            PaintColorChanger(Color.blue);
        }
    }
}
