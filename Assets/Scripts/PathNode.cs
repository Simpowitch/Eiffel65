using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode : MonoBehaviour
{
    public static PathNode dragConnectedNode;

    [SerializeField] bool allowedToPass = true;
    [SerializeField] float roadSpeedLimit = 30;
    [SerializeField] int pathFindingCost = 0;

    [SerializeField] List<PathNode> possibleNextNodes;


    /// <summary>
    /// Returns true when green light is on, or if there is no traffic light present
    /// </summary>
    public bool IsAllowedToPass()
    {
        //if (!hasTrafficLights)
        //{
        //    return true;
        //}
        return allowedToPass;
    }

    /// <summary>
    /// Sets flags the node as allowed to pass or not.
    /// </summary>
    public void SetAllowedToPass(bool input)
    {
        allowedToPass = input;
    }

    /// <summary>
    /// Switches the bool of allowed to pass
    /// </summary>
    public void SwitchAllowedToPass()
    {
        allowedToPass = allowedToPass ? false : true;
    }

    /// <summary>
    /// Returns the speed limit at this node
    /// </summary>
    public float GetRoadSpeedLimit()
    {
        return roadSpeedLimit;
    }

    /// <summary>
    /// Sets the speed limit at this node
    /// </summary>
    public void SetRoadSpeedLimit(float input)
    {
        roadSpeedLimit = input;
    }

    /// <summary>
    /// Returns the nodes possible to go to next from this position
    /// </summary>
    public List<PathNode> GetPathNodes()
    {
        return possibleNextNodes;
    }

    /// <summary>
    /// Adds a new node to the list of connected nodes, mainly used in editor too quickly create new nodes
    /// </summary>
    public void AddConnectedNode(PathNode input)
    {
        if (input != this)
        {
            possibleNextNodes.Add(input);
        }
    }
    public void AddConnectedNode(List<PathNode> input)
    {
        for (int i = 0; i < input.Count; i++)
        {
            possibleNextNodes.Add(input[i]);
        }
    }

    /// <summary>
    /// Replaces the connected node, mainly used in editor too quickly create new nodes between already made nodes
    /// </summary>
    public void ReplaceConnectedNode(PathNode input)
    {
        possibleNextNodes[0] = input;
    }

    public void AddPathFindingCost()
    {
        pathFindingCost++;
    }

    public void ReducePathFindingCost()
    {
        pathFindingCost--;
    }

    public int GetPathFindingCost()
    {
        return pathFindingCost;
    }



    [Header("Editor")]
    public Color lineColor;
    private Color nodeColor;
    public float nodeSize = 1f;
    private void OnDrawGizmos()
    {
        Transform[] pathTransforms = GetComponentsInChildren<Transform>();

        nodeColor = allowedToPass ? Color.blue : Color.red;
        Gizmos.color = nodeColor;
        Gizmos.DrawWireSphere(this.transform.position, nodeSize);

        Gizmos.color = lineColor;
        for (int i = 0; i < possibleNextNodes.Count; i++)
        {
            if (possibleNextNodes[i] != null)
            {
                Vector3 currentNode = this.transform.position;
                Vector3 nextNode = Vector3.zero;
                nextNode = possibleNextNodes[i].transform.position;
                Gizmos.DrawLine(currentNode, nextNode);
            }
            else
            {
                possibleNextNodes.Remove(possibleNextNodes[i]);
                i--;
            }
        }
    }
}