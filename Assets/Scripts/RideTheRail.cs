using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RideTheRail : MonoBehaviour
{
    public Transform[] routes;
    private int routeToGo;
    public float tParam;
    public float maxTParam;
    private Vector3 catPosition;
    public float speedModifier;
    public bool coroutineAllowed;
    public bool enableRotation;
    public bool preventDisappearing;
    public bool stopAt1;
    public Vector3 offset;

    private Vector3 currentRotation, previousRotation;

    private void Start()
    {
        routeToGo = 0;
    }

    private void Update()
    {
        if (coroutineAllowed)
        {
            StartCoroutine(GoByTheRoute(routeToGo));
        }
    }

    private IEnumerator GoByTheRoute(int routeNumber)
    {
        coroutineAllowed = false;

        Vector3 p0 = routes[routeNumber].GetChild(0).position;
        Vector3 p1 = routes[routeNumber].GetChild(1).position;
        Vector3 p2 = routes[routeNumber].GetChild(2).position;
        Vector3 p3 = routes[routeNumber].GetChild(3).position;

        while(tParam < Mathf.Min(1, maxTParam))
        {
            tParam += Time.deltaTime * speedModifier;

            catPosition = Mathf.Pow(1 - tParam, 3) * p0 +
                3 * Mathf.Pow(1 - tParam, 2) * tParam * p1 +
                3 * (1 - tParam) * Mathf.Pow(tParam, 2) * p2 +
                Mathf.Pow(tParam, 3) * p3;

            transform.position = catPosition + offset;

            if(GetComponent<ToWall>() != null || enableRotation)
            {
                // calculate rotation
                currentRotation = transform.position;

                if (previousRotation != Vector3.zero)
                {
                    Vector3 targetDirection = currentRotation - previousRotation;
                    transform.rotation = Quaternion.LookRotation(targetDirection);
                }

                previousRotation = transform.position;
            }

            yield return new WaitForEndOfFrame();
        }

        if(tParam >= maxTParam && stopAt1)
        {
            speedModifier = 0;
        }

        //tParam = 0;
        routeToGo += 1;

        if (routeToGo > routes.Length - 1)
        {
            routeToGo = 0;
            if(!preventDisappearing)
                gameObject.SetActive(false);
        }

        coroutineAllowed = true;
    }
}
