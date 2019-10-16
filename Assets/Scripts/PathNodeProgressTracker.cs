using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNodeProgressTracker : MonoBehaviour
{
    CarAI ai;

    List<Vector3> pathNodeProgressPositions;
    [SerializeField] int substeps = 10;

    Rigidbody rb;

    [SerializeField] int pathNodesToShow = 4;
    [SerializeField] float lookAheadDistanceModifier = 4f;
    [SerializeField] float lookAheadSpeedModifier = 0.1f;

    public Vector3 target;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }


    private void Update()
    {
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < pathNodeProgressPositions.Count; i++)
        {
            float testDistance = Vector3.Distance(rb.position, pathNodeProgressPositions[i]);
            if (testDistance < closestDistance && testDistance > lookAheadDistanceModifier)
            {
                //if the node is infront of the car
                if (Vector3.Distance(pathNodeProgressPositions[i], rb.position + transform.forward) < testDistance)
                closestDistance = testDistance;
                closestIndex = i;
            }
        }
        int indexToFollow = closestIndex + Mathf.RoundToInt(rb.velocity.magnitude * lookAheadSpeedModifier);
        indexToFollow = Mathf.Min(pathNodeProgressPositions.Count - 1, indexToFollow);

        target = pathNodeProgressPositions[indexToFollow];
    }

    public void UpdatePath(List<PathNode> path)
    {
        pathNodeProgressPositions = new List<Vector3>();

        int nodes = pathNodesToShow;
        nodes = Mathf.Min(path.Count, nodes);

        float speed = rb.velocity.magnitude * 3.6f;
        speed = Mathf.Max(1, speed);
        Vector3 positionBehindCar = rb.position - transform.forward * speed;

        //There needs to be at least 2 nodes to calculate a catmullrom-path
        for (int i = 0; i < nodes - 1; i++)
        {
            if (i == 0 && i + 1 < path.Count)
            {
                CreateProgressPath(positionBehindCar, rb.position, path[i].transform.position, path[i + 1].transform.position);
            }
            else if (i == 1 && i + 2 < path.Count)
            {
                CreateProgressPath(rb.position, path[i].transform.position, path[i + 1].transform.position, path[i + 2].transform.position);
            }
            else if (i + 3 < path.Count)
            {
                CreateProgressPath(path[i].transform.position, path[i + 1].transform.position, path[i + 2].transform.position, path[i + 3].transform.position);
            }
        }

        if (nodes == 1)
        {
            Debug.LogWarning("Cannot create a path with substeps since there is not enough nodes in the current path");
            CreateStraightPath(path[path.Count - 1].transform.position);
        }
    }

    private void CreateStraightPath(Vector3 endTarget)
    {
        for (int step = 0; step < substeps; step++)
        {
            float progress = (float)step / (float)substeps;
            pathNodeProgressPositions.Add(Vector3.Lerp(transform.position, endTarget, progress));
        }
    }

    private void CreateProgressPath(Vector3 positionComingFrom, Vector3 currentPosition, Vector3 targetPos, Vector3 positionAfterTargetPos)
    {
        for (int step = 0; step < substeps; step++)
        {
            float progress = (float)step / (float)substeps;
            pathNodeProgressPositions.Add(CatmullRom(positionComingFrom, currentPosition, targetPos, positionAfterTargetPos, progress));
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
            for (int i = 0; i < pathNodeProgressPositions.Count - 1; i++)
            {
                Gizmos.DrawLine(pathNodeProgressPositions[i], pathNodeProgressPositions[i + 1]);
            }

            Gizmos.color = targetIndicatorColor;
            Gizmos.DrawLine(transform.position, target);
            Gizmos.DrawWireSphere(target, 0.5f);
        }
    }
}
