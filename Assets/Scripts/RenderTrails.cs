using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderTrails : MonoBehaviour
{
    public float tParamLength;
    public bool coroutinesAllowed = true;
    private bool releaseTheKraken = false;
    public Transform[] children;
    public float delay;

    // Start is called before the first frame update
    void Start()
    {
        children = new Transform[transform.childCount];
        for(int i = 0; i < transform.childCount; i++)
        {
            children[i] = transform.GetChild(i);
        }

        StartCoroutine(DelayStart(delay));
    }

    public void MakeATrail()
    {
        if (releaseTheKraken)
        {
            for (int j = 0; j < children.Length; j++)
            {
                children[j].GetComponent<RideTheRail>().coroutineAllowed = true;
            }

            StartCoroutine(StartAgain());
        }
    }

    private IEnumerator StartAgain()
    {
        yield return new WaitForSeconds(3f);
        if (coroutinesAllowed)
        {
            for (int j = 0; j < children.Length; j++)
            {
                children[j].gameObject.SetActive(false);
                children[j].GetComponent<RideTheRail>().coroutineAllowed = false;
                children[j].GetComponent<RideTheRail>().tParam = j * 0.2f;
                children[j].gameObject.SetActive(true);
                children[j].GetComponent<RideTheRail>().coroutineAllowed = true;
            }
            MakeATrail();
        }
    }

    private IEnumerator DelayStart(float delay)
    {
        yield return new WaitForSeconds(delay);
        releaseTheKraken = true;
        MakeATrail();
    }
}
