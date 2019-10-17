using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNodeProgressTracker : MonoBehaviour
{
    CarAI ai;

    public List<Vector3> pathNodeProgressPositions;
    [SerializeField] int substeps = 10;

    Rigidbody rb;

    [SerializeField] int pathNodesToShow = 4;
    [SerializeField] float lookAheadMinDistance = 4f;
    [SerializeField] float lookAheadMaxDistance = 20f;
    [SerializeField] float lookAheadSpeedModifier = 0.05f;

    public Vector3 target;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float distanceToTarget = float.MaxValue;
        int targetIndex = 0;

        float speed = rb.velocity.magnitude * 3.6f;
        speed = Mathf.Max(1, speed);

        //find closest node infront of the car
        for (int i = 0; i < pathNodeProgressPositions.Count; i++)
        {
            float testDistance = Vector3.Distance(rb.position, pathNodeProgressPositions[i]);
            if (testDistance < distanceToTarget)
            {
                if (Vector3.Distance(pathNodeProgressPositions[i], rb.position + transform.forward) < testDistance && testDistance > lookAheadMinDistance && testDistance < lookAheadMaxDistance)
                {
                    distanceToTarget = testDistance;
                    targetIndex = i;
                }
            }
        }

        //if the car is moving, add a speed lookahead
        if (speed > 1)
        {
            float add = speed * lookAheadSpeedModifier;
            int maxTest = targetIndex + Mathf.RoundToInt(add);
            maxTest = Mathf.Min(maxTest, pathNodeProgressPositions.Count - 1);
            float testDistance = Vector3.Distance(rb.position, pathNodeProgressPositions[maxTest]);
            if (testDistance > lookAheadMaxDistance)
            {
                add -= (testDistance - lookAheadMaxDistance);
            }
            add = Mathf.Max(add, lookAheadMinDistance);
            targetIndex += Mathf.RoundToInt(add);
            //never make it go outside of the range of the array
            targetIndex = Mathf.Min(pathNodeProgressPositions.Count - 1, targetIndex);
        }

        target = pathNodeProgressPositions[targetIndex];
    }


    //TODO: Fix when path is close to end
    public void UpdatePath(List<PathNode> path, PathNode currentNode)
    {
        pathNodeProgressPositions = new List<Vector3>();

        int nodes = pathNodesToShow;
        nodes = Mathf.Min(path.Count-1, nodes);



        //There needs to be at least 2 nodes to calculate a catmullrom-path
        if (path.Count > 1)
        {
            for (int i = 0; i < nodes; i++)
            {
                Vector3 pos0 = Vector3.zero;
                Vector3 pos1 = Vector3.zero;
                Vector3 pos2 = Vector3.zero;
                Vector3 pos3 = Vector3.zero;

                if (i == 0)
                {
                    //instead of showing many paths for all different kind of combinations of inputs and outputs, we take the average of position 0 of the catmull-rom if there are multiple
                    Vector3 averageBackwardsNodePosition = Vector3.zero;
                    for (int inNode = 0; inNode < currentNode.backwardNodes.Count; inNode++)
                    {
                        averageBackwardsNodePosition += currentNode.backwardNodes[inNode].transform.position;
                    }
                    averageBackwardsNodePosition /= currentNode.backwardNodes.Count;
                    pos0 = averageBackwardsNodePosition;
                    pos1 = currentNode.transform.position;
                    pos2 = path[0].transform.position;
                    pos3 = path[1].transform.position;
                }
                else if (i == 1)
                {
                    pos0 = currentNode.transform.position;
                    pos1 = path[0].transform.position;
                    pos2 = path[1].transform.position;
                    pos3 = path[2].transform.position;
                }
                else
                {
                    pos0 = path[i - 2].transform.position;
                    pos1 = path[i - 1].transform.position;
                    pos2 = path[i].transform.position;
                    pos3 = path[i + 1].transform.position;
                }
                CreateProgressPath(pos0, pos1, pos2, pos3);
            }
        }
        else
        {
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


            for (int i = 0; i < pathNodeProgressPositions.Count; i++)
            {
                Gizmos.DrawWireSphere(pathNodeProgressPositions[i], 0.1f);
            }
        }
    }
}
