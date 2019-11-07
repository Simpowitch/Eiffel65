﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PathNodeProgressTracker))]
[RequireComponent(typeof(WheelDrive))]
[RequireComponent(typeof(Rigidbody))]
public class CarAI : MonoBehaviour
{
    //Immediately stops the car
    [SerializeField] bool debugStop = false;

    //Which direction is the car turning
    public Turn turningDirection;
    //Used for "blinkers" when turning
    int turnPathNodeCount = 0;
    //Used for "blinkers", while this is on, blinkers should be applied
    bool performingTurn = false;

    //The progress tracker responsible for following a path between pathnodes
    PathNodeProgressTracker aiPathProgressTracker;
    //The wheels controller script which applies torque, braking, steering etc.
    WheelDrive wheelController;
    //Our vehicle
    Rigidbody rb;

    //The path parent where the car picks random nodes from to go to
    [SerializeField] Transform pathParent;
    //How many nodes is the maximum when getting a new random path
    [SerializeField] int maxNodesInRandomizer = 30;

    //The path with pathnodes to follow
    [SerializeField] List<PathNode> path;
    //The node we are at/last visited
    PathNode currentNode;
    //A node where the car should stop
    //Leave un-assigned if car is supposed to wander around without a set goal to stop at
    [SerializeField] PathNode endNode;

    //How close we need to be a node to accept as arrived
    float nodeAcceptanceDistance = 4f;
    float criminalNodeAcceptanceDistance = 6f;

    //Our speed
    [SerializeField] float kmhSpeed;
    //Our speed limit
    float speedToHold = 30;
    //How much is the car steering to the right (1) or left (-1)
    float steerPercentage;
    //How much are we accelerating (-1 = 100% backwards & 1 = 100% forwards)
    float torquePercentage;
    //Tell the controller to brake or not
    bool braking = false;

    //Is the timer on to check if we have moved
    bool checkingIfStuck = false;
    //Time before reversing
    float checkTimeBeforeStuck = 5f;
    //Is the car stuck
    [SerializeField] bool isStuck = false;
    //If we are this speed lower than what we want, then we are possibly stuck
    float stuckSpeedSensistivity = 5f;
    //How long time shall we back if stuck
    float timeToBack = 3f;
    //Used to check if car is accelerating
    float lastSpeedCheck;

    //Car blocking our path
    public CarAI blockingCar = null;

    //The state of the driver/rules
    private enum AIState { Drive, StopAtNextPathNode, Queue, AvoidCollision, Stopping, BackingFromStuck, GiveWayEmergency }
    [SerializeField] AIState currentState;


    //Different types of criminal acts
    [SerializeField] bool recklessDriver = false;
    [SerializeField] bool ignoreStopsAndTrafficLights = false;
    [SerializeField] bool ignoreSpeedLimit = false;
    [SerializeField] bool runningFromPolice = false;
    //How many times faster do the criminal want to go compared to the road speed limit
    [SerializeField] float criminalSpeedFactor = 1.5f;


    private void Start()
    {
        wheelController = GetComponent<WheelDrive>();
        rb = GetComponent<Rigidbody>();
        aiPathProgressTracker = GetComponent<PathNodeProgressTracker>();

        currentNode = FindClosestNode();

        if (endNode)
        {
            SetNewEndTargetNode(endNode, null, false);
        }
        else
        {
            CalculatePathToRandomNode(null);
        }
        aiPathProgressTracker.UpdatePath(path, currentNode);

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
        //Check State of the AI
        blockingCar = null;
        searchBoxes.Clear();
        searchSpheres.Clear();
        CheckIfLostConnection();
        if (lostConnection)
        {
            Debug.Log(this.transform.name + " lost connection, looking for a new pathnode");
            currentNode = FindClosestNode();
            CalculatePathToRandomNode(null);
            aiPathProgressTracker.UpdatePath(path, currentNode);
            lostConnection = false;
        }
        if (!backingCoroutineIsOn)
        {
            CheckIfStuck();
        }
        currentState = CalculateState(out List<GameObject> collisionsInMyPath);

        if (collisionsInMyPath.Count > 0 && currentState == AIState.Queue)
        {
            if (blockingCar && blockingCar.blockingCar == this)
            {
                if (blockingCar && blockingCar.currentState == AIState.Queue)
                {
                    currentState = CheckTrafficRulePriority(blockingCar) ? AIState.Drive : AIState.Queue;
                }
            }
        }

        //Switch-case on the state
        switch (currentState)
        {
            case AIState.Drive:
                UpdatePath();
                steerPercentage = SteerTowardsPoint(aiPathProgressTracker.target);
                speedToHold = CalculateSpeedToHold();
                torquePercentage = CalculateTorqueAndBrakes();
                break;
            case AIState.StopAtNextPathNode:
                if (Vector3.Distance(transform.position, path[0].transform.position) < kmhSpeed - 10)
                {
                    UpdatePath();
                }
                steerPercentage = SteerTowardsPoint(aiPathProgressTracker.target);
                speedToHold = CalculateSpeedToHold();
                speedToHold = Mathf.Min(speedToHold, Vector3.Distance(this.transform.position, path[0].transform.position));
                torquePercentage = CalculateTorqueAndBrakes();
                break;
            case AIState.Queue:
                if (CheckIfAllowedToPass())
                {
                    UpdatePath();
                }
                float distanceToClosestObstacle = GetDistanceToObstacle(this.transform.position + transform.forward * carLength / 2, GetClosestObject(this.transform.position, collisionsInMyPath));
                steerPercentage = SteerTowardsPoint(aiPathProgressTracker.target);
                speedToHold = CalculateSpeedToHold();
                speedToHold = Mathf.Min(speedToHold, distanceToClosestObstacle);
                torquePercentage = CalculateTorqueAndBrakes();
                break;
            case AIState.AvoidCollision:
                if (CheckIfAllowedToPass())
                {
                    UpdatePath();
                }
                distanceToClosestObstacle = GetDistanceToObstacle(this.transform.position + transform.forward * carLength / 2, GetClosestObject(this.transform.position, collisionsInMyPath));
                steerPercentage = SteerTowardsPoint(aiPathProgressTracker.target);
                SteerAwayFromCollision(GetClosestObject(this.transform.position, collisionsInMyPath), ref steerPercentage);
                speedToHold = CalculateSpeedToHold();
                speedToHold = Mathf.Min(speedToHold, distanceToClosestObstacle);
                torquePercentage = CalculateTorqueAndBrakes();
                break;
            case AIState.Stopping:
                UpdatePath();
                steerPercentage = SteerTowardsPoint(aiPathProgressTracker.target);
                speedToHold = 0;
                torquePercentage = CalculateTorqueAndBrakes();
                break;
            case AIState.BackingFromStuck:
                UpdatePath();
                if (!backingCoroutineIsOn)
                {
                    StartCoroutine(BackFromStuckCoroutine());
                }
                steerPercentage = -SteerTowardsPoint(aiPathProgressTracker.target);
                speedToHold = 5;
                torquePercentage = -CalculateTorqueAndBrakes();
                break;
            case AIState.GiveWayEmergency:
                if (CheckIfAllowedToPass())
                {
                    UpdatePath();
                }
                if (collisionsInMyPath.Count > 0)
                {
                    distanceToClosestObstacle = GetDistanceToObstacle(this.transform.position + transform.forward * carLength / 2, GetClosestObject(this.transform.position, collisionsInMyPath));
                }
                else
                {
                    distanceToClosestObstacle = float.MaxValue;
                }
                steerPercentage = SteerTowardsPoint(aiPathProgressTracker.target);
                speedToHold = CalculateSpeedToHold();
                if (IsPoliceNearby(out PoliceVehicle policeVehicle))
                {
                    GiveWayToPolice(policeVehicle, ref speedToHold, ref steerPercentage);
                    float distanceToPolice = Vector3.Distance(this.transform.position, policeVehicle.transform.position);
                    distanceToClosestObstacle = Mathf.Min(distanceToClosestObstacle, distanceToPolice);
                    speedToHold = distanceToPolice < carLength * 2 ? 0 : Mathf.Min(speedToHold, distanceToClosestObstacle);
                }
                torquePercentage = CalculateTorqueAndBrakes();
                break;
        }
        //Apply to wheels
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
            //Debug.Log("Path to: '" + target.transform.name + target.transform.position + "', from '" + currentNode.transform.name + currentNode.transform.position + "' could not be found since it was too far away. A shorter route towards it was created");
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
            //Debug.Log("Path to: '" + target.transform.name + target.transform.position + "', from '" + currentNode.transform.name + currentNode.transform.position + "' was found without issue");
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
    private void CalculatePathToRandomNode(PathNode nodeToAvoid)
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
                CalculatePathToRandomNode(nodeToAvoid);
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
    #endregion

    /// <summary>
    /// Calculates which state the CarAI should have
    /// </summary>
    private AIState CalculateState(out List<GameObject> possibleCollisions)
    {
        AIState newState = AIState.Drive;
        possibleCollisions = new List<GameObject>();
        if (debugStop || path.Count == 0 || currentNode == null)
        {
            return AIState.Stopping;
        }

        if (ignoreStopsAndTrafficLights || CheckIfAllowedToPass())
        {
            newState = AIState.Drive;
        }
        else
        {
            newState = AIState.StopAtNextPathNode;
        }

        if (DetectCollisionOnWaypoints(out GameObject closestWaypointCollision))
        {
            possibleCollisions.Add(closestWaypointCollision);
            if (!recklessDriver)
            {
                newState = AIState.Queue;
            }
            else
            {
                newState = AIState.AvoidCollision;
            }
        }

        //Check if anything is on our side of which we are turning
        if (turningDirection != Turn.Straight && CheckSideSensors(out GameObject carOnTheSide))
        {
            possibleCollisions.Add(carOnTheSide);
            if (!recklessDriver)
            {
                newState = AIState.Queue;
            }
            else
            {
                newState = AIState.AvoidCollision;
            }
        }

        if (!runningFromPolice && IsPoliceNearby(out PoliceVehicle closestPolice))
        {
            newState = AIState.GiveWayEmergency;
        }
        else if (runningFromPolice)
        {
            //insert check for being pulled over...
        }

        for (int i = 0; i < possibleCollisions.Count; i++)
        {
            if (possibleCollisions[i].transform.parent.GetComponent<CarAI>() != null)
            {
                blockingCar = possibleCollisions[i].transform.parent.GetComponent<CarAI>();
                break;
            }
        }

        ////Apply logic if we are stuck
        //if (isStuck)
        //{
        //    switch (newState)
        //    {
        //        case AIState.Drive:
        //            newState = AIState.BackingFromStuck;
        //            break;
        //        case AIState.StopAtNextPathNode:
        //            break;
        //        case AIState.Queue:
        //            //Only allow statechange if both cars are blocking eachother
        //            if (blockingCar && blockingCar.blockingCar == this)
        //            {
        //                if (blockingCar && blockingCar.currentState == AIState.Queue)
        //                {
        //                    newState = CheckTrafficRulePriority(blockingCar) ? AIState.Drive : AIState.Queue;
        //                }
        //                else
        //                {
        //                    newState = AIState.BackingFromStuck;
        //                }
        //            }
        //            break;
        //        case AIState.AvoidCollision:
        //            newState = AIState.BackingFromStuck;
        //            break;
        //        case AIState.Stopping:
        //            break;
        //        case AIState.BackingFromStuck:
        //            newState = AIState.BackingFromStuck;
        //            break;
        //        case AIState.GiveWayEmergency:
        //            break;
        //    }
        //}

        return newState;
    }


    string terrainTag = "Terrain";
    string vehicleTag = "Vehicle";
    string pathNodeTag = "PathNode";
    /// <summary>
    /// Checks for any collisions towards a set of waypoints this car is going to
    /// Also checks for collisions right in front of this car
    /// </summary>
    private bool DetectCollisionOnWaypoints(out GameObject firstCollision)
    {
        firstCollision = null;
        float minimumDistanceToCheck = carLength;
        float collisionDetectionLength = kmhSpeed;

        List<Vector3> waypoints = aiPathProgressTracker.waypoints;
        List<Collider> collisions = new List<Collider>();
        List<GameObject> collisionObjects = new List<GameObject>();

        //Check directly infront of the car
        SearchSphere searchSphere = new SearchSphere(this.transform.position + (transform.forward * (carLength)), carWidth / 2 * 1.25f);
        searchSpheres.Add(searchSphere);
        collisions.AddRange(Physics.OverlapSphere(searchSphere.pos, searchSphere.radius));

        //Check waypoints within the minimum and maximum distance
        for (int i = 0; i < waypoints.Count; i++)
        {
            //If the distance between this car and the waypoint is short enough, check if there is a collision there
            if (Vector3.Distance(transform.position, waypoints[i]) < collisionDetectionLength)
            {
                if (Vector3.Distance(transform.position, waypoints[i]) > minimumDistanceToCheck)
                {
                    collisions.AddRange(Physics.OverlapSphere(waypoints[i], carWidth / 2));
                }
            }
            else
            {
                break;
            }
        }

        for (int i = 0; i < collisions.Count; i++)
        {
            if (collisions[i].gameObject.tag == terrainTag || collisions[i].gameObject.tag == pathNodeTag)
            {
                collisions.RemoveAt(i);
                i--;
            }
            else if (collisions[i].gameObject.tag == vehicleTag && collisions[i].transform.parent == this.transform)
            {
                collisions.RemoveAt(i);
                i--;
            }
            else
            {
                collisionObjects.Add(collisions[i].gameObject);
            }
        }

        //Display the search
        for (int i = 0; i < collisionObjects.Count; i++)
        {
            searchSpheres.Add(new SearchSphere(collisionObjects[i].transform.position, carWidth / 2));
        }

        if (collisionObjects.Count > 0)
        {
            firstCollision = GetClosestObject(transform.position, collisionObjects);
            Debug.DrawLine(this.transform.position, firstCollision.transform.position, Color.red);
        }
        return firstCollision != null;
    }

    /// <summary>
    /// Returns true if there is another car on the side of which we are turning. Outs the other vehicle
    /// </summary>
    private bool CheckSideSensors(out GameObject carOnTheSide)
    {
        carOnTheSide = null;
        Vector3 centerPoint;

        int detectors = Mathf.CeilToInt(carLength / carWidth);

        if (turningDirection == Turn.Left)
        {
            centerPoint = this.transform.position - (transform.right * carWidth);
        }
        else
        {
            centerPoint = this.transform.position + (transform.right * carWidth);
        }

        List<Collider> collisions = new List<Collider>();
        List<GameObject> collisionObjects = new List<GameObject>();

        //Start at the front of the car
        centerPoint += transform.forward * (carLength / 2);

        for (int i = 0; i <= detectors; i++)
        {
            SearchSphere detector = new SearchSphere(centerPoint, carWidth / 2);
            collisions.AddRange(Physics.OverlapSphere(detector.pos, detector.radius));
            searchSpheres.Add(detector);
            centerPoint -= transform.forward * ((carLength) / detectors);
        }


        for (int i = 0; i < collisions.Count; i++)
        {
            if (collisions[i].gameObject.tag != vehicleTag)
            {
                collisions.RemoveAt(i);
                i--;
            }
            else if (collisions[i].gameObject.tag == vehicleTag && collisions[i].transform.parent == this.transform)
            {
                collisions.RemoveAt(i);
                i--;
            }
            else
            {
                collisionObjects.Add(collisions[i].gameObject);
            }
        }

        if (collisionObjects.Count > 0)
        {
            carOnTheSide = GetClosestObject(transform.position, collisionObjects);
            Debug.DrawLine(this.transform.position, carOnTheSide.transform.position, Color.red);
        }
        return carOnTheSide != null;
    }

    /// <summary>
    /// Returns the closest objects from the origin
    /// </summary>
    public static GameObject GetClosestObject(Vector3 origin, List<GameObject> allObjects)
    {
        float distance = float.MaxValue;
        GameObject closest = null;
        for (int i = 0; i < allObjects.Count; i++)
        {
            float testDistance = Vector3.Distance(origin, allObjects[i].transform.position);
            if (testDistance < distance)
            {
                distance = testDistance;
                closest = allObjects[i];
            }
        }
        return closest;
    }
    public static Vector3 GetClosestVector3(Vector3 origin, List<Vector3> allPositions)
    {
        float distance = float.MaxValue;
        Vector3 closest = Vector3.zero;
        for (int i = 0; i < allPositions.Count; i++)
        {
            float testDistance = Vector3.Distance(origin, allPositions[i]);
            if (testDistance < distance)
            {
                distance = testDistance;
                closest = allPositions[i];
            }
        }
        return closest;
    }

    /// <summary>
    /// Updates the path and calculates a new route if we are close enough to the pathnode first in the list to visit
    /// Also sets the state of our turning enum
    /// </summary>
    private void UpdatePath()
    {
        if (Vector3.Distance(transform.position, path[0].transform.position) < nodeAcceptanceDistance)
        {
            //Leave old node
            path[0].RemoveCarFromNode(this);
            currentNode = path[0];
            path.RemoveAt(0);

            //If we have arrived at the last node
            if (path.Count == 0 && endNode)
            {
                currentNode = null;
                return;
            }
            //If there are more than one node left to visit
            if (!endNode && path.Count <= 1)
            {
                //Create new path
                path.Clear();
                CalculatePathToRandomNode(null);
            }
            aiPathProgressTracker.UpdatePath(path, currentNode);
            //roadSpeedLimit = currentNode.GetComponent<PathNode>().GetRoadSpeedLimit();
            path[0].AddCarToNode(this);
            turningDirection = CheckTurning();
        }
    }

    /// <summary>
    /// Returns a speed to hold dependent on how much this car is steering and how much the predicted future path curves
    /// </summary>
    private float CalculateSpeedToHold()
    {
        float newSpeed = currentNode.GetRoadSpeedLimit();
        newSpeed *= (1 - Mathf.Abs(steerPercentage));
        newSpeed *= (1 - Mathf.Abs(aiPathProgressTracker.curvePercentage));
        if (recklessDriver || ignoreSpeedLimit)
        {
            newSpeed *= criminalSpeedFactor;
        }
        newSpeed = Mathf.Max(10, newSpeed);
        return newSpeed;
    }

    /// <summary>
    /// Calculates how much the wheels should turn to go towards the target point
    /// </summary>
    private float SteerTowardsPoint(Vector3 point)
    {
        if (point == rb.position)
        {
            return 0;
        }
        Vector3 relativeVector = transform.InverseTransformPoint(point);
        relativeVector /= relativeVector.magnitude;
        return (relativeVector.x / relativeVector.magnitude);
    }

    /// <summary>
    /// Returns the torque recommended dependent on the speed to hold
    /// Also sets the brakes of the car to true or false
    /// </summary>
    private float CalculateTorqueAndBrakes()
    {
        if (kmhSpeed > speedToHold + 5 || speedToHold < 5)
        {
            SetBrake(true);
            return 0;
        }
        else
        {
            SetBrake(false);
        }

        if (kmhSpeed < speedToHold - 10)
        {
            return 1;
        }
        else
        {
            return 1 - (kmhSpeed / speedToHold);
        }
    }

    float closestNodeSearchDistance = 10f;
    /// <summary>
    /// Find the closest pathnode
    /// </summary>
    private PathNode FindClosestNode()
    {
        if (currentNode)
        {
            currentNode.RemoveCarFromNode(this);
            currentNode = null;
        }

        Collider[] allOverlappingColliders = Physics.OverlapSphere(transform.position, closestNodeSearchDistance);
        PathNode closestNode = null;
        float distance = float.MaxValue;
        foreach (var item in allOverlappingColliders)
        {
            if (item.GetComponent<PathNode>() != null)
            {
                float testDistance = Vector3.Distance(transform.position, item.transform.position);
                if (testDistance < distance)
                {
                    closestNode = item.GetComponent<PathNode>();
                    distance = testDistance;
                }
            }
        }

        if (closestNode == null)
        {
            closestNodeSearchDistance *= 2;
            closestNode = FindClosestNode();
            closestNodeSearchDistance = 10f;
        }
        return closestNode;
    }

    bool lostConnection = false;
    bool lostConnectionCoroutineInitialized = false;
    float maxDistanceToConnection = 1;
    /// <summary>
    /// Check if we are too far away from our target waypoint and is behind this car
    /// </summary>
    private void CheckIfLostConnection()
    {
        float distanceToProgressTarget = Vector3.Distance(this.transform.position, path[0].transform.position);
        if (distanceToProgressTarget > kmhSpeed + maxDistanceToConnection)
        {
            if (Vector3.Distance(this.transform.position + transform.forward.normalized, path[0].transform.position) > distanceToProgressTarget)
            {
                if (!lostConnectionCoroutineInitialized)
                {
                    StartCoroutine(CheckIfLostConnectionCoroutine());
                }
                return;
            }
        }
        if (lostConnectionCoroutineInitialized)
        {
            StopCoroutine(CheckIfLostConnectionCoroutine());
        }
    }

    IEnumerator CheckIfLostConnectionCoroutine()
    {
        lostConnectionCoroutineInitialized = true;
        yield return new WaitForSeconds(1f);
        PathNode nodeToCheck = path[0];
        float distanceToProgressTarget = Vector3.Distance(this.transform.position, path[0].transform.position);
        yield return new WaitForSeconds(0.1f);
        float newDistance = Vector3.Distance(this.transform.position, path[0].transform.position);
        //See if it still is the same node as before and it has not been updated
        if (nodeToCheck == path[0])
        {
            lostConnection = (newDistance > distanceToProgressTarget);
        }
        else
        {
            lostConnection = false;
        }
        lostConnectionCoroutineInitialized = false;
    }

    /// <summary>
    /// Set the car brakes and turn on/off brake-lights
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
    /// Returns true if there is a red light to stop for. Or if there are cars needed to wait for
    /// </summary>
    public bool CheckIfAllowedToPass()
    {
        if (path.Count > 1)
        {
            return path[0].IsAllowedToPass(path[1]);
        }
        else
        {
            return path[0].IsAllowedToPass(null);
        }
    }

    /// <summary>
    /// Starts a coroutine with a timer if we appear to be stuck
    /// For instance if our speed is lower than expected and is not increasing
    /// </summary>
    private void CheckIfStuck()
    {
        //Checking if stuck
        //if (kmhSpeed < speedToHold && kmhSpeed <= lastSpeedCheck)
        if (kmhSpeed + stuckSpeedSensistivity < speedToHold && kmhSpeed <= lastSpeedCheck)
        {
            if (!checkingIfStuck)
            {
                StartCoroutine(CheckIfStuckCoroutine());
            }
        }
        else
        {
            StopCoroutine(CheckIfStuckCoroutine());
            isStuck = false;
            checkingIfStuck = false;
        }
        lastSpeedCheck = kmhSpeed;
    }

    IEnumerator CheckIfStuckCoroutine()
    {
        checkingIfStuck = true;
        yield return new WaitForSeconds(checkTimeBeforeStuck);
        isStuck = (kmhSpeed + stuckSpeedSensistivity < speedToHold);
        //isStuck = (kmhSpeed < speedToHold);
        checkingIfStuck = false;
    }

    bool backingCoroutineIsOn = false;
    IEnumerator BackFromStuckCoroutine()
    {
        backingCoroutineIsOn = true;
        float timeBacked = 0f;
        while (timeBacked < timeToBack)
        {
            yield return new WaitForEndOfFrame();
            timeBacked += Time.deltaTime;
        }
        isStuck = false;
        backingCoroutineIsOn = false;
    }

    /// <summary>
    /// Check which direction we are turning from the next pathnode
    /// </summary>
    private Turn CheckTurning()
    {
        if (path.Count > 1)
        {
            for (int i = 0; i < path[0].outChoices.Count; i++)
            {
                if (path[1] == path[0].outChoices[i].outNode)
                {
                    Turn newTurn = path[0].outChoices[i].turnDirection;

                    //If we are turning
                    if (newTurn != Turn.Straight)
                    {
                        performingTurn = true;
                        turnPathNodeCount = 0;
                        return newTurn;
                    }
                    else
                    {
                        //If we are going straight from our next pathnode we still should use our blinkers through this turn we are currently making
                        //Therefore we check if we are just passing through and out of the pathnode indicating a turn, we dont change state untill we pass the next pathnode (2)
                        if (performingTurn)
                        {
                            turnPathNodeCount++;
                            if (turnPathNodeCount == 2)
                            {
                                performingTurn = false;
                                return newTurn;
                            }
                            else
                            {
                                return turningDirection;
                            }
                        }
                        else //If not turning, and is currently not performing a turn from before simply return the new turn, which is straight
                        {
                            return newTurn;
                        }
                    }
                }
            }
            Debug.LogWarning("Did not find the correct direction, check outnode choices");
        }
        return Turn.Straight;
    }

    /// <summary>
    /// Check if this car has priority over another
    /// </summary>
    private bool CheckTrafficRulePriority(CarAI otherCar)
    {
        //Debug.Log("Checking priority between this car: " + this.transform.name + " and other car: " + otherCar.transform.name);
        if (turningDirection == otherCar.turningDirection)
        {
            return true;
        }

        if (turningDirection == Turn.Left)
        {
            return false;
        }
        else if (turningDirection == Turn.Right)
        {
            if (otherCar.turningDirection == Turn.Straight)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        else //If turning direction is straight
        {
            if (otherCar.turningDirection == Turn.Straight)
            {
                Debug.Log("2 cars going straight are queuing for eachother, both is forced to go on:" + this.transform.name + " and " + otherCar.transform.name);
                return true;
            }
            else if (otherCar.turningDirection == Turn.Right)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        ////If we are turning (changling lane)
        //if (turningDirection != Turn.Straight)
        //{
        //    //if the other car is turning to the left
        //    if (otherCar.turningDirection == Turn.Left)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}
        //else
        //{
        //    return true;
        //}
    }

    /// <summary>
    /// Check if there is a police car nearby and out the closest one
    /// </summary>
    private bool IsPoliceNearby(out PoliceVehicle policeVehicle)
    {
        float distanceToCheck = Mathf.Max(10, kmhSpeed);
        Collider[] colliders = Physics.OverlapSphere(transform.position, distanceToCheck);
        float closestDistance = float.MaxValue;
        policeVehicle = null;
        foreach (var item in colliders)
        {
            if (item.gameObject.tag == vehicleTag && item.transform.parent.GetComponent<PoliceVehicle>() != null)
            {
                PoliceVehicle detectedPolice = item.transform.parent.GetComponent<PoliceVehicle>();
                if (detectedPolice.usingSirens)
                {
                    float testDistance = Vector3.Distance(transform.position, item.transform.position);
                    if (testDistance < closestDistance)
                    {
                        closestDistance = testDistance;
                        policeVehicle = detectedPolice;
                    }
                }
            }
        }

        //If we found any car return true. the closest car will be out'ed
        if (policeVehicle)
        {
            Debug.DrawLine(transform.position, policeVehicle.transform.position, Color.red);
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Provide steering to allow space for the police car
    /// </summary>
    private void GiveWayToPolice(PoliceVehicle vehicleToGiveWayTo, ref float speed, ref float steering)
    {
        float steerDirectionTowardsCar = SteerTowardsPoint(vehicleToGiveWayTo.transform.position);
        float distanceToPolice = Vector3.Distance(vehicleToGiveWayTo.transform.position, this.transform.position);

        //take the middle ground between where i want to go and to avoid
        float diviance = -steerDirectionTowardsCar / distanceToPolice;
        steering += diviance;
        float speedReduction = distanceToPolice;
        speed = Mathf.Max(5, distanceToPolice * 2);
    }

    /// <summary>
    /// Provide steering away from the objectToAvoid dependent on our waypoint path and the distance to the obstacle
    /// </summary>
    private void SteerAwayFromCollision(GameObject objectToAvoid, ref float steering)
    {
        float distanceToObstacle = Vector3.Distance(objectToAvoid.transform.position, this.transform.position);

        Vector3 waypointClosestToCollision = GetClosestVector3(objectToAvoid.transform.position, aiPathProgressTracker.waypoints);

        float steerPercentageToWaypoint = SteerTowardsPoint(waypointClosestToCollision);
        float steerPercentageToObstacle = SteerTowardsPoint(objectToAvoid.transform.position);

        //If obstacle is left of the path
        if (steerPercentageToObstacle < steerPercentageToWaypoint)
        {
            steering += 1f / distanceToObstacle;
        }
        else
        {
            steering -= 1f / distanceToObstacle;
        }
    }

    /// <summary>
    /// Return the actual distance from the front of the car to the closest point of the obstacle using raycasts and physics
    /// </summary>
    private float GetDistanceToObstacle(Vector3 startPoint, GameObject obstacle)
    {
        if (Physics.Raycast(startPoint, obstacle.transform.position - startPoint, out RaycastHit hit, Mathf.Infinity))
        {
            if (hit.transform.gameObject == obstacle)
            {
                return Vector3.Distance(startPoint, hit.point);
            }
        }
        return Vector3.Distance(startPoint, obstacle.transform.position);
    }

    #region Sensors
    [Header("Sensor Info")]
    [SerializeField] float carWidth = 2f;
    [SerializeField] float carLength = 3f;
    [SerializeField] float carHeight = 2.5f;
    #endregion

    #region GizmosAndEditor
    [Header("Editor")]
    public Color lineToNode = Color.yellow;
    public bool showLineToNextNode = false;
    List<SearchBox> searchBoxes = new List<SearchBox>();
    List<SearchSphere> searchSpheres = new List<SearchSphere>();

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

        for (int i = 0; i < searchBoxes.Count; i++)
        {
            Gizmos.DrawWireCube(searchBoxes[i].pos, searchBoxes[i].boxDimensions);
        }

        for (int i = 0; i < searchSpheres.Count; i++)
        {
            Gizmos.DrawWireSphere(searchSpheres[i].pos, searchSpheres[i].radius);
        }
    }
    #endregion
}

public struct SearchBox
{
    public Vector3 pos;
    public Vector3 boxDimensions;

    public SearchBox(Vector3 pos, Vector3 boxDimensions)
    {
        this.pos = pos;
        this.boxDimensions = boxDimensions;
    }
}

public struct SearchSphere
{
    public Vector3 pos;
    public float radius;

    public SearchSphere(Vector3 pos, float radius)
    {
        this.pos = pos;
        this.radius = radius;
    }
}

#region Archive
//FIXEDUPDATE
////Start with resetting values
//steerPercentage = 0;
//float torquePercentage = 0;
//braking = false;
//float distanceToObstacle = float.MaxValue;

////Stopping if prompted by debug-stop-bool (panic-button)
//if (debugStop)
//{
//    currentState = AIState.Stopping;
//    wheelController.AIDriver(0, 0, true);
//    return;
//}

////Update current node and path if we are in acceptable range
//if (ignoreStopsAndTrafficLights || CheckIfAllowedToPass())
//{
//    currentState = AIState.Drive;
//    UpdatePathNode();
//}
//else
//{
//    currentState = AIState.StopAtNextPathNode;
//}

////Calculate steering
//if (aiPathProgressTracker)
//{
//    progressTrackerAim = aiPathProgressTracker.target;
//}
//steerPercentage = aiPathProgressTracker != null ? SteerTowardsPoint(progressTrackerAim) : SteerTowardsNode(path[0]);

////Check for collisions forward
//if (CheckForCollision(steerPercentage, out RaycastHit collision))
//{
//    distanceToObstacle = Vector3.Distance(rb.position, collision.point);
//    if (collision.transform.tag == "WorldObject")
//    {
//        currentState = kmhSpeed < 5 ? AIState.BackingFromStuck : AIState.AvoidCollision;
//    }
//    else if (collision.transform.tag == "Vehicle")
//    {
//        currentState = recklessDriver ? AIState.AvoidCollision : AIState.Queue;
//    }
//    else
//    {
//        Debug.LogWarning("No function found for response to collison object tag");
//    }
//}

////Check if anything is on our side of which we are turning
//if (!recklessDriver && turningDirection != Turn.Straight && SideSensorChecks(ref steerPercentage, out collision))
//{
//    if (collision.transform.tag == "Vehicle" && collision.transform.GetComponent<CarAI>() != null)
//    {
//        if (!CheckLaneChangePriority(collision.transform.GetComponent<CarAI>()))
//        {
//            distanceToObstacle = Vector3.Distance(rb.position, collision.point);
//            currentState = AIState.Queue;
//        }
//    }
//}

//if (currentState == AIState.Drive || currentState == AIState.AvoidCollision)
//{
//    //Checking if stuck
//    if (kmhSpeed + stuckSpeedSensistivity < speedToHold && kmhSpeed <= lastSpeedCheck)
//    {
//        if (!checkingIfStuck)
//        {
//            StartCoroutine("CheckIfStuck");
//        }
//    }
//    else
//    {
//        StopCoroutine("CheckIfStuck");
//        isStuck = false;
//        checkingIfStuck = false;
//    }
//}
//else
//{
//    StopCoroutine("CheckIfStuck");
//    isStuck = false;
//    checkingIfStuck = false;
//}
//lastSpeedCheck = kmhSpeed;

////If target vector3 is behind the car
//if (Vector3.Distance(aiPathProgressTracker ? progressTrackerAim : path[0].transform.position, rb.position - transform.forward.normalized) < Vector3.Distance(aiPathProgressTracker ? progressTrackerAim : path[0].transform.position, rb.position))
//{
//    currentState = AIState.BackingFromStuck;
//}

//if (isStuck)
//{
//    currentState = AIState.BackingFromStuck;
//}

//if (IsPoliceNearby(out PoliceVehicle policeVehicle) && currentState != AIState.StopAtNextPathNode)
//{
//    currentState = AIState.GiveWayEmergency;
//}

//switch (currentState)
//{
//    case AIState.Drive:
//        {
//            float newSpeed = CalculateSpeedToHold();
//            newSpeed = Mathf.Max(10, newSpeed); //minimum
//            SetSpeedToHold(newSpeed);
//        }
//        break;
//    case AIState.StopAtNextPathNode:
//        {
//            float distanceToStop = Vector3.Distance(path[0].transform.position, transform.position);
//            float newSpeed = distanceToStop < 3 + (carLength / 2) ? 0 : Mathf.Min(roadSpeedLimit, distanceToStop);
//            SetSpeedToHold(newSpeed);
//        }
//        break;
//    case AIState.Queue:
//        {
//            float newSpeed = distanceToObstacle < 3 + (carLength / 2) ? 0 : Mathf.Min(roadSpeedLimit, distanceToObstacle);
//            SetSpeedToHold(newSpeed);
//        }
//        break;
//    case AIState.AvoidCollision:
//        {
//            AvertFromCollision(ref steerPercentage, out RaycastHit closestCollision);
//            float newSpeed = CalculateSpeedToHold();
//            float distanceToCollision = Vector3.Distance(rb.position, closestCollision.point);
//            if (distanceToCollision < kmhSpeed / 3.6f)
//            {
//                newSpeed = distanceToCollision;
//            }
//            newSpeed = Mathf.Max(10, newSpeed); //minimum
//            SetSpeedToHold(newSpeed);
//        }
//        break;
//    case AIState.Stopping:
//        {
//            steerPercentage = 0;
//            SetSpeedToHold(0);
//        }
//        break;
//    case AIState.BackingFromStuck:
//        {
//            SetSpeedToHold(5);
//        }
//        break;
//    case AIState.GiveWayEmergency:
//        {
//            float newSpeed = CalculateSpeedToHold();
//            newSpeed = Mathf.Max(10, newSpeed); //minimum
//            GiveWayToPolice(policeVehicle, ref newSpeed, ref steerPercentage);
//            newSpeed = distanceToObstacle < 3 + (carLength / 2) ? 0 : Mathf.Min(newSpeed, distanceToObstacle);
//            SetSpeedToHold(newSpeed);
//        }
//        break;
//}

//torquePercentage = Drive((currentState == AIState.BackingFromStuck));

////Invert values if we are reversing
//if (currentState == AIState.BackingFromStuck)
//{
//    steerPercentage *= -1;
//    torquePercentage *= -1;
//}

////Send values to the tires of the car
//wheelController.AIDriver(steerPercentage, torquePercentage, braking);



//PATHFINDING
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



//SENSORS
////How much does the speed affect sensor length, length = kmh / thisNumber
//[SerializeField] float sensorLengthSpeedDependency = 4f;

////Foward sensors
//[SerializeField] float minimumForwardSensorLength = 2f;
//float frontSensorLength;

////Angled forward sensors
//[SerializeField] float minimumAngledSensorLength = 2f;
//float frontAngledSensorLength;
//[SerializeField] float maxAngle = 45;
//float frontSensorsAngle;

//int numberOfSideSensors = 3;

///// <summary>
///// Check the sensors in front of the car to slow down and queue up if there is another car infront of this one. Outs the closest collision
///// </summary>
//private bool CheckForCollision(float turningPercentage, out RaycastHit closestCollision)
//{
//    Vector3 sensorStartPos = transform.position;
//    sensorStartPos += transform.forward * carLength / 2;

//    frontSensorLength = kmhSpeed / sensorLengthSpeedDependency;
//    frontSensorLength = Mathf.Max(frontSensorLength, minimumForwardSensorLength);

//    frontSensorsAngle = maxAngle * Mathf.Abs(turningPercentage);
//    frontAngledSensorLength = minimumAngledSensorLength * 2 * Mathf.Abs(turningPercentage);
//    frontAngledSensorLength = Mathf.Max(frontAngledSensorLength, minimumAngledSensorLength);


//    bool collisionDetected = false;
//    bool sensorCollision = false;
//    RaycastHit hit;
//    List<RaycastHit> collisions = new List<RaycastHit>();

//    //Front center sensor
//    sensorCollision = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
//    if (sensorCollision)
//    {
//        if (hit.transform.tag == "Vehicle")
//        {
//            collisions.Add(hit);
//            collisionDetected = true;
//        }
//        else if (hit.transform.tag == "WorldObject")
//        {
//            collisions.Add(hit);
//            collisionDetected = true;
//        }
//    }

//    //Check front right sensor
//    sensorStartPos += transform.right * (carWidth / 2);
//    sensorCollision = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
//    if (sensorCollision)
//    {
//        if (hit.transform.tag == "Vehicle")
//        {
//            collisions.Add(hit);
//            collisionDetected = true;
//        }
//        else if (hit.transform.tag == "WorldObject")
//        {
//            collisions.Add(hit);
//            collisionDetected = true;
//        }
//    }

//    //Front left sensor
//    sensorStartPos -= (transform.right * carWidth);
//    sensorCollision = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
//    if (sensorCollision)
//    {
//        if (hit.transform.tag == "Vehicle")
//        {
//            collisions.Add(hit);
//            collisionDetected = true;
//        }
//        else if (hit.transform.tag == "WorldObject")
//        {
//            collisions.Add(hit);
//            collisionDetected = true;
//        }
//    }

//    //if turning left
//    if (turningPercentage < -0.1f)
//    {
//        //Front angled left sensor
//        sensorCollision = UseSensor(sensorStartPos, Quaternion.AngleAxis(-frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength);
//        if (sensorCollision)
//        {
//            if (hit.transform.tag == "Vehicle")
//            {
//                collisions.Add(hit);
//                collisionDetected = true;
//            }
//            else if (hit.transform.tag == "WorldObject")
//            {
//                collisions.Add(hit);
//                collisionDetected = true;
//            }
//        }
//    }

//    sensorStartPos += (transform.right * carWidth);
//    //if turning right
//    if (turningPercentage > 0.1f)
//    {
//        //Front angled right sensor
//        sensorCollision = UseSensor(sensorStartPos, Quaternion.AngleAxis(frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength);
//        if (sensorCollision)
//        {
//            if (hit.transform.tag == "Vehicle")
//            {
//                collisions.Add(hit);
//                collisionDetected = true;
//            }
//            else if (hit.transform.tag == "WorldObject")
//            {
//                collisions.Add(hit);
//                collisionDetected = true;
//            }
//        }
//    }

//    closestCollision = new RaycastHit(); //needed for acceptance, overriden in for loop anyway if any hits are found
//    float distanceToClosestCollision = float.MaxValue;
//    for (int i = 0; i < collisions.Count; i++)
//    {
//        float testDistance = Vector3.Distance(rb.position, collisions[i].point);
//        if (testDistance < distanceToClosestCollision)
//        {
//            distanceToClosestCollision = testDistance;
//            closestCollision = collisions[i];
//        }
//    }
//    return collisionDetected;
//}

///// <summary>
///// Checks the sensors at the side the car is turning to avoid going into other cars. Outs the closest collision
///// </summary>
//private bool SideSensorChecks(ref float turningPercentage, out RaycastHit closestCollision)
//{
//    Vector3 carFront = transform.position;
//    carFront += transform.forward * carLength / 2;
//    float sensorLength = carWidth * 3;

//    bool collisionDetected = false;

//    RaycastHit hit;
//    List<RaycastHit> collisions = new List<RaycastHit>();

//    if (turningDirection == Turn.Right)
//    {
//        bool sensorCollision = false;
//        //Right sensors
//        for (int sideSensor = 0; sideSensor < numberOfSideSensors; sideSensor++)
//        {
//            Vector3 sensorPos = carFront - transform.forward * (sideSensor * carLength / numberOfSideSensors);
//            sensorCollision = UseSensor(sensorPos, Quaternion.AngleAxis(89, transform.up) * transform.forward, out hit, sensorLength);
//            if (sensorCollision)
//            {
//                if (hit.transform.tag == "Vehicle")
//                {
//                    collisions.Add(hit);
//                    //turningPercentage -= (float)((float)1 / (float)numberOfSideSensors) * 0.1f;
//                    collisionDetected = true;
//                }
//            }
//        }
//        //Front angled right sensor
//        Vector3 angledRightPos = carFront + (transform.right * carWidth / 2);
//        sensorCollision = UseSensor(angledRightPos, Quaternion.AngleAxis(45, transform.up) * transform.forward, out hit, sensorLength);
//        if (sensorCollision)
//        {
//            if (hit.transform.tag == "Vehicle")
//            {
//                collisions.Add(hit);
//                //turningPercentage -= 0.1f;
//                collisionDetected = true;
//            }
//        }
//        //Right progress tracker sensor
//        Vector3 progressTrackerSensor = carFront + (transform.right * carWidth / 2);
//        sensorCollision = UseSensor(progressTrackerSensor, progressTrackerAim - progressTrackerSensor, out hit, sensorLength);
//        if (sensorCollision)
//        {
//            if (hit.transform.tag == "Vehicle")
//            {
//                collisions.Add(hit);
//                //turningPercentage -= 0.1f;
//                collisionDetected = true;
//            }
//        }
//    }
//    else if (turningDirection == Turn.Left)
//    {
//        bool sensorCollision = false;
//        //Left sensors
//        for (int sideSensor = 0; sideSensor < numberOfSideSensors; sideSensor++)
//        {
//            Vector3 sensorPos = carFront - transform.forward * (sideSensor * carLength / numberOfSideSensors);
//            sensorCollision = UseSensor(sensorPos, Quaternion.AngleAxis(-89, transform.up) * transform.forward, out hit, sensorLength);
//            if (sensorCollision)
//            {
//                if (hit.transform.tag == "Vehicle")
//                {
//                    collisions.Add(hit);
//                    //turningPercentage += (float)((float)1 / (float)numberOfSideSensors) * 0.1f;
//                    collisionDetected = true;
//                }
//            }
//        }
//        //Front angled left sensor
//        Vector3 angledLeftPos = carFront - (transform.right * carWidth / 2);
//        sensorCollision = UseSensor(angledLeftPos, Quaternion.AngleAxis(-45, transform.up) * transform.forward, out hit, sensorLength);
//        if (sensorCollision)
//        {
//            if (hit.transform.tag == "Vehicle")
//            {
//                collisions.Add(hit);
//                // turningPercentage += 0.1f;
//                collisionDetected = true;
//            }
//        }
//        //Left progress tracker sensor
//        Vector3 progressTrackerSensor = carFront - (transform.right * carWidth / 2);
//        sensorCollision = UseSensor(progressTrackerSensor, progressTrackerAim - progressTrackerSensor, out hit, sensorLength);
//        if (sensorCollision)
//        {
//            if (hit.transform.tag == "Vehicle")
//            {
//                collisions.Add(hit);
//                // turningPercentage += 0.1f;
//                collisionDetected = true;
//            }
//        }
//    }


//    closestCollision = new RaycastHit(); //needed for acceptance, overriden in for loop anyway if any hits are found
//    float distanceToClosestCollision = float.MaxValue;
//    for (int i = 0; i < collisions.Count; i++)
//    {
//        float testDistance = Vector3.Distance(rb.position, collisions[i].point);
//        if (testDistance < distanceToClosestCollision)
//        {
//            distanceToClosestCollision = testDistance;
//            closestCollision = collisions[i];
//        }
//    }
//    return collisionDetected;
//}

///// <summary>
///// Check the sensors around the car for obstacles and provide steering to avoid. Outs the closest collision
///// </summary>
//private bool AvertFromCollision(ref float turningPercentage, out RaycastHit closestCollision)
//{
//    Vector3 sensorStartPos = transform.position;
//    sensorStartPos += transform.forward * carLength / 2;

//    frontSensorLength = kmhSpeed / sensorLengthSpeedDependency;
//    frontSensorLength = Mathf.Max(frontSensorLength, minimumForwardSensorLength);

//    frontSensorsAngle = maxAngle * Mathf.Abs(turningPercentage);
//    frontAngledSensorLength = minimumAngledSensorLength * 2 * Mathf.Abs(turningPercentage);
//    frontAngledSensorLength = Mathf.Max(frontAngledSensorLength, minimumAngledSensorLength);

//    float avoidMultiplier = 0;
//    bool avoidingCollision = false;

//    bool collisionDetected;
//    RaycastHit hit;
//    List<RaycastHit> collisions = new List<RaycastHit>();

//    //Front right sensor
//    sensorStartPos += transform.right * (carWidth / 2);
//    collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
//    if (collisionDetected)
//    {
//        avoidMultiplier -= 1f;
//        avoidingCollision = true;
//        collisions.Add(hit);
//    }

//    //if steering right
//    if (turningPercentage > 0.1f)
//    {
//        //Front angled right sensor
//        collisionDetected = UseSensor(sensorStartPos, Quaternion.AngleAxis(frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength * Mathf.Abs(turningPercentage));
//        if (collisionDetected)
//        {
//            avoidMultiplier -= 0.1f;
//            avoidingCollision = true;
//            collisions.Add(hit);
//        }
//    }

//    //Front left sensor
//    sensorStartPos -= (transform.right * carWidth);
//    collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
//    if (collisionDetected)
//    {
//        avoidMultiplier += 1f;
//        avoidingCollision = true;
//        collisions.Add(hit);
//    }

//    //if steering left
//    if (turningPercentage < -0.1f)
//    {
//        //Front angled left sensor
//        collisionDetected = UseSensor(sensorStartPos, Quaternion.AngleAxis(-frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength * Mathf.Abs(turningPercentage));
//        if (collisionDetected)
//        {
//            avoidMultiplier += 0.1f;
//            avoidingCollision = true;
//            collisions.Add(hit);
//        }
//    }

//    //Front center sensor
//    {
//        collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
//        if (collisionDetected)
//        {
//            if (avoidMultiplier == 0)
//            {
//                if (hit.normal.x < 0)
//                {
//                    avoidMultiplier = -1;
//                }
//                else
//                {
//                    avoidMultiplier = 1;
//                }
//            }
//            avoidingCollision = true;
//            collisions.Add(hit);
//        }
//    }

//    //Check closest collision
//    closestCollision = new RaycastHit(); //needed for acceptance, overriden in for loop anyway if any hits are found
//    float distanceToClosestCollision = float.MaxValue;
//    for (int i = 0; i < collisions.Count; i++)
//    {
//        float testDistance = Vector3.Distance(rb.position, collisions[i].point);
//        if (testDistance < distanceToClosestCollision)
//        {
//            distanceToClosestCollision = testDistance;
//            closestCollision = collisions[i];
//        }
//    }

//    //Apply counter steering
//    if (avoidingCollision)
//    {
//        turningPercentage += avoidMultiplier * Mathf.Min(1, (kmhSpeed / 3.6f) / distanceToClosestCollision);
//        if (turningPercentage < -1)
//        {
//            turningPercentage = -1;
//        }
//        else if (turningPercentage > 1)
//        {
//            turningPercentage = 1;
//        }
//    }
//    return avoidingCollision;
//}

//[SerializeField] float criticalCollisionDistance = 2f;
//[SerializeField] float warningCollisionDistance = 5f;
//[SerializeField] float infoCollisionDistance = 10f;

///// <summary>
///// Uses raycast to detect obstacles
///// </summary>
//private bool UseSensor(Vector3 startPos, Vector3 direction, out RaycastHit hit, float sensorLength)
//{
//    hit = new RaycastHit();

//    if (Physics.Raycast(startPos, direction, out hit, sensorLength))
//    {
//        if (hit.transform.tag != "Terrain" && hit.transform != transform)
//        {
//            float distance = Vector3.Distance(rb.position, hit.point);
//            Color rayColor = Color.black;
//            if (distance < criticalCollisionDistance)
//            {
//                rayColor = Color.red;
//            }
//            else if (distance < warningCollisionDistance)
//            {
//                rayColor = Color.yellow;
//            }
//            else if (distance < infoCollisionDistance)
//            {
//                rayColor = Color.white;
//            }
//            Debug.DrawLine(startPos, hit.point, rayColor);
//            return true;
//        }
//    }
//    return false;
//}

#endregion
