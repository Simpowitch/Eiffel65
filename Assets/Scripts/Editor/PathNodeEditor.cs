using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PathNode))]
[CanEditMultipleObjects]
public class PathNodeEditor : Editor
{
    string pathName = "PathNode";
    float pathSpeed = 0;
    bool changeSpeed = false;
    int metersBetweenNodes = 15;
    string roadName = "RoadName";
    bool createExtraLanesToTheRight = false;
    int metersBetweenLanes = 4;
    int lanesToCreate = 1;
    bool forbidLaneChangeInEnd = false;
    int laneChangeDisallowanceNodes = 1;
    Transform roadSegmentParent;

    public override void OnInspectorGUI()
    {
        PathNode myPathNode = (PathNode)target;

        DrawDefaultInspector();

        GUILayout.Space(10);
        GUILayout.Label("\nNew node options");

        if (roadName == "RoadName" && myPathNode.transform.parent)
        {
            roadName = myPathNode.transform.parent.tag == "RoadSegment" ? myPathNode.transform.parent.name : "RoadName";
        }
        pathName = EditorGUILayout.TextField("PathName", pathName);
        roadName = EditorGUILayout.TextField("RoadName", roadName);

        if (roadName != "RoadName")
        {
            if (GameObject.Find(roadName))
            {
                roadSegmentParent = GameObject.Find(roadName).transform;
            }
            else
            {
                roadSegmentParent = null;
            }
        }
        else
        {
            roadSegmentParent = null;
        }

        changeSpeed = EditorGUILayout.Toggle("New node has new speed", changeSpeed);
        if (changeSpeed)
        {
            pathSpeed = EditorGUILayout.FloatField("New path speed", pathSpeed);
        }

        Event ev = Event.current;
        if (GUILayout.Button("Create new connected node (Shift + C)") || (ev.type == EventType.KeyDown && ev.shift && ev.keyCode == KeyCode.C))
        {
            Selection.activeGameObject = CreateNewSingleConnection(myPathNode);
        }

        if (GUILayout.Button("Replace connected node with new node (Shift + R)") || (ev.type == EventType.KeyDown && ev.shift && ev.keyCode == KeyCode.R))
        {
            Selection.activeGameObject = ReplaceNodeSingle(myPathNode);
        }

        metersBetweenNodes = EditorGUILayout.IntField("Meters beween nodes (multiple node creation options)", metersBetweenNodes);

        if (GUILayout.Button("Replace connected node with new multiple nodes (Shift + M)") || (ev.type == EventType.KeyDown && ev.shift && ev.keyCode == KeyCode.M))
        {
            Selection.activeGameObject = ReplaceNodeMultiple(myPathNode);
        }

        createExtraLanesToTheRight = EditorGUILayout.Toggle((createExtraLanesToTheRight ? "Create Lanes to the right" : "Create Lanes to the left"), createExtraLanesToTheRight);
        metersBetweenLanes = EditorGUILayout.IntField("Meters beween lanes", metersBetweenLanes);
        lanesToCreate = EditorGUILayout.IntField("Lanes to create", lanesToCreate);
        forbidLaneChangeInEnd = EditorGUILayout.Toggle("Forbid lanechange in end", forbidLaneChangeInEnd);

        if (GUILayout.Button("Create Road Towards Connected Node"))
        {
            Selection.activeGameObject = CreateRoadSection(myPathNode);
        }

        EditorGUILayout.HelpBox("Connect two nodes by pressing C and then select the node to add to the first selected node and press C again", MessageType.Info);
        if (PathNode.selectedNodeForConnection != null)
        {
            EditorGUILayout.HelpBox(PathNode.selectedNodeForConnection.name + " is selected as node to receive new connection", MessageType.Info);
            if (GUILayout.Button("Reset selected node to receive connection"))
            {
                PathNode.selectedNodeForConnection = null;
            }
        }
    }

    private PathNode CreateNewNode(PathNode selectedPathNode)
    {
        PathNode newNode;
        //newNode = Instantiate(Resources.Load<PathNode>("Prefabs/Road/Pathnode"), selectedPathNode.transform.position, selectedPathNode.transform.rotation);
        GameObject newObject = PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/Road/Pathnode")) as GameObject;
        newObject.transform.position = selectedPathNode.transform.position;
        newNode = newObject.GetComponent<PathNode>();

        if (roadSegmentParent)
        {
            newNode.transform.SetParent(roadSegmentParent);
        }
        else
        {
            CreateRoadSegmentParent(selectedPathNode.transform.position, selectedPathNode);
        }
        newNode.gameObject.name = roadName + " - " + pathName;
        newNode.SetRoadSpeedLimit(changeSpeed ? pathSpeed : selectedPathNode.GetRoadSpeedLimit());
        return newNode;
    }

    private GameObject CreateNewSingleConnection(PathNode myPathNode)
    {
        //PathNode newNode;
        //newNode = Instantiate(Resources.Load<PathNode>("Prefabs/Road/Pathnode"), myPathNode.transform.position, myPathNode.transform.rotation);
        PathNode newNode = CreateNewNode(myPathNode);
        DirectionChoice newChoice = new DirectionChoice(newNode, myPathNode, Turn.Straight);
        myPathNode.AddOutChoice(newChoice);
        //newNode.transform.SetParent(myPathNode.transform.parent);
        //newNode.gameObject.name = roadName + " - " + pathName;
        //newNode.SetRoadSpeedLimit(changeSpeed ? pathSpeed : myPathNode.GetRoadSpeedLimit());
        return newNode.gameObject;
    }

    private GameObject ReplaceNodeSingle(PathNode myPathNode)
    {
        if (myPathNode.GetOutChoices().Count != 1)
        {
            Debug.LogWarning("Not allowed to replace nodes if the number of connections isn't 1");
            return null;
        }

        PathNode newNode = CreateNewNode(myPathNode);
        newNode.transform.position = (myPathNode.transform.position + myPathNode.GetOutChoices()[0].nextNode.transform.position) / 2;
        newNode.AddOutChoice(myPathNode.GetOutChoices());
        myPathNode.ReplaceSingleConnection(newNode);
        return newNode.gameObject;


        //PathNode newNode;
        //newNode = Instantiate(Resources.Load<PathNode>("Prefabs/Road/Pathnode"), myPathNode.transform.position, myPathNode.transform.rotation);
        //newNode.transform.position = (myPathNode.transform.position + myPathNode.GetOutChoices()[0].outNode.transform.position) / 2;
        //newNode.AddOutChoice(myPathNode.GetOutChoices());
        //newNode.SetRoadSpeedLimit(changeSpeed ? pathSpeed : myPathNode.GetRoadSpeedLimit());
        //myPathNode.ReplaceSingleConnection(newNode);
        //newNode.transform.SetParent(myPathNode.transform.parent);
        //newNode.gameObject.name = roadName + " - " + pathName;
        //return newNode.gameObject;
    }

    private Transform CreateRoadSegmentParent(Vector3 position, PathNode selectedPathNode)
    {
        GameObject empty = new GameObject();
        roadSegmentParent = Instantiate(empty).transform;
        DestroyImmediate(empty);
        roadSegmentParent.gameObject.name = roadName;
        roadSegmentParent.SetParent(selectedPathNode.gameObject.transform.parent);
        roadSegmentParent.transform.position = position;
        roadSegmentParent.tag = "RoadSegment";
        return roadSegmentParent;
    }

    private GameObject ReplaceNodeMultiple(PathNode myPathNode)
    {
        if (myPathNode.GetOutChoices().Count != 1)
        {
            Debug.LogWarning("Not allowed to replace nodes if the number of connections isn't 1");
            return null;
        }

        PathNode newNode = myPathNode;
        PathNode endNode = myPathNode.GetOutChoices()[0].nextNode;
        Vector3 endPos = endNode.transform.position;
        int nodesToCreate = Mathf.FloorToInt(Vector3.Distance(myPathNode.transform.position, endPos) / metersBetweenNodes);
        Vector3 direction = endPos - myPathNode.transform.position;
        direction = direction.normalized;
        Vector3 position = myPathNode.transform.position;
        PathNode previousNode = myPathNode;

        //Save the old connections
        List<PathNode> originalConnections = new List<PathNode>();
        List<DirectionChoice> originalChoices = myPathNode.GetOutChoices();
        for (int i = 0; i < originalChoices.Count; i++)
        {
            originalConnections.Add(originalChoices[i].nextNode);
        }

        //Remove the selected node from in-nodes on the old connected nodes
        for (int i = 0; i < originalConnections.Count; i++)
        {
            originalConnections[i].inNodes.Remove(myPathNode);
        }

        List<PathNode> createdNodes = new List<PathNode>();
        for (int i = 0; i < nodesToCreate; i++)
        {
            position += direction * metersBetweenNodes;
            if (Vector3.Distance(position, endPos) > metersBetweenNodes / 2)
            {
                newNode = CreateNewNode(myPathNode);
                //newNode = Instantiate(Resources.Load<PathNode>("Prefabs/Road/Pathnode"), myPathNode.transform.position, myPathNode.transform.rotation);
                newNode.transform.position = position;
                //newNode.SetRoadSpeedLimit(changeSpeed ? pathSpeed : myPathNode.GetRoadSpeedLimit());
                //newNode.transform.SetParent(myPathNode.transform.parent);
                //newNode.gameObject.name = roadName + " - " + pathName + " " + i;
                createdNodes.Add(newNode);
            }
        }

        //Replace original pathnode connections with the first created node
        myPathNode.ReplaceSingleConnection(createdNodes[0]);

        //Add connectivity to each newly created node
        for (int i = 0; i < createdNodes.Count - 1; i++)
        {
            DirectionChoice newDirection = new DirectionChoice(createdNodes[i + 1], createdNodes[i], Turn.Straight);
            createdNodes[i].AddOutChoice(newDirection);
        }
        //add the original nodes to the last created node
        newNode = createdNodes[createdNodes.Count - 1];

        for (int i = 0; i < originalConnections.Count; i++)
        {
            DirectionChoice choice = new DirectionChoice(originalConnections[i], newNode, Turn.Straight);
            newNode.AddOutChoice(choice);
        }
        return newNode.gameObject;
    }

    private GameObject CreateRoadSection(PathNode myPathNode)
    {
        if (myPathNode.GetOutChoices().Count != 1)
        {
            Debug.LogWarning("Not allowed to replace nodes if the number of connections isn't 1");
            return null;
        }

        PathNode newNode = myPathNode;
        if (!roadSegmentParent)
        {
            CreateRoadSegmentParent((myPathNode.transform.position + myPathNode.GetOutChoices()[0].nextNode.transform.position) / 2, myPathNode);
        }

        //Rotate the node - helps us create the nodes in line towards the endnode
        myPathNode.transform.LookAt(myPathNode.GetOutChoices()[0].nextNode.transform);

        List<PathNode> originalInNodes = new List<PathNode>();
        //Save the inconnections from the startnode
        for (int i = 0; i < myPathNode.inNodes.Count; i++)
        {
            originalInNodes.Add(myPathNode.inNodes[i]);
        }


        PathNode endNode = myPathNode.GetOutChoices()[0].nextNode;
        Vector3 startPos = myPathNode.transform.position;
        Vector3 endPos = endNode.transform.position;
        int nodesToCreate = Mathf.FloorToInt(Vector3.Distance(myPathNode.transform.position, endPos) / metersBetweenNodes) + 1; //the start and finish nodes will be removed
        Vector3 direction = endPos - myPathNode.transform.position;
        direction = direction.normalized;
        Vector3 position = startPos;

        List<PathNode>[] lanes = new List<PathNode>[lanesToCreate];
        for (int i = 0; i < lanes.Length; i++)
        {
            lanes[i] = new List<PathNode>();
        }

        //How much to the left or right should the lanes differ in position
        Vector3 offset = myPathNode.transform.right * (createExtraLanesToTheRight ? (float)metersBetweenLanes : (float)-metersBetweenLanes);

        //Create the lanes
        for (int lane = 0; lane < lanes.Length; lane++)
        {
            PathNode previousNode = null;
            position = startPos + offset * lane;

            //Create the nodes for the current lane
            for (int node = 0; node < nodesToCreate; node++)
            {
                //last node
                if (node == nodesToCreate - 1)
                {
                    position = endPos + offset * lane;
                }

                newNode = CreateNewNode(myPathNode);
                newNode.transform.position = position;
                //newNode = Instantiate(Resources.Load<PathNode>("Prefabs/Road/Pathnode"), position, myPathNode.transform.rotation);

                //newNode.SetRoadSpeedLimit(changeSpeed ? pathSpeed : myPathNode.GetRoadSpeedLimit());

                if (previousNode != null)
                {
                    DirectionChoice choice = new DirectionChoice(newNode, previousNode, Turn.Straight);
                    previousNode.AddOutChoice(choice);
                }

                string name = roadName + " - " + pathName;
                if (node == 0)
                {
                    name += " start";
                }
                else if (node == nodesToCreate - 1)
                {
                    name += " end";
                }
                else
                {
                    name += node;
                }
                newNode.gameObject.name = name + ". Lane " + lane;

                newNode.transform.SetParent(roadSegmentParent);

                previousNode = newNode;
                position += direction * metersBetweenNodes;

                lanes[lane].Add(newNode);
            }
        }

        //Cross connections for lane change
        if (lanesToCreate > 1)
        {
            for (int lane = 0; lane < lanes.Length; lane++)
            {
                for (int node = 0; node < lanes[lane].Count; node++)
                {
                    //If we want to avoid cross connections close to the intersection for instance
                    if (forbidLaneChangeInEnd && node >= (lanes[lane].Count - laneChangeDisallowanceNodes - 1))
                    {
                        continue;
                    }

                    //If there is a lane to one side of our (inside the array but not this) and there is a node forward on that lane to connect to
                    if (lane + 1 < lanes.Length && node + 1 < lanes[lane + 1].Count)
                    {
                        DirectionChoice choice = new DirectionChoice(lanes[lane + 1][node + 1], lanes[lane][node], Turn.Straight);
                        lanes[lane][node].AddOutChoice(choice);
                    }
                    //If there is a lane to one side of our (inside the array but not this) and there is a node forward on that lane to connect to
                    if (lane - 1 >= 0 && node + 1 < lanes[lane - 1].Count)
                    {
                        DirectionChoice choice = new DirectionChoice(lanes[lane - 1][node + 1], lanes[lane][node], Turn.Straight);
                        lanes[lane][node].AddOutChoice(choice);
                    }
                }
            }
        }

        //Add the original in-nodes connection towards the newly created first node on the new road
        for (int i = 0; i < originalInNodes.Count; i++)
        {
            DirectionChoice newchoice = new DirectionChoice(lanes[0][0], originalInNodes[i], Turn.Straight);
            originalInNodes[i].AddOutChoice(newchoice);
        }

        GameObject.DestroyImmediate(myPathNode.GetOutChoices()[0].nextNode.gameObject);
        GameObject.DestroyImmediate(myPathNode.gameObject);
        return newNode.gameObject;
    }


    private void OnSceneGUI()
    {
        Event ev = Event.current;

        //Connect with shortcut command
        if (Event.current.type == EventType.KeyDown && !Event.current.shift && Event.current.keyCode == KeyCode.C)
        {
            if (PathNode.selectedNodeForConnection == null)
            {
                PathNode.selectedNodeForConnection = (PathNode)target;
                Debug.Log("Node to connect set");
            }
            else
            {
                DirectionChoice choice = new DirectionChoice((PathNode)target, PathNode.selectedNodeForConnection, Turn.Straight);
                PathNode.selectedNodeForConnection.AddOutChoice(choice);
                PathNode.selectedNodeForConnection = null;
                Debug.Log("Node connected");
            }
        }

        if (Selection.transforms.Length > 0 && Selection.transforms[0].GetComponent<PathNode>() != null)
        {
            PathNode myPathNode = Selection.transforms[0].GetComponent<PathNode>();
            myPathNode = (PathNode)target;

            if (ev.type == EventType.KeyDown && ev.shift && ev.keyCode == KeyCode.C)
            {
                Selection.activeGameObject = CreateNewSingleConnection(myPathNode);
            }

            if (ev.type == EventType.KeyDown && ev.shift && ev.keyCode == KeyCode.R)
            {
                Selection.activeGameObject = ReplaceNodeSingle(myPathNode);
            }

            if (ev.type == EventType.KeyDown && ev.shift && ev.keyCode == KeyCode.M)
            {
                Selection.activeGameObject = ReplaceNodeMultiple(myPathNode);
            }
        }
    }



    [MenuItem("PathNode/Create new pathnode #n")]
    static void CreateNewNode()
    {
        //PathNode newNode = Instantiate(Resources.Load<PathNode>("Prefabs/Road/Pathnode"));

        GameObject newObject = PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/Road/Pathnode")) as GameObject;
        PathNode newNode = newObject.GetComponent<PathNode>();

        Camera cam = SceneView.lastActiveSceneView.camera;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            newNode.transform.position = hit.point;
        }
        else
        {
            newNode.transform.position = cam.transform.position + cam.transform.forward * 20;
        }

        newNode.gameObject.name = "PathNode";

        Selection.activeGameObject = newNode.transform.gameObject;
    }
}
