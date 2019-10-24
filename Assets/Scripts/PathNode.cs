using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode : MonoBehaviour
{
    public static PathNode selectedNodeForConnection; //used with hotkeys for quick connection between nodes

    [SerializeField] bool allowedToPass = true;
    [SerializeField] float roadSpeedLimit = 30;

    public bool isPartOfIntersection = false; //Used to enable cars to check for other cars in the intersection
    public Intersection intersection;
    List<CarAI> carsOnThisNode = new List<CarAI>();

    [SerializeField] List<PathNode> possibleNextNodes = new List<PathNode>();

    public List<PathNode> backwardNodes = new List<PathNode>(); //used for catmull-rom (curved path)


    private void Start()
    {
        if (possibleNextNodes.Count < 1)
        {
            Debug.LogError("You have not set up the path correctly, this node is missing a nodeconnection " + transform.name);
        }
    }


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
        if (input != this && !possibleNextNodes.Contains(input))
        {
            possibleNextNodes.Add(input);
            input.AddBackwardsNodeConnection(this);
        }
    }
    public void AddConnectedNode(List<PathNode> input)
    {
        for (int i = 0; i < input.Count; i++)
        {
            AddConnectedNode(input[i]);
        }
    }

    /// <summary>
    /// Adds a new node to the list of previous or incoming nodes, used for catmull-rom calculation
    /// </summary>
    public void AddBackwardsNodeConnection(PathNode input)
    {
        if (input != this && !backwardNodes.Contains(input))
        {
            backwardNodes.Add(input);
        }
    }

    /// <summary>
    /// Returns all backward connections of this node
    /// </summary>
    public List<PathNode> GetBackWardConnections()
    {
        return backwardNodes;
    }

    /// <summary>
    /// Replaces the connected node, mainly used in editor too quickly create new nodes between already made nodes
    /// </summary>
    public void ReplaceConnectedNode(PathNode input)
    {
        if (possibleNextNodes.Count == 0)
        {
            possibleNextNodes.Add(input);
        }
        else
        {
            possibleNextNodes[0] = input;
        }
        input.AddBackwardsNodeConnection(this);
    }

    public void ClearForwardConnections()
    {
        possibleNextNodes.Clear();
    }



    public void AddCarToNode(CarAI car)
    {
        carsOnThisNode.Add(car);
    }

    public void RemoveCarFromNode(CarAI car)
    {
        carsOnThisNode.Remove(car);
    }

    public List<CarAI> GetCarsOnThisNode()
    {
        return carsOnThisNode;
    }




    [Header("Editor")]
    public Color lineColor;
    private Color nodeColor;
    public float nodeSize = 1f;
    public int visualPathSubsteps = 15;

    private void OnDrawGizmos()
    {
        bool catmullCurveAllowed = true;

        int visualizationSubsteps = visualPathSubsteps;
        nodeColor = allowedToPass ? Color.blue : Color.red;
        Gizmos.color = nodeColor;
        Gizmos.DrawWireSphere(this.transform.position, nodeSize);

        Gizmos.color = lineColor;


        //Safety check, delete inactive nodes
        for (int i = 0; i < backwardNodes.Count; i++)
        {
            if (backwardNodes[i] == null)
            {
                backwardNodes.Remove(backwardNodes[i]);
            }
        }

        for (int outNode = 0; outNode < possibleNextNodes.Count; outNode++)
        {
            if (possibleNextNodes[outNode] != null)
            {
                Vector3 currentNode = this.transform.position;
                Vector3 nextNode = Vector3.zero;
                nextNode = possibleNextNodes[outNode].transform.position;

                Vector3 direction = (nextNode - currentNode).normalized;
                Vector3 arrowPosition = currentNode + direction;
                DrawArrow.ForGizmo(arrowPosition, direction, lineColor, 0.4f, 30);
            }
            else
            {
                possibleNextNodes.Remove(possibleNextNodes[outNode]);
                outNode--;
            }

            //Safety check before catmull-rom to make sure connections can be checked both ways
            if (!possibleNextNodes[outNode].GetBackWardConnections().Contains(this))
            {
                possibleNextNodes[outNode].AddBackwardsNodeConnection(this);
            }

            //If we are missing any connections anywhere here we cannot make a proper catmull-curve
            if (backwardNodes.Count < 1 || possibleNextNodes.Count < 1 || possibleNextNodes[outNode].possibleNextNodes.Count < 1)
            {
                catmullCurveAllowed = false;
                Color temp = Gizmos.color;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(this.transform.position, possibleNextNodes[outNode].transform.position);
                Gizmos.color = temp;
            }

            if (catmullCurveAllowed)
            {
                //instead of showing many paths for all different kind of combinations of inputs and outputs, we take the average of position 1 and 4 of the catmull-rom if there are multiple
                Vector3 averageBackwardsNodePosition = Vector3.zero;
                for (int inNode = 0; inNode < backwardNodes.Count; inNode++)
                {
                    averageBackwardsNodePosition += backwardNodes[inNode].transform.position;
                }
                averageBackwardsNodePosition /= backwardNodes.Count;

                Vector3 averageOutNodeOutNodesPosition = Vector3.zero;
                for (int outNodeOutNode = 0; outNodeOutNode < possibleNextNodes[outNode].possibleNextNodes.Count; outNodeOutNode++)
                {
                    averageOutNodeOutNodesPosition += possibleNextNodes[outNode].possibleNextNodes[outNodeOutNode].transform.position;
                }
                averageOutNodeOutNodesPosition /= possibleNextNodes[outNode].possibleNextNodes.Count;

                //use catmull rom to draw a curved path
                for (int step = 0; step < visualizationSubsteps; step++)
                {
                    float progress = (float)step / (float)visualizationSubsteps;
                    Vector3 a = CatmullRom(averageBackwardsNodePosition, this.transform.position, possibleNextNodes[outNode].transform.position, averageOutNodeOutNodesPosition, progress);
                    progress = ((float)step + 1) / (float)visualizationSubsteps;
                    Vector3 b = CatmullRom(averageBackwardsNodePosition, this.transform.position, possibleNextNodes[outNode].transform.position, averageOutNodeOutNodesPosition, progress);
                    Gizmos.DrawLine(a, b);
                }
            }
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
}



public static class DrawArrow
{
    public static void ForGizmo(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Gizmos.DrawRay(pos, direction);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
        Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
    }

    public static void ForGizmo(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Gizmos.color = color;
        Gizmos.DrawRay(pos, direction);
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
        Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
    }

    public static void ForDebug(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Debug.DrawRay(pos, direction);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Debug.DrawRay(pos + direction, right * arrowHeadLength);
        Debug.DrawRay(pos + direction, left * arrowHeadLength);
    }
    public static void ForDebug(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Debug.DrawRay(pos, direction, color);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Debug.DrawRay(pos + direction, right * arrowHeadLength, color);
        Debug.DrawRay(pos + direction, left * arrowHeadLength, color);
    }
}