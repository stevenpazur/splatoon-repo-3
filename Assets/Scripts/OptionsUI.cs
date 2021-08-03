using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionsUI : MonoBehaviour
{
    public GameObject waveUI;
    public float waveMoveSpeed = 1;
    public float waveResetTimer = 7;

    private Vector3 waveStartingPos;

    // Start is called before the first frame update
    void Start()
    {
        waveStartingPos = waveUI.transform.position;
        InvokeRepeating("resetWaveImagePos", 0, waveResetTimer);
    }

    // Update is called once per frame
    void Update()
    {
        waveUI.transform.Translate(new Vector3(waveMoveSpeed, 0, 0));
    }

    private void resetWaveImagePos()
    {
        waveUI.transform.position = waveStartingPos;
    }
}
