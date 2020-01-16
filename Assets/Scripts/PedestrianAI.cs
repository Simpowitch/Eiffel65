using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PedestrianAI : MonoBehaviour
{
    NavMeshAgent agent;
    [SerializeField] Transform pedestrianNodesParent = null;
    Transform[] pedestrianNodes;
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
    }

    private void SetRandomTarget()
    {
        agent.SetDestination(pedestrianNodes[Random.Range(0, pedestrianNodes.Length)].position);
    }
}
