using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PedestrianAI : MonoBehaviour
{
    NavMeshAgent agent;
    [SerializeField] Transform pedestrianNodesParent = null;
    Transform[] pedestrianNodes;
    public PedestrianTrafficLight nearbyTrafficLight; //DEBUG
    PedestrianTrafficLight lastVisitedTrafficLight;
    public bool isWaiting = false;

    public enum PedestrianState { OnSidewalk, OnRoad}
    public PedestrianState state = PedestrianState.OnSidewalk;

    public bool isOnRoad = false;

    private void Start()
    {
        pedestrianNodes = pedestrianNodesParent.GetComponentsInChildren<Transform>();
        agent = GetComponent<NavMeshAgent>();
        SetRandomTarget();
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, agent.destination) < agent.stoppingDistance)
        {
            SetRandomTarget();
        }


        if (nearbyTrafficLight != null && !nearbyTrafficLight.greenLight || CheckForCars())
        {
            isWaiting = true;
            agent.isStopped = true;
        }
        else
        {
            isWaiting = false;
            agent.isStopped = false;
        }
    }

    float maxVelocityOfCarToFeelSafe = 10f;
    float detectionRadius = 2f;
    private bool CheckForCars()
    {
        Collider[] collisions = Physics.OverlapSphere(this.transform.position + transform.forward * detectionRadius / 2, detectionRadius);

        foreach (var item in collisions)
        {
            if (item.gameObject.tag == "Vehicle")
            {
                if (item.transform.parent.GetComponent<Rigidbody>().velocity.magnitude * 3.6f > maxVelocityOfCarToFeelSafe)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void SetRandomTarget()
    {
        agent.SetDestination(pedestrianNodes[Random.Range(0, pedestrianNodes.Length)].position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PedestrianTrafficLight>() != null)
        {
            nearbyTrafficLight = other.GetComponent<PedestrianTrafficLight>();
        }
        
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PedestrianTrafficLight>() != null)
        {
            if (nearbyTrafficLight == other.GetComponent<PedestrianTrafficLight>())
            {
                nearbyTrafficLight = null;
                lastVisitedTrafficLight = other.GetComponent<PedestrianTrafficLight>();
                state = PedestrianState.OnRoad;
                isOnRoad = true;
            }
        }
        if (lastVisitedTrafficLight && other.tag == "PedestrianCrossing")
        {
            lastVisitedTrafficLight = null;
            StartCoroutine(GetOffRoadInSeconds());
        }
    }

    float seconds = 2;
    IEnumerator GetOffRoadInSeconds()
    {
        yield return new WaitForSeconds(seconds);
        state = PedestrianState.OnSidewalk;
        isOnRoad = false;
    }
}
