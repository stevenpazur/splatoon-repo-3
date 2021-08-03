using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CheckNearbyCollisions : MonoBehaviour
{
    public Animator anim;

    private bool isRiding = false;
    private Riderail lastRail;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if(hit.gameObject.GetComponent<Riderail>() != null && hit.gameObject.GetComponent<LineRenderer>() != null)
        {
            GetNearbyCollisionPoint(hit.gameObject.GetComponent<Riderail>());
        }
        if(hit.gameObject.GetComponentInParent<Riderail>() != null && hit.gameObject.GetComponent<LineRenderer>() != null)
        {
            GetNearbyCollisionPoint(hit.gameObject.GetComponentInParent<Riderail>());
        }
    }

    void GetNearbyCollisionPoint(Riderail rail)
    {
        if (!isRiding)
        {
            lastRail = rail;
            isRiding = true;
            Transform[] points = new Transform[rail.GetComponentInChildren<RailContactPoints>().transform.childCount];
            float[] distances = new float[rail.GetComponentInChildren<RailContactPoints>().transform.childCount];
            for (int i = 0; i < rail.GetComponentInChildren<RailContactPoints>().transform.childCount; i++)
            {
                points[i] = rail.GetComponentInChildren<RailContactPoints>().transform.GetChild(i);
                distances[i] = Vector3.Distance(transform.position, rail.GetComponentInChildren<RailContactPoints>().transform.GetChild(i).position);
            }
            float min = distances[0];
            Transform minPoint = points[0];
            int index = 0;
            for (int j = 0; j < points.Length; j++)
            {
                if (distances[j] < min)
                {
                    min = distances[j];
                    minPoint = points[j];
                    index = j;
                }
            }
            transform.GetComponent<ToWall>().enabled = false;
            transform.GetComponent<RideTheRail>().enabled = true;
            transform.GetComponent<RideTheRail>().routes = minPoint.GetComponent<RideTheRail>().routes;
            transform.GetComponent<RideTheRail>().tParam = minPoint.GetComponent<RideTheRail>().tParam;
            transform.GetComponent<RideTheRail>().coroutineAllowed = true;
        }
    }

    private void Update()
    {
        //anim.enabled = !isRiding;
        anim.SetBool("OnRideRail", isRiding);
        if(GetComponent<RideTheRail>().tParam >= GetComponent<RideTheRail>().maxTParam && isRiding)
        {
            GetComponent<RideTheRail>().enabled = false;
            GetComponent<ToWall>().enabled = true;
            StartCoroutine(MoveMe(8, lastRail));
            isRiding = false;
        }

        if (isRiding)
        {
            if (Input.GetButtonDown("Jump"))
            {
                StartCoroutine(JumpMe(10));
            }
        }
    }

    private IEnumerator MoveMe(int frames, Riderail rail)
    {
        yield return null;
        for(int i = 0; i < frames; i++)
        {
            yield return null;
            Transform lastChild = lastRail.GetComponentInChildren<RailContactPoints>().transform.GetChild(lastRail.GetComponentInChildren<RailContactPoints>().transform.childCount - 2);
            Transform childBeforeLastChild = lastRail.GetComponentInChildren<RailContactPoints>().transform.GetChild(lastRail.GetComponentInChildren<RailContactPoints>().transform.childCount - 1);
            Vector3 forwardDirection = childBeforeLastChild.position - lastChild.position;
            GetComponent<CharacterController>().Move(2 * (transform.up + forwardDirection));
        }
    }

    private IEnumerator JumpMe(int frames)
    {
        for(int i = 0; i < frames; i++)
        {
            yield return null;
            GetComponent<RideTheRail>().offset.y += 0.1f;
        }
    }
}
