using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointsMove : MonoBehaviour
{
    public Transform target;
    public float speed;
    public List<Vector3> points= new List<Vector3>();
    public bool cyclic;
    [Range(0, 2)]
    public float easeAmount;
    private float nextMoveTime;
    public float waitTime;
    private int fromWaypointIndex;
    private float percentBetweenWaypoints;
    
    private float Ease(float x)
    {
        float a = easeAmount + 1f;
        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
    }

    private void Update()
    {
        Vector3 velocity = CalcMovement();
        target.Translate(velocity);
    }

    private Vector3 CalcMovement()
    {
        if (Time.time < nextMoveTime)
        {
            return Vector3.zero;
        }
        fromWaypointIndex %= points.Count;
        int toWaypointIndex = (fromWaypointIndex + 1) % points.Count;
        float distanceBetweenWaypoints = Vector3.Distance(points[fromWaypointIndex], points[toWaypointIndex]);
        percentBetweenWaypoints += Time.deltaTime * speed / distanceBetweenWaypoints;
        percentBetweenWaypoints = Mathf.Clamp01(percentBetweenWaypoints);
        float easedPercentBetweenWaypoints = Ease(percentBetweenWaypoints);
        Vector3 newPos = Vector3.Lerp(points[fromWaypointIndex], points[toWaypointIndex], easedPercentBetweenWaypoints);
        if (percentBetweenWaypoints >= 1)
        {
            percentBetweenWaypoints = 0f;
            fromWaypointIndex++;

            if (!cyclic)
            {
                if (fromWaypointIndex >= points.Count - 1)
                {
                    fromWaypointIndex = 0;
                    points.Reverse();
                }
            }
            nextMoveTime = Time.time + waitTime;
        }
        return newPos - target.position;
    }
}
