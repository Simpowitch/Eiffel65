using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode : MonoBehaviour
{
    public static PathNode selectedNodeForConnection; //used with hotkeys for quick connection between nodes

    [SerializeField] bool allowedToPass = true;
    [SerializeField] float roadSpeedLimit = 30;
    float intersectionSpeedLimitOverride = 30;

    public bool isPartOfIntersection = false; //Used to enable cars to check for other cars in the intersection
    //[SerializeField] List<PathNode> nodesToWaitFor = new List<PathNode>();
    public List<CarAI> carsOnThisNode = new List<CarAI>(); //debug public

    public List<DirectionChoice> outChoices = new List<DirectionChoice>();
    public List<PathNode> inNodes = new List<PathNode>(); //used for catmull-rom (curved path)


    private void Start()
    {
        if (outChoices.Count < 1)
        {
            Debug.LogError("You have not set up the path correctly, this node is missing a nodeconnection " + transform.name);
        }
        ValidateConnections();
        ValidateSetup();
    }

    /// <summary>
    /// Returns true when green light is on, or if there is no traffic light present
    /// </summary>
    public bool IsAllowedToPass(PathNode nextNodeToGoTo)
    {
        if (!allowedToPass)
        {
            return false;
        }
        else
        {
            if (isPartOfIntersection)
            {
                //Check which outnode of this node we are going to next
                for (int i = 0; i < outChoices.Count; i++)
                {
                    if (outChoices[i].outNode == nextNodeToGoTo)
                    {
                        //Look through that choice and the nodes to wait for to see if there are any cars
                        for (int j = 0; j < outChoices[i].nodesToWaitFor.Count; j++)
                        {
                            if (outChoices[i].nodesToWaitFor[i].carsOnThisNode.Count != 0)
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            else
            {
                return true;
            }
        }
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
    /// Returns the nodes and directions possible to go to next from this position
    /// </summary>
    public List<DirectionChoice> GetOutChoices()
    {
        return outChoices;
    }

    /// <summary>
    /// Adds a new node to the list of connected nodes and out direction choices
    /// </summary>
    public void AddOutChoice(DirectionChoice input)
    {
        if (input.outNode == this)
        {
            Debug.Log("Cannot add this pathnode to itself" + transform.name);
            return;
        }

        for (int i = 0; i < outChoices.Count; i++)
        {
            if (outChoices[i].outNode == input.outNode)
            {
                Debug.Log("Cannot add an already existing connected node to this pathnode" + transform.name);
                return;
            }
        }
        outChoices.Add(input);
        input.outNode.AddInConnection(this);
    }

    /// <summary>
    /// Adds a list of nodes to the list of connected nodes and out direction choices
    /// </summary>
    public void AddOutChoice(List<DirectionChoice> input)
    {
        for (int i = 0; i < input.Count; i++)
        {
            AddOutChoice(input[i]);
        }
    }

    /// <summary>
    /// Adds a new node to the list of previous or incoming nodes, used for catmull-rom calculation
    /// </summary>
    public void AddInConnection(PathNode input)
    {
        if (input != this && !inNodes.Contains(input))
        {
            inNodes.Add(input);
        }
    }

    /// <summary>
    /// Returns all backward connections of this node
    /// </summary>
    public List<PathNode> GetInConnections()
    {
        return inNodes;
    }

    /// <summary>
    /// Replaces the connected node, mainly used in editor too quickly create new nodes between already made nodes
    /// </summary>
    public void ReplaceSingleConnection(PathNode newNode)
    {
        outChoices.Clear();
        DirectionChoice newChoice = new DirectionChoice(newNode, Turn.Straight);
        AddOutChoice(newChoice);
    }

    //public void ClearForwardConnections()
    //{
    //    for (int i = 0; i < possibleNextNodes.Count; i++)
    //    {
    //        possibleNextNodes[i].inNodes.Remove(this);
    //    }
    //    possibleNextNodes.Clear();
    //}



    public void AddCarToNode(CarAI car)
    {
        if (carsOnThisNode.Contains(car))
        {
            Debug.LogWarning("Tried to att the an already existing car to this node" + transform.name);
        }
        else
        {
            carsOnThisNode.Add(car);
        }
    }

    public void RemoveCarFromNode(CarAI car)
    {
        if (carsOnThisNode.Contains(car))
        {
            carsOnThisNode.Remove(car);
        }
        else
        {
            Debug.LogWarning("Tried to remove a car from this node" + transform.name);
        }
    }

    public List<CarAI> GetCarsOnThisNode()
    {
        return carsOnThisNode;
    }




    [Header("Editor")]
    Color lineColor = Color.green;
    Color allowedToPassColor = Color.green;
    Color notAllowedToPassColor = Color.red;
    float nodeSize = 1f;
    int visualPathSubsteps = 15; //substeps for catmull-rom curve

    private void OnDrawGizmos()
    {
        ValidateConnections();
        ValidateSetup();
        
        //Draw sphere
        Gizmos.color = allowedToPass ? allowedToPassColor : notAllowedToPassColor;
        Gizmos.DrawWireSphere(this.transform.position, nodeSize);

        #region DrawLinesAndCheckConnectivity
        bool catmullCurveAllowed = true;
        int visualizationSubsteps = visualPathSubsteps;


        //Lines and curves
        Gizmos.color = lineColor;
        for (int i = 0; i < outChoices.Count; i++)
        {
            Vector3 currentNode = this.transform.position;
            Vector3 nextNode = Vector3.zero;
            nextNode = outChoices[i].outNode.transform.position;

            Vector3 direction = (nextNode - currentNode).normalized;
            Vector3 arrowPosition = currentNode + direction;
            DrawArrow.ForGizmo(arrowPosition, direction, lineColor, 0.4f, 30);

            //Safety check before catmull-rom to make sure connections can be checked both ways
            if (!outChoices[i].outNode.GetInConnections().Contains(this))
            {
                outChoices[i].outNode.AddInConnection(this);
            }

            //If we are missing any connections anywhere here we cannot make a proper catmull-curve
            if (inNodes.Count < 1 || inNodes.Count < 1 || outChoices[i].outNode.outChoices.Count < 1)
            {
                catmullCurveAllowed = false;
                Color temp = Gizmos.color;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(this.transform.position, outChoices[i].outNode.transform.position);
                Gizmos.color = temp;
            }

            if (catmullCurveAllowed)
            {
                //instead of showing many paths for all different kind of combinations of inputs and outputs, we take the average of position 1 and 4 of the catmull-rom if there are multiple
                Vector3 averageBackwardsNodePosition = Vector3.zero;
                for (int inNode = 0; inNode < inNodes.Count; inNode++)
                {
                    averageBackwardsNodePosition += inNodes[inNode].transform.position;
                }
                averageBackwardsNodePosition /= inNodes.Count;

                Vector3 averageOutNodeOutNodesPosition = Vector3.zero;
                for (int j = 0; j < outChoices[i].outNode.outChoices.Count; j++)
                {
                    averageOutNodeOutNodesPosition += outChoices[i].outNode.outChoices[j].outNode.transform.position;
                }
                averageOutNodeOutNodesPosition /= outChoices[i].outNode.outChoices.Count;

                //use catmull rom to draw a curved path
                for (int step = 0; step < visualizationSubsteps; step++)
                {
                    float progress = (float)step / (float)visualizationSubsteps;
                    Vector3 a = CatmullRom(averageBackwardsNodePosition, this.transform.position, outChoices[i].outNode.transform.position, averageOutNodeOutNodesPosition, progress);
                    progress = ((float)step + 1) / (float)visualizationSubsteps;
                    Vector3 b = CatmullRom(averageBackwardsNodePosition, this.transform.position, outChoices[i].outNode.transform.position, averageOutNodeOutNodesPosition, progress);
                    Gizmos.DrawLine(a, b);
                }
            }
        }
        #endregion

        //Show waiting nodes
        Gizmos.color = Color.red;

        for (int i = 0; i < outChoices.Count; i++)
        {
            //Look through that choice and the nodes to wait for to see if there are any cars
            for (int j = 0; j < outChoices[i].nodesToWaitFor.Count; j++)
            {
                if (outChoices[i].nodesToWaitFor[i].carsOnThisNode.Count != 0)
                {
                    Gizmos.DrawLine(this.transform.position, outChoices[i].nodesToWaitFor[j].transform.position);
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

    private void ValidateConnections()
    {
        //Safety check, delete inactive nodes
        for (int i = 0; i < inNodes.Count; i++)
        {
            if (inNodes[i] == null)
            {
                inNodes.Remove(inNodes[i]);
                Debug.Log("Removed null-node");
            }
        }
        for (int i = 0; i < outChoices.Count; i++)
        {
            if (outChoices[i].outNode == null)
            {
                outChoices.RemoveAt(i);
                Debug.Log("Removed null-node");
            }
        }

        //Add if this is missing in connected nodes backward nodes
        for (int i = 0; i < outChoices.Count; i++)
        {
            if (!outChoices[i].outNode.GetInConnections().Contains(this))
            {
                outChoices[i].outNode.AddInConnection(this);
                Debug.Log("Added missing node connection");
            }
        }

        //If this node has backward connections which no longer is connected to this node, remove the backward - connection
        for (int i = 0; i < inNodes.Count; i++)
        {
            List<DirectionChoice> choices = inNodes[i].GetOutChoices();
            bool remove = true;
            for (int j = 0; j < choices.Count; j++)
            {
                if (choices[j].outNode == this)
                {
                    remove = false;
                }
            }
            if (remove)
            {
                inNodes.RemoveAt(i);
                Debug.Log("Removed backward node (" + inNodes[i].transform.name + ") to a no longer connected node from: " + transform.name + transform.position);
            }
        }
    }

    private void ValidateSetup()
    {
        for (int i = 0; i < outChoices.Count; i++)
        {
            if (outChoices[i].nodesToWaitFor.Count > 0)
            {
                isPartOfIntersection = true;
                break;
            }
        }

        if (isPartOfIntersection)
        {
            roadSpeedLimit = intersectionSpeedLimitOverride;

            for (int i = 0; i < inNodes.Count; i++)
            {
                inNodes[i].roadSpeedLimit = intersectionSpeedLimitOverride;
            }
        }
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

public enum Turn { Straight, Left, Right }

[System.Serializable]
public class DirectionChoice
{
    public DirectionChoice(PathNode node, Turn direction)
    {
        outNode = node;
        turnDirection = direction;
    }

    public PathNode outNode;
    public Turn turnDirection;
    public List<PathNode> nodesToWaitFor = new List<PathNode>();
}
