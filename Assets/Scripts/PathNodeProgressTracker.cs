using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNodeProgressTracker : MonoBehaviour
{
    CarAI ai;

    List<Vector3> waypoints;
    [SerializeField] int substeps = 10;

    Rigidbody rb;

    [SerializeField] int pathNodesToShow = 4;
    [SerializeField] float lookAheadMinDistance = 1f;
    [SerializeField] float lookAheadMaxDistance = 10f;
    [SerializeField] float lookAheadSpeedModifier = 0.05f;

    public Vector3 target;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (waypoints.Count > 0)
        {
            float distanceToTarget = float.MaxValue;
            int targetIndex = 0;

            float speed = rb.velocity.magnitude * 3.6f;
            speed = Mathf.Max(1, speed);

            float minDist = lookAheadMinDistance * speed * lookAheadSpeedModifier;
            float maxDist = lookAheadMaxDistance * speed * lookAheadSpeedModifier;

            //find closest node infront of the car that is within acceptable distances
            for (int i = 0; i < waypoints.Count; i++)
            {
                float testDistance = Vector3.Distance(rb.position, waypoints[i]);
                if (testDistance < distanceToTarget)
                {
                    if (Vector3.Distance(waypoints[i], rb.position + transform.forward) < testDistance && testDistance > minDist && testDistance < maxDist)
                    {
                        distanceToTarget = testDistance;
                        targetIndex = i;
                    }
                }
            }
            target = waypoints[targetIndex];
        }
    }


    //TODO: Fix when path is close to end
    public void UpdatePath(List<PathNode> path, PathNode currentNode)
    {
        waypoints = new List<Vector3>();
        if (path.Count > 0)
        {
            List<Vector3> nodePositions = new List<Vector3>();

            int nodes = pathNodesToShow;
            nodes = Mathf.Min(path.Count - 1, nodes);


            Vector3 averageBackwardsNodePosition = Vector3.zero;
            for (int inNode = 0; inNode < currentNode.backwardNodes.Count; inNode++)
            {
                averageBackwardsNodePosition += currentNode.backwardNodes[inNode].transform.position;
            }
            averageBackwardsNodePosition /= currentNode.backwardNodes.Count;

            nodePositions.Add(averageBackwardsNodePosition);
            nodePositions.Add(currentNode.transform.position);
            for (int i = 0; i < path.Count; i++)
            {
                nodePositions.Add(path[i].transform.position);
            }

            if (nodePositions.Count > 3)
            {
                for (int i = 0; i < nodes; i++)
                {
                    CreateProgressPath(nodePositions[i], nodePositions[i + 1], nodePositions[i + 2], nodePositions[i + 3]);
                }
            }
            else
            {
                CreateStraightPath(path[0].transform.position);
            }
        }
    }

    private void CreateStraightPath(Vector3 endTarget)
    {
        for (int step = 0; step < substeps; step++)
        {
            float progress = (float)step / (float)substeps;
            waypoints.Add(Vector3.Lerp(transform.position, endTarget, progress));
        }
    }

    private void CreateProgressPath(Vector3 positionComingFrom, Vector3 currentPosition, Vector3 targetPos, Vector3 positionAfterTargetPos)
    {
        for (int step = 0; step < substeps; step++)
        {
            float progress = (float)step / (float)substeps;
            waypoints.Add(CatmullRom(positionComingFrom, currentPosition, targetPos, positionAfterTargetPos, progress));
        }
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float i)
    {
        // comments are no use here... it's the catmull-rom equation.
        // Un-magic this, lord vector!
        return 0.5f *
               ((2 * p1) + (-p0 + p2) * i + (2 * p0 - 5 * p1 + 4 * p2 - p3) * i * i +
                (-p0 + 3 * p1 - 3 * p2 + p3) * i * i * i);
    }

    [Header("Editor")]
    public Color targetIndicatorColor = Color.yellow;

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (waypoints.Count > 0)
            {
                for (int i = 0; i < waypoints.Count - 1; i++)
                {
                    Gizmos.DrawLine(waypoints[i], waypoints[i + 1]);
                }
                Gizmos.color = targetIndicatorColor;
                Gizmos.DrawLine(transform.position, target);
                Gizmos.DrawWireSphere(target, 0.5f);

                for (int i = 0; i < waypoints.Count; i++)
                {
                    Gizmos.DrawWireSphere(waypoints[i], 0.1f);
                }
            }
        }
    }
}
