using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public enum NodeType { Default, Intersection }
public class PathNode : MonoBehaviour
{
    //[SerializeField] NodeType typeOfNode;
    //[SerializeField] bool hasTrafficLights;
    [SerializeField] bool allowedToPass = true;
    [SerializeField] float roadSpeedLimit = 30;

    [SerializeField] PathNode[] possibleNextNodes;


    /// <summary>
    /// Return this node's type of path
    /// </summary>
    //public NodeType GetNodeType()
    //{
    //    return typeOfNode;
    //}

    /// <summary>
    /// Returns true if this node has traffic lights 
    /// </summary>
    //public bool HasTrafficLights()
    //{
    //    return hasTrafficLights;
    //}

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
    /// Returns the nodes possible to go to next from this position
    /// </summary>
    public PathNode[] GetPathNodes()
    {
        return possibleNextNodes;
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
        for (int i = 0; i < possibleNextNodes.Length; i++)
        {
            Vector3 currentNode = this.transform.position;
            Vector3 nextNode = Vector3.zero;
            nextNode = possibleNextNodes[i].transform.position;
            Gizmos.DrawLine(currentNode, nextNode);
        }
    }
}
