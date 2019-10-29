using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarAI : MonoBehaviour
{
    public Turn turningDirection;

    //The progress tracker responsible for following a path between pathnodes
    PathNodeProgressTracker aiPathProgressTracker;
    //The wheels controller script which applies torque, braking, steering etc.
    WheelDrive wheelController;
    //Our vehicle
    Rigidbody rb;

    //The aimed for targetpathnode-progress
    Vector3 progressTrackerAim;

    [SerializeField] Transform pathParent; //The path parent where the car picks random nodes from to go to
    [SerializeField] int maxNodesInRandomizer = 30;

    public List<PathNode> path; //The path with pathnodes to follow
    public PathNode currentNode; //DEBUG PUBLIC - SHOULD BE SET TO THE CLOSEST ONE AT START
    [SerializeField] PathNode endNode; //Leave un-assigned if car is supposed to wander around without a set goal to stop at

    //How close we need to be a node to accept as arrived
    float nodeAcceptanceDistance = 4f;
    float criminalNodeAcceptanceDistance = 6f;

    //Our speed
    [SerializeField] float kmhSpeed; //Debug for info
    //Our speed limit
    float speedToHold = 30;
    //The road speed limit
    float roadSpeedLimit = 10;
    //Tell the controller to brake or not
    bool braking = false;
    //Immediately stops the car
    [SerializeField] bool debugStop = false;
    //How much is the car steering to the right (1) or left (-1)
    float steerPercentage;

    bool checkingIfStuck = false;
    float timeBeforeReversingIfStuck = 3f;
    bool isStuck = false;
    int stuckSpeedSensistivity = 10;
    float lastSpeedCheck;

    private enum AIState { Drive, WaitForStopAndTrafficLights, Queue, AvoidCollision, Stopping, BackingFromStuck }
    [SerializeField] AIState currentState;

    [SerializeField] bool recklessDriver = false;
    [SerializeField] bool ignoreStopsAndTrafficLights = false;
    [SerializeField] bool ignoreSpeedLimit = false;
    //How many times faster do the criminal want to go compared to the road speed limit
    [SerializeField] float criminalSpeedFactor = 1.5f;


    private void Start()
    {
        wheelController = GetComponent<WheelDrive>();
        rb = GetComponent<Rigidbody>();


        if (endNode)
        {
            SetNewEndTargetNode(endNode, null, false);
        }
        else
        {
            SetRandomTargetNode(null);
        }

        SetRoadSpeedLimit(currentNode.GetRoadSpeedLimit());

        if (recklessDriver && aiPathProgressTracker)
        {
            Debug.LogWarning("A rekless driver should not have the PathNodeProgressTracker script");
        }

        if (GetComponent<PathNodeProgressTracker>() != null)
        {
            aiPathProgressTracker = GetComponent<PathNodeProgressTracker>();
            aiPathProgressTracker.UpdatePath(path, currentNode);
        }

        if (recklessDriver)
        {
            nodeAcceptanceDistance = criminalNodeAcceptanceDistance;
        }

        path[0].AddCarToNode(this);
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
            currentState = AIState.Stopping;
            wheelController.AIDriver(0, 0, true);
            return;
        }
        else if (currentNode == null)
        {
            if (currentState != AIState.Stopping)
            {
                Debug.LogWarning("Current node of car " + transform.name + " is missing");
            }
            currentState = AIState.Stopping;
            wheelController.AIDriver(0, 0, true);
            return;
        }

        //Update current node and path if we are in acceptable range
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
        steerPercentage = aiPathProgressTracker != null ? SteerTowardsPoint(progressTrackerAim) : SteerTowardsNode(path[0]);

        //Check for collisions forward
        if (CheckForCollision(steerPercentage, out RaycastHit collision))
        {
            distanceToObstacle = Vector3.Distance(rb.position, collision.point);
            if (collision.transform.tag == "WorldObject")
            {
                currentState = kmhSpeed < 5 ? AIState.BackingFromStuck : AIState.AvoidCollision;
            }
            else if (collision.transform.tag == "Vehicle")
            {
                currentState = recklessDriver ? AIState.AvoidCollision : AIState.Queue;
            }
            else
            {
                Debug.LogWarning("No function found for response to collison object tag");
            }
        }

        //Check if anything is on our side of which we are turning
        if (!recklessDriver && SideSensorChecks(ref steerPercentage, out collision))
        {
            distanceToObstacle = Vector3.Distance(rb.position, collision.point);
            currentState = AIState.Queue;
        }

        if (currentState == AIState.Drive || currentState == AIState.AvoidCollision)
        {
            //Checking if stuck
            if (kmhSpeed + stuckSpeedSensistivity < speedToHold && kmhSpeed <= lastSpeedCheck)
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
        }
        else
        {
            StopCoroutine("CheckIfStuck");
            isStuck = false;
            checkingIfStuck = false;
        }
        lastSpeedCheck = kmhSpeed;

        //If target vector3 is behind the car
        if (Vector3.Distance(aiPathProgressTracker ? progressTrackerAim : path[0].transform.position, rb.position - transform.forward.normalized) < Vector3.Distance(aiPathProgressTracker ? progressTrackerAim : path[0].transform.position, rb.position))
        {
            currentState = AIState.BackingFromStuck;
        }

        if (isStuck)
        {
            currentState = AIState.BackingFromStuck;
        }

        switch (currentState)
        {
            case AIState.Drive:
                {
                    float newSpeed = CalculateSpeedToHold();
                    newSpeed = Mathf.Max(10, newSpeed); //minimum
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
                    float newSpeed = distanceToObstacle < 3 ? 0 : Mathf.Min(roadSpeedLimit, distanceToObstacle);
                    SetSpeedToHold(newSpeed);
                }
                break;
            case AIState.AvoidCollision:
                {
                    AvertFromCollision(ref steerPercentage, out RaycastHit closestCollision);
                    float newSpeed = CalculateSpeedToHold();
                    float distanceToCollision = Vector3.Distance(rb.position, closestCollision.point);
                    if (distanceToCollision < kmhSpeed / 3.6f)
                    {
                        newSpeed = distanceToCollision;
                    }
                    newSpeed = Mathf.Max(10, newSpeed); //minimum
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
                    SetSpeedToHold(5);
                }
                break;
        }

        torquePercentage = Drive((currentState == AIState.BackingFromStuck));

        //Invert values if we are reversing
        if (currentState == AIState.BackingFromStuck)
        {
            steerPercentage *= -1;
            torquePercentage *= -1;
        }

        //Send values to the tires of the car
        wheelController.AIDriver(steerPercentage, torquePercentage, braking);
    }

    #region PathCreation
    /// <summary>
    /// Set a defined target node with a node to avoid. Also creates and saves the path to get there
    /// /// </summary>
    private bool SetNewEndTargetNode(PathNode target, PathNode nodeToAvoid, bool useMaxNodes)
    {
        path = Pathfinding.GetPathToFollow(currentNode, target, nodeToAvoid, useMaxNodes, maxNodesInRandomizer).nodes;

        if (!path.Contains(target) && path.Count == maxNodesInRandomizer)
        {
            Debug.Log("Path to: '" + target.transform.name + target.transform.position + "', from '" + currentNode.transform.name + currentNode.transform.position + "' could not be found since it was too far away. A shorter route towards it was created");
            currentState = AIState.Drive;
            return true;
        }
        else if (path == null || path.Count == 0)
        {
            Debug.LogWarning("Path to: '" + target.transform.name + target.transform.position + "', from '" + currentNode.transform.name + currentNode.transform.position + "', not found. Please confirm that a path to this node exists");
            currentState = AIState.Stopping;
            return false;
        }
        else
        {
            Debug.Log("Path to: '" + target.transform.name + target.transform.position + "', from '" + currentNode.transform.name + currentNode.transform.position + "' was found without issue");
            currentState = AIState.Drive;
            return true;
        }
    }

    [SerializeField] float maxRangeFromCarToEndNodeRandom = 500f;
    int tests = 0;
    int maxTests = 100;
    /// <summary>
    /// Set a random target node and then calls for a calculate path, with possibility to avoid a node
    /// /// </summary>
    private void SetRandomTargetNode(PathNode nodeToAvoid)
    {
        tests = 0;
        if (pathParent)
        {
            PathNode[] allNodes = pathParent.GetComponentsInChildren<PathNode>();
            int maxNodes = allNodes.Length;
            PathNode chosenTarget = allNodes[Random.Range(0, maxNodes)];
            float distance = Vector3.Distance(rb.position, chosenTarget.transform.position);

            while (distance > maxRangeFromCarToEndNodeRandom && tests < maxTests)
            {
                chosenTarget = allNodes[Random.Range(0, maxNodes)];
                distance = Vector3.Distance(rb.position, chosenTarget.transform.position);
                tests++;
            }

            if (!SetNewEndTargetNode(chosenTarget, nodeToAvoid, true) && tests < maxTests)
            {
                Debug.Log("Failed to find a path, trying a new random path");
                SetRandomTargetNode(nodeToAvoid);
                tests++;
            }
            else if (tests >= maxTests)
            {
                Debug.LogError("Failed to find a random node to find a path to on " + tests + " tries. Check node connections. This car will probably not function:  " + this.transform.name);
            }
        }
        else
        {
            currentState = AIState.Stopping;
        }
    }

    //was inneficient, improved old one above by choosing pathnode within allowed distance
    ///// <summary>
    ///// Find a node x-number of nodes forward from current point, then calls for the PathFinding to find the closest and best way there
    ///// It may differ from the order we make here since there can be multiple lanes and we don't want the car to swtch more times than it needs
    ///// /// </summary>
    //int pathCreationNumberOfNodes = 30;
    //private void CreateRandomizedPath()
    //{
    //    PathNode nodeAt = currentNode;
    //    for (int i = 0; i < pathCreationNumberOfNodes; i++)
    //    {
    //        nodeAt = nodeAt.GetPathNodes()[Random.Range(0, nodeAt.GetPathNodes().Count)];
    //    }
    //    SetNewEndTargetNode(nodeAt, null);
    //}
    #endregion

    private float CalculateSpeedToHold()
    {
        if (recklessDriver)
        {
            return roadSpeedLimit * (ignoreSpeedLimit ? criminalSpeedFactor : 1) * (1 - Mathf.Abs(steerPercentage));
        }
        else
        {

            float newSpeed = roadSpeedLimit * (1 - Mathf.Abs(steerPercentage));
            if (aiPathProgressTracker)
            {
                newSpeed *= (1 - Mathf.Abs(aiPathProgressTracker.curvePercentage));
            }
            return ignoreSpeedLimit ? newSpeed * criminalSpeedFactor : newSpeed;
        }
    }

    /// <summary>
    /// Calculates how much the wheels should turn to go towards the current node
    /// </summary>
    private float SteerTowardsNode(PathNode inputNode)
    {
        if (recklessDriver) //do not aim exactly at the nodes
        {
            float distanceToNode = Vector3.Distance(transform.position, inputNode.transform.position);
            Vector3 posToTest = transform.position + GetComponent<Rigidbody>().velocity.normalized * distanceToNode;
            if (Vector3.Distance(posToTest, inputNode.transform.position) < nodeAcceptanceDistance)
            {
                return 0;
            }
            else
            {
                return SteerTowardsPoint(inputNode.transform.position);
            }
        }
        else
        {
            return SteerTowardsPoint(inputNode.transform.position);
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
            //Leaving old node
            path[0].RemoveCarFromNode(this);
            currentNode = path[0];
            path.RemoveAt(0);

            //If there are still nodes to travel to
            if (path.Count > 0)
            {
                SetRoadSpeedLimit(currentNode.GetComponent<PathNode>().GetRoadSpeedLimit());
                path[0].AddCarToNode(this);
            }
            else if (endNode)
            {
                currentNode = null;
            }
            else
            {
                //Create new path
                SetRandomTargetNode(null);
                path[0].AddCarToNode(this);
            }
        }
        if (aiPathProgressTracker)
        {
            aiPathProgressTracker.UpdatePath(path, currentNode);
        }
        turningDirection = CheckTurning();
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
        return path[0].IsAllowedToPass();
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

    //How much does the speed affect sensor length, length = kmh / thisNumber
    [SerializeField] float sensorLengthSpeedDependency = 4f;

    //Foward sensors
    [SerializeField] float minimumForwardSensorLength = 2f;
    float frontSensorLength;

    //Angled forward sensors
    [SerializeField] float minimumAngledSensorLength = 2f;
    float frontAngledSensorLength;
    [SerializeField] float maxAngle = 45;
    float frontSensorsAngle;

    int numberOfSideSensors = 3;

    /// <summary>
    /// Check the sensors in front of the car to slow down and queue up if there is another car infront of this one. Outs the closest collision
    /// </summary>
    private bool CheckForCollision(float turningPercentage, out RaycastHit closestCollision)
    {
        Vector3 sensorStartPos = transform.position;
        sensorStartPos += transform.forward * carLength / 2;

        frontSensorLength = kmhSpeed / sensorLengthSpeedDependency;
        frontSensorLength = Mathf.Max(frontSensorLength, minimumForwardSensorLength);

        frontSensorsAngle = maxAngle * Mathf.Abs(turningPercentage);
        frontAngledSensorLength = minimumAngledSensorLength * 2 * Mathf.Abs(turningPercentage);
        frontAngledSensorLength = Mathf.Max(frontAngledSensorLength, minimumAngledSensorLength);


        bool collisionDetected = false;
        bool sensorCollision = false;
        RaycastHit hit;
        List<RaycastHit> collisions = new List<RaycastHit>();

        //Front center sensor
        sensorCollision = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (sensorCollision)
        {
            if (hit.transform.tag == "Vehicle")
            {
                collisions.Add(hit);
                collisionDetected = true;
            }
            else if (hit.transform.tag == "WorldObject")
            {
                collisions.Add(hit);
                collisionDetected = true;
            }
        }

        //Check front right sensor
        sensorStartPos += transform.right * (carWidth / 2);
        sensorCollision = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (sensorCollision)
        {
            if (hit.transform.tag == "Vehicle")
            {
                collisions.Add(hit);
                collisionDetected = true;
            }
            else if (hit.transform.tag == "WorldObject")
            {
                collisions.Add(hit);
                collisionDetected = true;
            }
        }

        //Front left sensor
        sensorStartPos -= (transform.right * carWidth);
        sensorCollision = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (sensorCollision)
        {
            if (hit.transform.tag == "Vehicle")
            {
                collisions.Add(hit);
                collisionDetected = true;
            }
            else if (hit.transform.tag == "WorldObject")
            {
                collisions.Add(hit);
                collisionDetected = true;
            }
        }

        //if turning left
        if (turningPercentage < -0.1f)
        {
            //Front angled left sensor
            sensorCollision = UseSensor(sensorStartPos, Quaternion.AngleAxis(-frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength);
            if (sensorCollision)
            {
                if (hit.transform.tag == "Vehicle")
                {
                    collisions.Add(hit);
                    collisionDetected = true;
                }
                else if (hit.transform.tag == "WorldObject")
                {
                    collisions.Add(hit);
                    collisionDetected = true;
                }
            }
        }

        sensorStartPos += (transform.right * carWidth);
        //if turning right
        if (turningPercentage > 0.1f)
        {
            //Front angled right sensor
            sensorCollision = UseSensor(sensorStartPos, Quaternion.AngleAxis(frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength);
            if (sensorCollision)
            {
                if (hit.transform.tag == "Vehicle")
                {
                    collisions.Add(hit);
                    collisionDetected = true;
                }
                else if (hit.transform.tag == "WorldObject")
                {
                    collisions.Add(hit);
                    collisionDetected = true;
                }
            }
        }

        closestCollision = new RaycastHit(); //needed for acceptance, overriden in for loop anyway if any hits are found
        float distanceToClosestCollision = float.MaxValue;
        for (int i = 0; i < collisions.Count; i++)
        {
            float testDistance = Vector3.Distance(rb.position, collisions[i].point);
            if (testDistance < distanceToClosestCollision)
            {
                distanceToClosestCollision = testDistance;
                closestCollision = collisions[i];
            }
        }
        return collisionDetected;
    }

    /// <summary>
    /// Checks the sensors at the side the car is turning to avoid going into other cars. Outs the closest collision
    /// </summary>
    private bool SideSensorChecks(ref float turningPercentage, out RaycastHit closestCollision)
    {
        Vector3 carFront = transform.position;
        carFront += transform.forward * carLength / 2;
        float sensorLength = carWidth * 3;

        bool collisionDetected = false;

        RaycastHit hit;
        List<RaycastHit> collisions = new List<RaycastHit>();

        if (!aiPathProgressTracker || aiPathProgressTracker.curvePercentage > 0.05f)
        {
            bool sensorCollision = false;
            //Right sensors
            for (int sideSensor = 0; sideSensor < numberOfSideSensors; sideSensor++)
            {
                Vector3 sensorPos = carFront - transform.forward * (sideSensor * carLength / numberOfSideSensors);
                sensorCollision = UseSensor(sensorPos, Quaternion.AngleAxis(89, transform.up) * transform.forward, out hit, sensorLength);
                if (sensorCollision)
                {
                    if (hit.transform.tag == "Vehicle")
                    {
                        collisions.Add(hit);
                        turningPercentage -= (float)((float)1 / (float)numberOfSideSensors) * 0.1f;
                        collisionDetected = true;
                    }
                }
            }
            //Front angled right sensor
            Vector3 angledRightPos = carFront + (transform.right * carWidth / 2);
            sensorCollision = UseSensor(angledRightPos, Quaternion.AngleAxis(45, transform.up) * transform.forward, out hit, sensorLength);
            if (sensorCollision)
            {
                if (hit.transform.tag == "Vehicle")
                {
                    collisions.Add(hit);
                    turningPercentage -= 0.1f;
                    collisionDetected = true;
                }
            }
            //Right progress tracker sensor
            Vector3 progressTrackerSensor = carFront + (transform.right * carWidth / 2);
            sensorCollision = UseSensor(progressTrackerSensor, progressTrackerAim, out hit, sensorLength);
            if (sensorCollision)
            {
                if (hit.transform.tag == "Vehicle")
                {
                    collisions.Add(hit);
                    turningPercentage -= 0.1f;
                    collisionDetected = true;
                }
            }
        }
        else if (!aiPathProgressTracker || aiPathProgressTracker.curvePercentage < -0.05f)
        {
            bool sensorCollision = false;
            //Left sensors
            for (int sideSensor = 0; sideSensor < numberOfSideSensors; sideSensor++)
            {
                Vector3 sensorPos = carFront - transform.forward * (sideSensor * carLength / numberOfSideSensors);
                sensorCollision = UseSensor(sensorPos, Quaternion.AngleAxis(-89, transform.up) * transform.forward, out hit, sensorLength);
                if (sensorCollision)
                {
                    if (hit.transform.tag == "Vehicle")
                    {
                        collisions.Add(hit);
                        turningPercentage += (float)((float)1 / (float)numberOfSideSensors) * 0.1f;
                        collisionDetected = true;
                    }
                }
            }
            //Front angled left sensor
            Vector3 angledLeftPos = carFront - (transform.right * carWidth / 2);
            sensorCollision = UseSensor(angledLeftPos, Quaternion.AngleAxis(-45, transform.up) * transform.forward, out hit, sensorLength);
            if (sensorCollision)
            {
                if (hit.transform.tag == "Vehicle")
                {
                    collisions.Add(hit);
                    turningPercentage += 0.1f;
                    collisionDetected = true;
                }
            }
            //Left progress tracker sensor
            Vector3 progressTrackerSensor = carFront - (transform.right * carWidth / 2);
            sensorCollision = UseSensor(progressTrackerSensor, progressTrackerAim, out hit, sensorLength);
            if (sensorCollision)
            {
                if (hit.transform.tag == "Vehicle")
                {
                    collisions.Add(hit);
                    turningPercentage += 0.1f;
                    collisionDetected = true;
                }
            }
        }


        closestCollision = new RaycastHit(); //needed for acceptance, overriden in for loop anyway if any hits are found
        float distanceToClosestCollision = float.MaxValue;
        for (int i = 0; i < collisions.Count; i++)
        {
            float testDistance = Vector3.Distance(rb.position, collisions[i].point);
            if (testDistance < distanceToClosestCollision)
            {
                distanceToClosestCollision = testDistance;
                closestCollision = collisions[i];
            }
        }
        return collisionDetected;
    }

    /// <summary>
    /// Check the sensors around the car for obstacles and provide steering to avoid. Outs the closest collision
    /// </summary>
    private bool AvertFromCollision(ref float turningPercentage, out RaycastHit closestCollision)
    {
        Vector3 sensorStartPos = transform.position;
        sensorStartPos += transform.forward * carLength / 2;

        frontSensorLength = kmhSpeed / sensorLengthSpeedDependency;
        frontSensorLength = Mathf.Max(frontSensorLength, minimumForwardSensorLength);

        frontSensorsAngle = maxAngle * Mathf.Abs(turningPercentage);
        frontAngledSensorLength = minimumAngledSensorLength * 2 * Mathf.Abs(turningPercentage);
        frontAngledSensorLength = Mathf.Max(frontAngledSensorLength, minimumAngledSensorLength);

        float avoidMultiplier = 0;
        bool avoidingCollision = false;

        bool collisionDetected;
        RaycastHit hit;
        List<RaycastHit> collisions = new List<RaycastHit>();

        //Front right sensor
        sensorStartPos += transform.right * (carWidth / 2);
        collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (collisionDetected)
        {
            avoidMultiplier -= 1f;
            avoidingCollision = true;
            collisions.Add(hit);
        }

        //if steering right
        if (turningPercentage > 0.1f)
        {
            //Front angled right sensor
            collisionDetected = UseSensor(sensorStartPos, Quaternion.AngleAxis(frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength * Mathf.Abs(turningPercentage));
            if (collisionDetected)
            {
                avoidMultiplier -= 0.1f;
                avoidingCollision = true;
                collisions.Add(hit);
            }
        }

        //Front left sensor
        sensorStartPos -= (transform.right * carWidth);
        collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (collisionDetected)
        {
            avoidMultiplier += 1f;
            avoidingCollision = true;
            collisions.Add(hit);
        }

        //if steering left
        if (turningPercentage < -0.1f)
        {
            //Front angled left sensor
            collisionDetected = UseSensor(sensorStartPos, Quaternion.AngleAxis(-frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength * Mathf.Abs(turningPercentage));
            if (collisionDetected)
            {
                avoidMultiplier += 0.1f;
                avoidingCollision = true;
                collisions.Add(hit);
            }
        }

        //Front center sensor
        {
            collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
            if (collisionDetected)
            {
                if (avoidMultiplier == 0)
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
                avoidingCollision = true;
                collisions.Add(hit);
            }
        }

        //Check closest collision
        closestCollision = new RaycastHit(); //needed for acceptance, overriden in for loop anyway if any hits are found
        float distanceToClosestCollision = float.MaxValue;
        for (int i = 0; i < collisions.Count; i++)
        {
            float testDistance = Vector3.Distance(rb.position, collisions[i].point);
            if (testDistance < distanceToClosestCollision)
            {
                distanceToClosestCollision = testDistance;
                closestCollision = collisions[i];
            }
        }

        //Apply counter steering
        if (avoidingCollision)
        {
            turningPercentage += avoidMultiplier * Mathf.Min(1, (kmhSpeed / 3.6f) / distanceToClosestCollision);
            if (turningPercentage < -1)
            {
                turningPercentage = -1;
            }
            else if (turningPercentage > 1)
            {
                turningPercentage = 1;
            }
        }
        return avoidingCollision;
    }

    [SerializeField] float criticalCollisionDistance = 2f;
    [SerializeField] float warningCollisionDistance = 5f;
    [SerializeField] float infoCollisionDistance = 10f;

    /// <summary>
    /// Uses raycast to detect obstacles
    /// </summary>
    private bool UseSensor(Vector3 startPos, Vector3 direction, out RaycastHit hit, float sensorLength)
    {
        hit = new RaycastHit();

        if (Physics.Raycast(startPos, direction, out hit, sensorLength))
        {
            if (hit.transform.tag != "Terrain" && hit.transform != transform)
            {
                float distance = Vector3.Distance(rb.position, hit.point);
                Color rayColor = Color.black;
                if (distance < criticalCollisionDistance)
                {
                    rayColor = Color.red;
                }
                else if (distance < warningCollisionDistance)
                {
                    rayColor = Color.yellow;
                }
                else if (distance < infoCollisionDistance)
                {
                    rayColor = Color.white;
                }
                Debug.DrawLine(startPos, hit.point, rayColor);
                return true;
            }
        }
        return false;
    }

    private Turn CheckTurning()
    {
        if (path.Count > 1 && path[0].GetOutChoices().Count > 1)
        {
            for (int i = 0; i < path[0].outChoices.Count; i++)
            {
                if (path[1] == path[0].outChoices[i].outNode)
                {
                    return path[0].outChoices[i].turnDirection;
                }
            }
            Debug.LogWarning("Did not find the correct direction, check outnode choices");
        }
        return Turn.Straight;
    }


    [Header("Editor")]
    public Color lineToNode = Color.yellow;
    public bool showLineToNextNode = false;
    private void OnDrawGizmos()
    {
        if (showLineToNextNode)
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
