using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CarAI : MonoBehaviour
{
    Vector3 progressTrackerAim;
    PathNodeProgressTracker aiPathProgressTracker;

    [SerializeField] Transform pathParent; //The path parent where the car picks random nodes from to go to
    public List<PathNode> path; //DEBUG PUBLIC
    public PathNode currentNode; //DEBUG PUBLIC - SHOULD BE SET TO THE CLOSEST ONE AT START
    [SerializeField] PathNode endNode; //Leave un-assigned if car is supposed to wander around without a set goal to stop at

    //How close we need to be a node to accept as arrived
    [SerializeField] float nodeAcceptanceDistance = 4f;
    //The wheels controller script which applies torque, braking, steering etc.
    WheelDrive wheelController;
    //Our vehicle
    Rigidbody rb;
    //Our speed
    [SerializeField] float kmhSpeed;
    //Our speed limit
    [SerializeField] float speedToHold = 10;
    //The road speed limit
    float roadSpeedLimit = 10;
    //Tell the controller to brake or not
    bool braking = false;
    //How much does the speed affect sensor length, length = kmh / thisNumber
    [SerializeField] float sensorLengthSpeedDependency = 4f;
    //How many times faster do the criminal want to go compared to the road speed limit
    [SerializeField] float criminalSpeedFactor = 1.5f;
    //Immediately stops the car
    [SerializeField] bool debugStop = false;

    bool checkingIfStuck = false;
    float timeBeforeReversingIfStuck = 2f;
    bool isStuck = false;
    int stuckSpeedSensistivity = 10;

    private enum AIState { Drive, WaitForStopAndTrafficLights, Queue, AvoidCollision, Stopping, BackingFromStuck }
    [SerializeField] AIState currentState;

    [SerializeField] bool isCriminal = false;
    [SerializeField] bool ignoreStopsAndTrafficLights = false;
    [SerializeField] bool ignoreSpeedLimit = false;

    //How much is the car steering to the right (1) or left (-1)
    float steerPercentage;

    private void Start()
    {
        if (endNode)
        {
            SetNewEndTargetNode(endNode, null);
        }
        else
        {
            SetRandomTargetNode();
        }


        wheelController = GetComponent<WheelDrive>();
        rb = GetComponent<Rigidbody>();

        if (GetComponent<PathNodeProgressTracker>() != null)
        {
            aiPathProgressTracker = GetComponent<PathNodeProgressTracker>();
            aiPathProgressTracker.UpdatePath(path, currentNode);
        }
    }



    private void Update()
    {
        kmhSpeed = rb.velocity.magnitude * 3.6f;
    }

    //Main function of the car AI
    private void FixedUpdate()
    {
        //Start with resetting values
        steerPercentage = 0;
        float torquePercentage = 0;
        braking = false;
        float distanceToObstacle = float.MaxValue;

        //Stopping the car if we have no current node (should not happen)
        //Also stopping if prompted by debug-stop-bool (panic-button)
        if (debugStop)
        {
            wheelController.AIDriver(0, 0, true);
            return;
        }
        else if (currentNode == null)
        {
            currentState = AIState.Stopping;
            wheelController.AIDriver(0, 0, true);
            return;
        }

        //Update current node and path if we are in acceptable range
        //Set status to Queue if there is a red light on our pathnode
        if (ignoreStopsAndTrafficLights)
        {
            currentState = AIState.Drive;
            UpdateWaypoint();
        }
        else if (CheckIfAllowedToPass())
        {
            currentState = AIState.Drive;
            UpdateWaypoint();
        }
        else
        {
            currentState = AIState.WaitForStopAndTrafficLights;
        }

        //Calculate steering
        if (aiPathProgressTracker)
        {
            progressTrackerAim = aiPathProgressTracker.target;
        }
        steerPercentage = aiPathProgressTracker != null ? SteerTowardsPoint(progressTrackerAim) : SteerTowardsNextNode();

        //Check for collisions
        if (CheckForCollision(steerPercentage, out RaycastHit collision))
        {
            if (collision.transform.tag == "WorldObject")
            {
                currentState = kmhSpeed < 5 ? AIState.BackingFromStuck : AIState.AvoidCollision;
            }
            else if (collision.transform.tag == "Vehicle")
            {
                currentState = isCriminal ? AIState.AvoidCollision : AIState.Queue;
            }
            else
            {
                Debug.LogWarning("No function found for response to collison object tag");
            }

            //What does this? How will it work, if only when turning, maybe different names needed
            if (LaneChangeCheck(ref steerPercentage) && !isCriminal)
            {
                currentState = AIState.Queue;
            }
            distanceToObstacle = Vector3.Distance(rb.position, collision.point);
        }

        if (kmhSpeed + stuckSpeedSensistivity < speedToHold)
        {
            if (!checkingIfStuck)
            {
                StartCoroutine("CheckIfStuck");
            }
        }
        else
        {
            StopCoroutine("CheckIfStuck");
            isStuck = false;
            checkingIfStuck = false;
        }

        if (isStuck)
        {
            currentState = AIState.BackingFromStuck;
        }

        switch (currentState)
        {
            case AIState.Drive:
                {
                    float newSpeed = ignoreSpeedLimit ? roadSpeedLimit * criminalSpeedFactor * (1 - Mathf.Abs(steerPercentage)) : roadSpeedLimit * (1 - Mathf.Abs(steerPercentage));
                    SetSpeedToHold(newSpeed);
                }
                break;
            case AIState.WaitForStopAndTrafficLights:
                {
                    float distanceToStop = Vector3.Distance(path[0].transform.position, transform.position);
                    float newSpeed = distanceToStop < 5 ? 0 : Mathf.Min(roadSpeedLimit, distanceToStop);
                    SetSpeedToHold(newSpeed);
                }
                break;
            case AIState.Queue:
                {
                    float distanceToStop = distanceToObstacle;
                    float newSpeed = distanceToStop < 5 ? 0 : Mathf.Min(roadSpeedLimit, distanceToStop);
                    SetSpeedToHold(newSpeed);
                }
                break;
            case AIState.AvoidCollision:
                {
                    AvertFromCollision(ref steerPercentage);
                    float newSpeed = ignoreSpeedLimit ? Mathf.Max(20, roadSpeedLimit * 1f * (1 - Mathf.Abs(steerPercentage))) : Mathf.Min(10, roadSpeedLimit * (1 - Mathf.Abs(steerPercentage)));
                    SetSpeedToHold(newSpeed);
                }
                break;
            case AIState.Stopping:
                {
                    steerPercentage = 0;
                    SetSpeedToHold(0);
                }
                break;
            case AIState.BackingFromStuck:
                {
                    SetSpeedToHold(3);
                }
                break;
        }

        torquePercentage = Drive((currentState == AIState.BackingFromStuck));

        if (currentState == AIState.BackingFromStuck)
        {
            steerPercentage *= -1;
            torquePercentage *= -1;
        }



        wheelController.AIDriver(steerPercentage, torquePercentage, braking);
    }

    #region PathCreation
    /// <summary>
    /// Set a defined target node with a node to avoid. Also creates and saves the path to get there
    /// /// </summary>
    private bool SetNewEndTargetNode(PathNode target, PathNode nodeToAvoid)
    {
        path = Pathfinding.GetPathToFollow(currentNode, target, nodeToAvoid).nodes;
        if (path == null || path.Count == 0 || !path.Contains(target))
        {
            Debug.LogWarning("Path to: '" + target.transform.name + target.transform.position + "', not found. Please confirm that a path to this node exists");
            currentState = AIState.Stopping;
            return false;
        }
        else
        {
            currentState = AIState.Drive;
            return true;
        }
    }

    int tests = 0;
    /// <summary>
    /// Set a random target node and then calls for a calculate path WITHOUT a node to avoid
    /// /// </summary>
    private void SetRandomTargetNode()
    {
        if (pathParent)
        {
            PathNode[] allNodes = pathParent.GetComponentsInChildren<PathNode>();
            PathNode chosenTarget = allNodes[Random.Range(0, allNodes.Length)];
            if (!SetNewEndTargetNode(chosenTarget, null) && tests < 10)
            {
                Debug.Log("Failed to find a path, trying a new random path");
                SetRandomTargetNode();
                tests++;
            }
            else if (tests >= 15)
            {
                Debug.LogError("Failed to find a random node to find a path to on 10 tries. Check node connections. This car will probably not function:  " + this.transform.name);
            }
            else
            {
                tests = 0;
            }
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
    #endregion

    /// <summary>
    /// Calculates how much the wheels should turn to go towards the current node
    /// </summary>
    private float SteerTowardsNextNode()
    {
        if (isCriminal) //do not aim exactly at the nodes
        {
            float distanceToNode = Vector3.Distance(transform.position, path[0].transform.position);
            Vector3 posToTest = transform.position + GetComponent<Rigidbody>().velocity.normalized * distanceToNode;
            if (Vector3.Distance(posToTest, path[0].transform.position) < nodeAcceptanceDistance)
            {
                return 0;
            }
            else
            {
                return SteerTowardsPoint(path[0].transform.position);
            }
        }
        else
        {
            return SteerTowardsPoint(path[0].transform.position);
        }
    }

    /// <summary>
    /// Calculates how much the wheels should turn to go towards the target point, can be used by pathnodeprogresstracker
    /// </summary>
    private float SteerTowardsPoint(Vector3 point)
    {
        Vector3 relativeVector = transform.InverseTransformPoint(point);
        relativeVector /= relativeVector.magnitude;
        return (relativeVector.x / relativeVector.magnitude);
    }

    /// <summary>
    /// Applies torque to make the car go faster
    /// </summary>
    private float Drive(bool reversing)
    {
        if (kmhSpeed > speedToHold + 5 || speedToHold < 1)
        {
            SetBrake(true);
        }
        else
        {
            SetBrake(false);
        }
        if (kmhSpeed >= speedToHold)
        {
            if (reversing)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
        return 1;
    }

    /// <summary>
    /// Checks if we are close enough to the current node
    /// If so sets our current node to the next in the list
    /// Unless we are at the goal node. Then we search for a new random nodes
    /// </summary>
    private void UpdateWaypoint()
    {
        if (Vector3.Distance(transform.position, path[0].transform.position) < nodeAcceptanceDistance)
        {
            if (path.Count > 0)
            {
                currentNode.ReducePathFindingCost();

                currentNode = path[0];
                path.RemoveAt(0);

                if (path.Count > 0)
                {
                    SetRoadSpeedLimit(currentNode.GetComponent<PathNode>().GetRoadSpeedLimit());
                    currentNode.AddPathFindingCost();
                }
                else if (endNode)
                {
                    currentNode = null;
                }
                else
                {
                    SetRandomTargetNode(); //add new path to go to
                }
            }
            GetComponent<PathNodeProgressTracker>().UpdatePath(path, currentNode);
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

    //private bool 


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

    IEnumerator CheckIfStuck()
    {
        checkingIfStuck = true;
        yield return new WaitForSeconds(timeBeforeReversingIfStuck);
        isStuck = (kmhSpeed + stuckSpeedSensistivity < speedToHold);
        checkingIfStuck = false;
    }



    [Header("Sensor")]

    [SerializeField] float carWidth = 2f;
    [SerializeField] float carLength = 3f;

    //Foward sensors
    [SerializeField] float minimumForwardSensorLength = 2f;
    float frontSensorLength = 5f;

    //Angled forward sensors
    [SerializeField] float minimumAngledSensorLength = 2f;
    float frontAngledSensorLength = 10f;
    [SerializeField] float maxAngle = 45;
    float frontSensorsAngle = 45;

    int numberOfSideSensors = 3;

    /// <summary>
    /// Check the sensors in front of the car to slow down and queue up if there is another car infront of this one
    /// Also changes state to Backing if there is an object blocking the path
    /// </summary>
    private bool CheckForCollision(float turningPercentage, out RaycastHit collision)
    {
        Vector3 sensorStartPos = transform.position;
        sensorStartPos += transform.forward * carLength / 2;

        frontSensorLength = kmhSpeed / sensorLengthSpeedDependency;
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
            collision = hit;
            if (hit.transform.tag == "Vehicle")
            {
                return true;
            }
            else if (hit.transform.tag == "WorldObject")
            {
                return true;
            }
        }

        //Check front right sensor
        sensorStartPos += transform.right * (carWidth / 2);
        collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (collisionDetected)
        {
            collision = hit;
            if (hit.transform.tag == "Vehicle")
            {
                return true;
            }
            else if (hit.transform.tag == "WorldObject")
            {
                return true;
            }
        }

        //Front left sensor
        sensorStartPos -= (transform.right * carWidth);
        collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (collisionDetected)
        {
            collision = hit;
            if (hit.transform.tag == "Vehicle")
            {
                return true;
            }
            else if (hit.transform.tag == "WorldObject")
            {
                return true;
            }
        }

        //if turning left
        if (turningPercentage < -0.1f)
        {
            //Front angled left sensor
            collisionDetected = UseSensor(sensorStartPos, Quaternion.AngleAxis(-frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength);
            if (collisionDetected)
            {
                collision = hit;
                if (hit.transform.tag == "Vehicle")
                {
                    return true;
                }
                else if (hit.transform.tag == "WorldObject")
                {
                    return true;
                }
            }
        }

        sensorStartPos += (transform.right * carWidth);
        //if turning right
        if (turningPercentage > 0.1f)
        {
            //Front angled right sensor
            collisionDetected = UseSensor(sensorStartPos, Quaternion.AngleAxis(frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength);
            if (collisionDetected)
            {
                collision = hit;
                if (hit.transform.tag == "Vehicle")
                {
                    return true;
                }
                else if (hit.transform.tag == "WorldObject")
                {
                    return true;
                }
            }
        }
        collision = hit;
        return false;
    }

    /// <summary>
    /// Checks the sensors at the side the car is turning to avoid going into other cars
    /// </summary>
    private bool LaneChangeCheck(ref float turningPercentage)
    {
        Vector3 sensorStartPos = transform.position;
        sensorStartPos += transform.forward * carLength / 2;

        bool collisionDetected = false;
        RaycastHit hit;

        float sensorLength = carWidth;

        //Right sensors
        for (int sideSensor = 0; sideSensor < numberOfSideSensors; sideSensor++)
        {
            Vector3 sensorPos = sensorStartPos - transform.forward * (sideSensor * carLength / numberOfSideSensors);
            collisionDetected = UseSensor(sensorStartPos, transform.right, out hit, sensorLength);
            if (collisionDetected)
            {
                if (hit.transform.tag == "Vehicle")
                {
                    turningPercentage -= 0.1f;
                    collisionDetected = true;
                }
            }
        }

        //Left sensors
        for (int sideSensor = 0; sideSensor < numberOfSideSensors; sideSensor++)
        {
            Vector3 sensorPos = sensorStartPos - transform.forward * (sideSensor * carLength / numberOfSideSensors);
            collisionDetected = UseSensor(sensorStartPos, -transform.right, out hit, sensorLength);
            if (collisionDetected)
            {
                if (hit.transform.tag == "Vehicle")
                {
                    turningPercentage += 0.1f;
                    collisionDetected = true;
                }
            }
        }
        return collisionDetected;
    }

    /// <summary>
    /// Check the sensors around the car for obstacles and provide steering to avoid
    /// </summary>
    private bool AvertFromCollision(ref float steerPercentage)
    {
        Vector3 sensorStartPos = transform.position;
        sensorStartPos += transform.forward * carLength / 2;

        frontSensorLength = kmhSpeed / sensorLengthSpeedDependency;
        frontSensorLength = Mathf.Max(frontSensorLength, minimumForwardSensorLength);

        frontSensorsAngle = maxAngle * Mathf.Abs(steerPercentage);
        frontAngledSensorLength = minimumAngledSensorLength * 2 * Mathf.Abs(steerPercentage);
        frontAngledSensorLength = Mathf.Max(frontAngledSensorLength, minimumAngledSensorLength);

        float avoidMultiplier = 0;
        float distanceToClosestCollision = float.MaxValue;
        bool avoidingCollision = false;

        bool collisionDetected;
        RaycastHit hit;

        //Front right sensor
        sensorStartPos += transform.right * (carWidth / 2);
        collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (collisionDetected)
        {
            avoidMultiplier -= 1f;
            avoidingCollision = true;
            distanceToClosestCollision = Mathf.Min(distanceToClosestCollision, Vector3.Distance(hit.point, transform.position));
        }

        //if steering right
        if (steerPercentage > 0.1f)
        {
            //Front angled right sensor
            collisionDetected = UseSensor(sensorStartPos, Quaternion.AngleAxis(frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength * Mathf.Abs(steerPercentage));
            if (collisionDetected)
            {
                avoidMultiplier -= 0.1f;
                avoidingCollision = true;
                distanceToClosestCollision = Mathf.Min(distanceToClosestCollision, Vector3.Distance(hit.point, transform.position));
            }
        }

        //Front left sensor
        sensorStartPos -= (transform.right * carWidth);
        collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (collisionDetected)
        {
            avoidMultiplier += 1f;
            avoidingCollision = true;
            distanceToClosestCollision = Mathf.Min(distanceToClosestCollision, Vector3.Distance(hit.point, transform.position));
        }

        //if steering left
        if (steerPercentage < -0.1f)
        {
            //Front angled left sensor
            collisionDetected = UseSensor(sensorStartPos, Quaternion.AngleAxis(-frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength * Mathf.Abs(steerPercentage));
            if (collisionDetected)
            {
                avoidMultiplier += 0.1f;
                avoidingCollision = true;
                distanceToClosestCollision = Mathf.Min(distanceToClosestCollision, Vector3.Distance(hit.point, transform.position));
            }
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
                avoidingCollision = true;
                distanceToClosestCollision = Mathf.Min(distanceToClosestCollision, Vector3.Distance(hit.point, transform.position));
            }
        }

        //Apply counter steering
        if (avoidingCollision)
        {
            steerPercentage += avoidMultiplier * Mathf.Min(1, (kmhSpeed / 3.6f) * distanceToClosestCollision);
            if (steerPercentage < -1)
            {
                steerPercentage = -1;
            }
            else if (steerPercentage > 1)
            {
                steerPercentage = 1;
            }
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


    [Header("Editor")]
    public Color lineToNode = Color.yellow;
    private void OnDrawGizmos()
    {
        if (!aiPathProgressTracker)
        {
            if (path.Count > 0)
            {
                Gizmos.color = lineToNode;
                Vector3 currentPos = this.transform.position;
                Vector3 nextNodePos = path[0].transform.position;
                Gizmos.DrawLine(currentPos, nextNodePos);
            }
        }
    }
}
