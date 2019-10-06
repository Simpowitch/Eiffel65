using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CarAI : MonoBehaviour
{
    [SerializeField] Transform pathParent;
    public List<PathNode> path; //DEBUG PUBLIC
    public PathNode currentNode; //DEBUG PUBLIC - SHOULD BE SET TO THE CLOSEST ONE AT START
    [SerializeField] PathNode targetNode;

    //How close we need to be a node to accept as arrived
    [SerializeField] float distanceToAcceptNodeArrival = 2f;
    //The wheels controller script which applies torque, braking, steering etc.
    WheelDrive wheelController;
    //Our vehicle
    Rigidbody rb;
    //Our speed
    float kmhSpeed;
    //Our speed limit
    [SerializeField] float speedToHold = 50f;
    //The road speed limit
    [SerializeField] float roadSpeedLimit = 50f;
    //Tell the controller to brake or not
    [SerializeField] bool braking = false;

    private enum AIState { Drive, Queue, AvoidCollision, Stopping, BackingFromStuck }
    [SerializeField] AIState currentState;


    private void Start()
    {
        //currentNode = SEARCH FOR CLOSEST NODE 
        if (targetNode)
        {
            SetNewEndTargetNode(targetNode);
        }
        else
        {
            SetRandomTargetNode();
        }


        wheelController = GetComponent<WheelDrive>();
        rb = GetComponent<Rigidbody>();
    }



    private void Update()
    {
        kmhSpeed = rb.velocity.magnitude * 3.6f;
    }

    //Main function of the car AI
    private void FixedUpdate()
    {
        switch (currentState)
        {
            case AIState.Drive:
                {
                    float steerPercentage = SteerTowardsNextNode();
                    if (CheckFOrCollisions(steerPercentage))
                    {
                        currentState = AIState.Queue;
                        return;
                    }

                    SetSpeedToHold(Mathf.Max(10, roadSpeedLimit * (1 - Mathf.Abs(steerPercentage))));


                    if (!CheckIfAllowedToPass())
                    {
                        currentState = AIState.Queue;
                        return;
                    }
                    else
                    {
                        UpdateWaypoint();
                    }
                    float torquePercentage = Drive(steerPercentage);
                    wheelController.AIDriver(steerPercentage, torquePercentage, braking);
                }
                break;
            case AIState.Queue:
                {
                    float steerPercentage = SteerTowardsNextNode();

                    if (CheckFOrCollisions(steerPercentage)) //If car in front of us
                    {
                        SetSpeedToHold(0);
                        UpdateWaypoint();
                    }
                    else
                    {
                        if (CheckIfAllowedToPass()) //If not allowed to pass node
                        {
                            currentState = AIState.Drive;
                            UpdateWaypoint();
                            return;
                        }
                        else
                        {
                            float distanceToStop = Vector3.Distance(path[0].transform.position, transform.position);
                            if (distanceToStop < 5)
                            {
                                speedToHold = 0;
                            }
                            else
                            {
                                SetSpeedToHold(Mathf.Min(roadSpeedLimit, distanceToStop));
                            }
                        }
                    }
                    float torquePercentage = Drive(steerPercentage);
                    wheelController.AIDriver(steerPercentage, torquePercentage, braking);
                }
                break;
            case AIState.AvoidCollision:
                {
                    //float steerPercentage = SteerTowardsNextNode();
                    //float torquePercentage = Drive(steerPercentage);
                    //CheckForCollisionAndAvoid(ref steerPercentage);
                    //CheckWayPointDistance();
                    //wheelController.AIDriver(steerPercentage, torquePercentage, braking);
                }
                break;
            case AIState.Stopping:
                {
                    //is currently not used
                    float steerPercentage = 0;
                    SetSpeedToHold(0);
                    float torquePercentage = Drive(steerPercentage);
                    wheelController.AIDriver(steerPercentage, torquePercentage, braking);
                }
                break;
            case AIState.BackingFromStuck:
                {
                    SetBrake(false);
                    SetSpeedToHold(3);
                    float steerPercentage = 0;
                    float torquePercentage = -0.25f;
                    RaycastHit hit;
                    Vector3 sensorStartPos = transform.position;
                    sensorStartPos += transform.forward * frontSensorPosition.z;
                    sensorStartPos += transform.up * frontSensorPosition.y;
                    if (!UseSensor(sensorStartPos, transform.forward, out hit, 6.5f))
                    {
                        currentState = AIState.Drive;
                        return;
                    }
                    else
                    {
                        steerPercentage = SteerTowardsNextNode() * -1;
                    }
                    wheelController.AIDriver(steerPercentage, torquePercentage, braking);
                }
                break;
        }
    }

    /// <summary>
    /// Set a defined target node. Also creates and saves the path to get there
    /// /// </summary>
    private void SetNewEndTargetNode(PathNode target)
    {
        path = Pathfinding.GetPathToFollow(currentNode, target, null).nodes;
        if (path == null || path.Count == 0 || !path.Contains(target))
        {
            currentState = AIState.Stopping;
            Debug.LogError("Path not found");
        }
        currentState = AIState.Drive;
    }

    /// <summary>
    /// Set a defined target node with a node to avoid. Also creates and saves the path to get there
    /// /// </summary>
    private void SetNewEndTargetNode(PathNode target, PathNode avoid)
    {
        path = Pathfinding.GetPathToFollow(currentNode, target, avoid).nodes;
        if (path == null || path.Count == 0 || !path.Contains(target))
        {
            currentState = AIState.Stopping;
            Debug.LogError("Path not found");
        }
        currentState = AIState.Drive;
    }

    /// <summary>
    /// Set a random target node and then calls for a calculate path WITHOUT a node to avoid
    /// /// </summary>
    private void SetRandomTargetNode()
    {
        if (pathParent)
        {
            PathNode[] allNodes = pathParent.GetComponentsInChildren<PathNode>();
            PathNode chosenTarget = allNodes[Random.Range(0, allNodes.Length)];
            SetNewEndTargetNode(chosenTarget);
        }
        else
        {
            currentState = AIState.Stopping;
        }
    }

    /// <summary>
    /// Set a random target node and then calls for a calculate path WITH a node to avoid
    /// /// </summary>
    private void SetRandomTargetNode(PathNode nodeToAvoid)
    {
        if (pathParent)
        {
            PathNode[] allNodes = pathParent.GetComponentsInChildren<PathNode>();
            PathNode chosenTarget = allNodes[Random.Range(0, allNodes.Length)];
            SetNewEndTargetNode(chosenTarget, nodeToAvoid);
        }
        else
        {
            currentState = AIState.Stopping;
        }
    }


    //private List<PathNode> GetPathToFollow(PathNode target)
    //{

    //    //List<PathNode> foundPath = new List<PathNode>();
    //    //List<PathNode> testedNodes = new List<PathNode>();
    //    //SearchNode(currentNode, target, foundPath, testedNodes);
    //    //foundPath.Reverse();
    //    //return foundPath;
    //}

    ///// <summary>
    ///// Searches for a path to the target node and adds them to the referenced ref input pathlist
    ///// </summary>
    ////private bool SearchNode(PathNode posToSearchFrom, PathNode target, List<PathNode> pathSoFar, List<PathNode> testedNodes)
    ////{
    ////    PathNode[] pathOptions = posToSearchFrom.GetPathNodes();
    ////    pathOptions = SortNodesByDistance(pathOptions, target);
    ////    for (int i = 0; i < pathOptions.Length; i++)
    ////    {
    ////        if (testedNodes.Contains(pathOptions[i]))
    ////        {
    ////            continue;
    ////        }
    ////        if (pathOptions[i] == target)
    ////        {
    ////            pathSoFar.Add(pathOptions[i]);
    ////            return true;
    ////        }
    ////        else
    ////        {
    ////            testedNodes.Add(pathOptions[i]);
    ////            if (SearchNode(pathOptions[i], target, pathSoFar, testedNodes))
    ////            {
    ////                pathSoFar.Add(pathOptions[i]);
    ////                return true;
    ////            }
    ////        }
    ////    }
    ////    return false;
    ////}

    /////// <summary>
    /////// Sorts the nodes by the distance to the target
    /////// </summary>
    ////private PathNode[] SortNodesByDistance(PathNode[] nodesToSort, PathNode target)
    ////{
    ////    PathNode[] sortedArray = new PathNode[nodesToSort.Length];
    ////    sortedArray = nodesToSort.OrderBy(x => Vector3.Distance(target.transform.position, x.transform.position)).ToArray();
    ////    return sortedArray;
    ////}




    /// <summary>
    /// Calculates how much the wheels should turn to go towards the current node
    /// </summary>
    private float SteerTowardsNextNode()
    {
        Vector3 relativeVector = transform.InverseTransformPoint(path[0].transform.position);
        relativeVector /= relativeVector.magnitude;

        return (relativeVector.x / relativeVector.magnitude);
    }

    /// <summary>
    /// Applies torque to make the car go faster
    /// </summary>
    private float Drive(float steerPercentage)
    {
        if (kmhSpeed > speedToHold + 5 || speedToHold < 5)
        {
            SetBrake(true);
        }
        else
        {
            SetBrake(false);
        }
        if (kmhSpeed >= speedToHold)
        {
            return 0;
        }
        return 1;
    }



    /// <summary>
    /// Checks if we are close enough to the current node
    /// If so sets our current node to the next in the list
    /// Unless we are at the goal node. Then we brake
    /// </summary>
    private void UpdateWaypoint()
    {
        if (Vector3.Distance(transform.position, path[0].transform.position) < distanceToAcceptNodeArrival)
        {
            if (path.Count > 1)
            {
                path.RemoveAt(0);
                Debug.Log(transform.gameObject.name + " is going to pathnode: " + path[0]);
                SetRoadSpeedLimit(path[0].GetComponent<PathNode>().GetRoadSpeedLimit());
                currentNode = path[0];
            }
            else
            {
                SetRandomTargetNode(); //add new path to go to

                //Debug.Log(transform.gameObject.name + " is stopping.");
                //currentState = AIState.Stopping;
                //SetBrake(true);
            }
        }
    }


    /// <summary>
    /// The car brakes
    /// </summary>
    private void SetBrake(bool input)
    {
        braking = input;

        if (braking)
        {
            //apply brakelights
        }
        else
        {
            //turn off brakelights
        }
    }


    /// <summary>
    /// Sets the new speed limit which the car will try to follow
    /// </summary>
    public void SetSpeedToHold(float newMaxSpeed)
    {
        speedToHold = newMaxSpeed;
    }

    /// <summary>
    /// Sets the new speed limit of the road
    /// </summary>
    public void SetRoadSpeedLimit(float newMaxSpeed)
    {
        roadSpeedLimit = newMaxSpeed;
    }

    /// <summary>
    /// Returns true if there is a red light to stop for
    /// </summary>
    public bool CheckIfAllowedToPass()
    {
        return path[0].GetComponent<PathNode>().IsAllowedToPass();
    }





    [Header("Sensor")]
    [SerializeField] float minimumForwardSensorLength = 2f;
    [SerializeField] float minimumAngledSensorLength = 2f;
    [SerializeField] float maxAngle = 45;

    float frontSensorLength = 5f;
    float frontAngledSensorLength = 10f;
    [SerializeField] Vector3 frontSensorPosition = new Vector3(0, 0.2f, 1f);
    [SerializeField] float frontSideSensorXOffsetPos = 1f;
    float frontSensorsAngle = 45;

    /// <summary>
    /// Check the sensors in front of the car to slow down and queue up if there is another car infront of this one
    /// Also changes state to Backing if there is an object blocking the path
    /// </summary>
    private bool CheckFOrCollisions(float turningPercentage)
    {
        Vector3 sensorStartPos = transform.position;
        sensorStartPos += transform.forward * frontSensorPosition.z;
        sensorStartPos += transform.up * frontSensorPosition.y;

        frontSensorLength = kmhSpeed / 2;
        frontSensorLength = Mathf.Max(frontSensorLength, minimumForwardSensorLength);

        frontSensorsAngle = maxAngle * Mathf.Abs(turningPercentage);
        frontAngledSensorLength = minimumAngledSensorLength * 2 * Mathf.Abs(turningPercentage);
        frontAngledSensorLength = Mathf.Max(frontAngledSensorLength, minimumAngledSensorLength);


        bool collisionDetected;
        RaycastHit hit;

        //Front center sensor
        collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (collisionDetected)
        {
            if (hit.transform.tag == "Vehicle")
            {
                return true;
            }
            else if (hit.transform.tag == "WorldObject")
            {
                currentState = AIState.BackingFromStuck;
            }
        }

        //Check front right sensor
        sensorStartPos += transform.right * frontSideSensorXOffsetPos;
        collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (collisionDetected)
        {
            if (hit.transform.tag == "Vehicle")
            {
                return true;
            }
            else if (hit.transform.tag == "WorldObject")
            {
                currentState = AIState.BackingFromStuck;
            }
        }

        //Front left sensor
        sensorStartPos -= (transform.right * frontSideSensorXOffsetPos) * 2;
        collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (collisionDetected)
        {
            if (hit.transform.tag == "Vehicle")
            {
                return true;
            }
            else if (hit.transform.tag == "WorldObject")
            {
                currentState = AIState.BackingFromStuck;
            }
        }

        //Front angled left sensor
        collisionDetected = UseSensor(sensorStartPos, Quaternion.AngleAxis(-frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength);
        if (collisionDetected)
        {
            if (hit.transform.tag == "Vehicle")
            {
                return true;
            }
            else if (hit.transform.tag == "WorldObject")
            {
                currentState = AIState.BackingFromStuck;
            }
        }
        //Front angled right sensor
        sensorStartPos += (transform.right * frontSideSensorXOffsetPos) * 2;
        collisionDetected = UseSensor(sensorStartPos, Quaternion.AngleAxis(frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength);
        if (collisionDetected)
        {
            if (hit.transform.tag == "Vehicle")
            {
                return true;
            }
            else if (hit.transform.tag == "WorldObject")
            {
                currentState = AIState.BackingFromStuck;
            }
        }
        return false;
    }

    /// <summary>
    /// Check the sensors around the car for obstacles and provide steering to avoid
    /// </summary>
    private bool AvertFromCollision(ref float steerPercentage)
    {
        Vector3 sensorStartPos = transform.position;
        sensorStartPos += transform.forward * frontSensorPosition.z;
        sensorStartPos += transform.up * frontSensorPosition.y;

        frontSensorLength = kmhSpeed / 2;
        frontAngledSensorLength = frontSensorLength / 2;

        frontSensorLength = Mathf.Max(frontSensorLength, 5);
        frontAngledSensorLength = Mathf.Max(frontAngledSensorLength, 5);

        float avoidMultiplier = 0;
        bool avoidingCollision = false;

        bool collisionDetected;
        RaycastHit hit;

        //Front right sensor
        sensorStartPos += transform.right * frontSideSensorXOffsetPos;
        collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (collisionDetected)
        {
            avoidMultiplier -= 1f;
        }
        //Front angled right sensor
        collisionDetected = UseSensor(sensorStartPos, Quaternion.AngleAxis(frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength * Mathf.Abs(steerPercentage));
        if (collisionDetected)
        {
            avoidMultiplier -= 0.1f;
        }

        //Front left sensor
        sensorStartPos -= (transform.right * frontSideSensorXOffsetPos) * 2;
        collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (collisionDetected)
        {
            avoidMultiplier += 1f;
        }

        //Front angled left sensor
        collisionDetected = UseSensor(sensorStartPos, Quaternion.AngleAxis(-frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength * Mathf.Abs(steerPercentage));
        if (collisionDetected)
        {
            avoidMultiplier += 0.1f;
        }

        //Front center sensor
        if (avoidMultiplier == 0)
        {
            collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
            if (collisionDetected)
            {
                if (hit.normal.x < 0)
                {
                    avoidMultiplier = -1;
                }
                else
                {
                    avoidMultiplier = 1;
                }
            }
        }

        //Apply counter steering
        if (avoidingCollision)
        {
            steerPercentage = avoidMultiplier;
        }
        return avoidingCollision;
    }

    /// <summary>
    /// Uses raycast to detect obstacles
    /// </summary>
    private bool UseSensor(Vector3 startPos, Vector3 direction, out RaycastHit hit, float sensorLength)
    {
        hit = new RaycastHit();

        if (Physics.Raycast(startPos, direction, out hit, sensorLength))
        {
            if (hit.transform.tag != "Terrain")
            {
                Debug.DrawLine(startPos, hit.point);
                return true;
            }
        }
        return false;
    }

}
