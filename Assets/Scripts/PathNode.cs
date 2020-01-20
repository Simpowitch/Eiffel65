using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class PathNode : MonoBehaviour
{
    public static PathNode selectedNodeForConnection; //used with hotkeys for quick connection between nodes

    [SerializeField] bool greenLight = true;
    [SerializeField] float roadSpeedLimit = 30;
    float intersectionSpeedLimitOverride = 30;

    public bool isPartOfIntersection = false; //Used to enable cars to check for other cars in the intersection
    public List<CarAI> carsOnThisNode = new List<CarAI>(); //debug public

    public List<DirectionChoice> outChoices = new List<DirectionChoice>();
    public List<PathNode> inNodes = new List<PathNode>(); //used for catmull-rom (curved path)

    private void Start()
    {
        AnalyzeAndValidate();

        if (outChoices.Count < 1)
        {
            Debug.LogError("You have not set up the path correctly, this node is missing a nodeconnection " + transform.name);
        }
    }

    /// <summary>
    /// Returns true when green light is on, or if there is no traffic light present
    /// </summary>
    public bool IsCarAllowedToPass(PathNode nextNodeToGoTo)
    {
        if (!greenLight)
        {
            return false;
        }
        if (isPartOfIntersection)
        {
            //Check which outnode of this node we are going to next
            for (int i = 0; i < outChoices.Count; i++)
            {
                if (outChoices[i].nextNode == nextNodeToGoTo)
                {
                    //Look through that choice and the nodes to wait for to see if there are any cars
                    for (int j = 0; j < outChoices[i].nodesToWaitFor.Count; j++)
                    {
                        if (outChoices[i].nodesToWaitFor[j].carsOnThisNode.Count != 0 && outChoices[i].nodesToWaitFor[j].greenLight)
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

    /// <summary>
    /// Sets flags the node as allowed to pass or not.
    /// </summary>
    public void SetAllowedToPass(bool input)
    {
        greenLight = input;
    }

    /// <summary>
    /// Switches the bool of allowed to pass
    /// </summary>
    public void SwitchAllowedToPass()
    {
        greenLight = greenLight ? false : true;
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
        if (input.nextNode == this)
        {
            Debug.LogWarning("Cannot add this pathnode to itself" + transform.name);
            return;
        }

        for (int i = 0; i < outChoices.Count; i++)
        {
            if (outChoices[i].nextNode == input.nextNode)
            {
                Debug.LogWarning("Cannot add an already existing connected node to this pathnode" + transform.name);
                return;
            }
        }
        DirectionChoice newChoice = input;
        outChoices.Add(newChoice);
        newChoice.nextNode.AddInConnection(this);

        Debug.Log("Node added");
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
        DirectionChoice newChoice = new DirectionChoice(newNode, this, Turn.Straight);
        AddOutChoice(newChoice);
    }


    public void AddCarToNode(CarAI car)
    {
        if (carsOnThisNode.Contains(car))
        {
            //Do nothing
            //Debug.LogWarning("Tried to add an already existing car: " + car.transform.name + " to this node" + transform.name);
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
            StartCoroutine(RemoveCarAfterSeconds(2f, car));
            //carsOnThisNode.Remove(car);
        }
        else
        {
            //DO nothing
            //Debug.Log("Tried to remove " + car.transform.name + " from this node" + transform.name);
        }
    }

    IEnumerator RemoveCarAfterSeconds(float seconds, CarAI car)
    {
        CarAI tempCar = null;
        carsOnThisNode.Remove(car);
        carsOnThisNode.Add(tempCar);
        yield return new WaitForSeconds(seconds);
        carsOnThisNode.Remove(tempCar);
    }

    public List<CarAI> GetCarsOnThisNode()
    {
        return carsOnThisNode;
    }

    [Header("Editor")]
    Color allowedToPassColor = Color.green;
    Color notAllowedToPassColor = Color.red;
    static float nodeSize = 1.5f;
    static int visualPathSubsteps = 10; //substeps for catmull-rom curve
    static float maxDistanceToEditorCamera = 150f;

    private void OnDrawGizmos()
    {
        AnalyzeAndValidate();

        if (Vector3.Distance(this.transform.position, SceneView.lastActiveSceneView.camera.transform.position) > maxDistanceToEditorCamera)
        {
            return;
        }

        #region DrawLinesAndCheckConnectivity
        //Draw sphere
        Gizmos.color = greenLight ? allowedToPassColor : notAllowedToPassColor;
        Gizmos.DrawWireSphere(this.transform.position, nodeSize);

        bool catmullCurveAllowed = true;
        int visualizationSubsteps = visualPathSubsteps;


        //Lines and curves
        for (int i = 0; i < outChoices.Count; i++)
        {
            Gizmos.color = greenLight ? allowedToPassColor : notAllowedToPassColor; ;

            //Safety check before catmull-rom to make sure connections can be checked both ways
            if (!outChoices[i].nextNode.GetInConnections().Contains(this))
            {
                outChoices[i].nextNode.AddInConnection(this);
            }

            //If we are missing any connections anywhere here we cannot make a proper catmull-curve
            if (inNodes.Count < 1 || inNodes.Count < 1 || outChoices[i].nextNode.outChoices.Count < 1)
            {
                catmullCurveAllowed = false;
                Color temp = Gizmos.color;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(this.transform.position, outChoices[i].nextNode.transform.position);
                Gizmos.color = temp;
            }

            //Change color of gizmo if we cannot use this outchoice due to need to wait, or if not allowed to pass
            for (int j = 0; j < outChoices[i].nodesToWaitFor.Count; j++)
            {
                if (outChoices[i].nodesToWaitFor[j].carsOnThisNode.Count != 0 && outChoices[i].nodesToWaitFor[j].greenLight)
                {
                    Gizmos.color = Color.red;
                    break;
                }
            }

            //Draw arrow
            Vector3 currentNode = this.transform.position;
            Vector3 nextNode = Vector3.zero;
            nextNode = outChoices[i].nextNode.transform.position;

            //Check if double node created (if at same space)
            if (nextNode == currentNode)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(this.transform.position, 10f);
            }

            Vector3 direction = (nextNode - currentNode).normalized;
            Turn turnDirection = outChoices[i].turnDirection;
            Color arrowColor = Color.green;
            switch (turnDirection)
            {
                case Turn.Straight:
                    break;
                case Turn.Left:
                    arrowColor = Color.blue;
                    break;
                case Turn.Right:
                    arrowColor = Color.red;
                    break;
            }
            Vector3 arrowPosition = currentNode + direction;
            DrawArrow.ForGizmo(arrowPosition, direction, arrowColor, 0.4f, 30);

            //Reset color
            Gizmos.color = Color.green;

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
                for (int j = 0; j < outChoices[i].nextNode.outChoices.Count; j++)
                {
                    averageOutNodeOutNodesPosition += outChoices[i].nextNode.outChoices[j].nextNode.transform.position;
                }
                averageOutNodeOutNodesPosition /= outChoices[i].nextNode.outChoices.Count;

                //use catmull rom to draw a curved path
                for (int step = 0; step < visualizationSubsteps; step++)
                {
                    float progress = (float)step / (float)visualizationSubsteps;
                    Vector3 a = CatmullRom(averageBackwardsNodePosition, this.transform.position, outChoices[i].nextNode.transform.position, averageOutNodeOutNodesPosition, progress);
                    progress = ((float)step + 1) / (float)visualizationSubsteps;
                    Vector3 b = CatmullRom(averageBackwardsNodePosition, this.transform.position, outChoices[i].nextNode.transform.position, averageOutNodeOutNodesPosition, progress);
                    Gizmos.DrawLine(a, b);
                }
            }
        }
        #endregion

        //Show waiting nodes
        //Gizmos.color = Color.red;

        //for (int i = 0; i < outChoices.Count; i++)
        //{
        //    //Look through that choice and the nodes to wait for to see if there are any cars
        //    for (int j = 0; j < outChoices[i].nodesToWaitFor.Count; j++)
        //    {
        //        if (outChoices[i].nodesToWaitFor[j].carsOnThisNode.Count != 0)
        //        {
        //            Gizmos.DrawLine(this.transform.position, outChoices[i].nodesToWaitFor[j].transform.position);
        //        }
        //    }
        //}
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float i)
    {
        // comments are no use here... it's the catmull-rom equation.
        // Un-magic this, lord vector!
        return 0.5f *
               ((2 * p1) + (-p0 + p2) * i + (2 * p0 - 5 * p1 + 4 * p2 - p3) * i * i +
                (-p0 + 3 * p1 - 3 * p2 + p3) * i * i * i);
    }


    //Validations
    public void AnalyzeAndValidate()
    {
        ValidateConnections();
        ValidateSetup();
        SetPositionToMatchTerrain();
    }

    private void ValidateConnections()
    {
        //Add if this is missing in connected nodes backward nodes
        for (int i = 0; i < outChoices.Count; i++)
        {
            if (!outChoices[i].nextNode.GetInConnections().Contains(this))
            {
                outChoices[i].nextNode.AddInConnection(this);
            }
        }

        //Safety check, delete inactive nodes
        for (int i = 0; i < inNodes.Count; i++)
        {
            if (inNodes[i] == null)
            {
                inNodes.Remove(inNodes[i]);
            }
        }
        for (int i = 0; i < outChoices.Count; i++)
        {
            if (outChoices[i].nextNode == null)
            {
                outChoices.RemoveAt(i);
            }
        }


        //If this node has backward connections which no longer is connected to this node, remove the backward - connection
        for (int i = 0; i < inNodes.Count; i++)
        {
            List<DirectionChoice> choices = inNodes[i].GetOutChoices();
            bool remove = true;
            for (int j = 0; j < choices.Count; j++)
            {
                if (choices[j].nextNode == this)
                {
                    remove = false;
                }
            }
            if (remove)
            {
                inNodes.RemoveAt(i);
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

    private void SetPositionToMatchTerrain()
    {
        if (Physics.Raycast(this.transform.position, -this.transform.up, out RaycastHit hit))
        {
            if (hit.transform.tag == "Terrain")
            {
                this.transform.position = hit.point + new Vector3(0, nodeSize / 2, 0);
            }
        }
        else if (Physics.Raycast(this.transform.position + this.transform.up * 100, -this.transform.up, out hit))
        {
            if (hit.transform.tag == "Terrain")
            {
                this.transform.position = hit.point + new Vector3(0, nodeSize / 2, 0);
            }
        }
    }
}



public static class DrawArrow
{
    public static void ForGizmo(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        if (direction == Vector3.zero)
        {
            return;
        }

        Gizmos.DrawRay(pos, direction);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
        Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
    }

    public static void ForGizmo(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        if (direction == Vector3.zero)
        {
            return;
        }

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
    public DirectionChoice(PathNode outNode, PathNode inNode, Turn direction)
    {
        nextNode = outNode;
        turnDirection = direction;
        originalNode = inNode;
    }


    public PathNode nextNode;
    public PathNode originalNode;
    public Turn turnDirection = Turn.Straight;
    public List<PathNode> nodesToWaitFor = new List<PathNode>();
}
