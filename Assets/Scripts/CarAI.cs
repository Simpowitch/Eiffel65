﻿using System.Collections;
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
    [SerializeField] float kmhSpeed; //DEBUG
    //Our speed limit
    [SerializeField] float speedToHold = 50f;
    //The road speed limit
    [SerializeField] float roadSpeedLimit = 50f;
    //Tell the controller to brake or not
    [SerializeField] bool braking = false;

    private enum AIState { Drive, Queue, AvoidCollision, Stopping, BackingFromStuck }
    [SerializeField] AIState currentState;

    [SerializeField] bool isCriminal = false;

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
        float steerPercentage = 0;
        float torquePercentage = 0;

        steerPercentage = SteerTowardsNextNode();

        if (!CheckIfAllowedToPass() && !isCriminal)
        {
            currentState = AIState.Queue;
        }
        else
        {
            currentState = AIState.Drive;
        }

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
        }



        switch (currentState)
        {
            case AIState.Drive:
                {
                    float newSpeed = isCriminal ? Mathf.Max(30, roadSpeedLimit * 2 * (1 - Mathf.Abs(steerPercentage))) : Mathf.Max(10, roadSpeedLimit * (1 - Mathf.Abs(steerPercentage)));
                    SetSpeedToHold(newSpeed);
                }
                break;
            case AIState.Queue:
                {
                    float distanceToStop = Vector3.Distance(path[0].transform.position, transform.position);
                    float newSpeed = distanceToStop < 5 ? 0 : Mathf.Min(roadSpeedLimit, distanceToStop);
                    SetSpeedToHold(newSpeed);
                }
                break;
            case AIState.AvoidCollision:
                {
                    AvertFromCollision(ref steerPercentage);
                    float newSpeed = isCriminal ? Mathf.Max(30, roadSpeedLimit * 1.5f * (1 - Mathf.Abs(steerPercentage))) : Mathf.Min(10, roadSpeedLimit * (1 - Mathf.Abs(steerPercentage)));
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

        if (currentState != AIState.Queue || CheckIfAllowedToPass())
        {
            UpdateWaypoint();
        }

        wheelController.AIDriver(steerPercentage, torquePercentage, braking);
    }

    #region PathCreation
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
            if (Vector3.Distance(posToTest, path[0].transform.position) < distanceToAcceptNodeArrival)
            {
                return 0;
            }
            else
            {
                Vector3 relativeVector = transform.InverseTransformPoint(path[0].transform.position);
                relativeVector /= relativeVector.magnitude;
                return (relativeVector.x / relativeVector.magnitude);
            }
        }
        else
        {
            Vector3 relativeVector = transform.InverseTransformPoint(path[0].transform.position);
            relativeVector /= relativeVector.magnitude;
            return (relativeVector.x / relativeVector.magnitude);
        }
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
        if (Vector3.Distance(transform.position, path[0].transform.position) < distanceToAcceptNodeArrival)
        {
            if (path.Count > 1)
            {
                path.RemoveAt(0);
                SetRoadSpeedLimit(path[0].GetComponent<PathNode>().GetRoadSpeedLimit());
                currentNode = path[0];
            }
            else
            {
                SetRandomTargetNode(); //add new path to go to
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
    private bool CheckForCollision(float turningPercentage, out RaycastHit collision)
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
        sensorStartPos += transform.right * frontSideSensorXOffsetPos;
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
        sensorStartPos -= (transform.right * frontSideSensorXOffsetPos) * 2;
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
        //Front angled right sensor
        sensorStartPos += (transform.right * frontSideSensorXOffsetPos) * 2;
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
        collision = hit;
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
        sensorStartPos += transform.right * frontSideSensorXOffsetPos;
        collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (collisionDetected)
        {
            avoidMultiplier -= 1f;
            avoidingCollision = true;
            distanceToClosestCollision = Mathf.Min(distanceToClosestCollision, Vector3.Distance(hit.point, transform.position));
        }
        //Front angled right sensor
        collisionDetected = UseSensor(sensorStartPos, Quaternion.AngleAxis(frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength * Mathf.Abs(steerPercentage));
        if (collisionDetected)
        {
            avoidMultiplier -= 0.1f;
            avoidingCollision = true;
            distanceToClosestCollision = Mathf.Min(distanceToClosestCollision, Vector3.Distance(hit.point, transform.position));
        }

        //Front left sensor
        sensorStartPos -= (transform.right * frontSideSensorXOffsetPos) * 2;
        collisionDetected = UseSensor(sensorStartPos, transform.forward, out hit, frontSensorLength);
        if (collisionDetected)
        {
            avoidMultiplier += 1f;
            avoidingCollision = true;
            distanceToClosestCollision = Mathf.Min(distanceToClosestCollision, Vector3.Distance(hit.point, transform.position));
        }

        //Front angled left sensor
        collisionDetected = UseSensor(sensorStartPos, Quaternion.AngleAxis(-frontSensorsAngle, transform.up) * transform.forward, out hit, frontAngledSensorLength * Mathf.Abs(steerPercentage));
        if (collisionDetected)
        {
            avoidMultiplier += 0.1f;
            avoidingCollision = true;
            distanceToClosestCollision = Mathf.Min(distanceToClosestCollision, Vector3.Distance(hit.point, transform.position));
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
            steerPercentage = avoidMultiplier / (distanceToClosestCollision / 2);
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
        if (path.Count > 0)
        {
            Gizmos.color = lineToNode;
            Vector3 currentPos = this.transform.position;
            Vector3 nextNodePos = path[0].transform.position;
            Gizmos.DrawLine(currentPos, nextNodePos);
        }
    }
}
