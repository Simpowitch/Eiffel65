using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PathNode))]
public class PathNodeEditor : Editor
{
    string pathName = "PathNode";
    float pathSpeed = 0;
    bool changeSpeed = false;
    int metersBetweenNodes = 15;
    bool createExtraLanesToTheRight = false;
    int metersBetweenLanes = 4;
    int lanesToCreate = 1;
    bool forbidLaneChangeInEnd = false;
    int laneChangeDisallowanceNodes = 1;

    public override void OnInspectorGUI()
    {
        PathNode myPathNode = (PathNode)target;

        DrawDefaultInspector();

        GUILayout.Label("\nNew node options");

        pathName = EditorGUILayout.TextField("PathName", pathName);
        changeSpeed = EditorGUILayout.Toggle("New node has new speed", changeSpeed);
        if (changeSpeed)
        {
            pathSpeed = EditorGUILayout.FloatField("New path speed", pathSpeed);
        }

        Event ev = Event.current;
        if (GUILayout.Button("Create new connected node (Shift + C)") || (ev.type == EventType.KeyDown && ev.shift && ev.keyCode == KeyCode.C))
        {
            Selection.activeGameObject = CreateNewNode(myPathNode);
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

    private GameObject CreateNewNode(PathNode myPathNode)
    {
        PathNode newNode;
        newNode = Instantiate(Resources.Load<PathNode>("Prefabs/Road/Pathnode"), myPathNode.transform.position, myPathNode.transform.rotation);
        myPathNode.AddConnectedNode(newNode);
        newNode.transform.SetParent(myPathNode.transform.parent);
        newNode.gameObject.name = pathName;
        newNode.SetRoadSpeedLimit(changeSpeed ? pathSpeed : myPathNode.GetRoadSpeedLimit());
        return newNode.gameObject;
    }

    private GameObject ReplaceNodeSingle(PathNode myPathNode)
    {
        PathNode newNode;
        newNode = Instantiate(Resources.Load<PathNode>("Prefabs/Road/Pathnode"), myPathNode.transform.position, myPathNode.transform.rotation);
        newNode.transform.position = (myPathNode.transform.position + myPathNode.GetPathNodes()[0].transform.position) / 2;
        newNode.AddConnectedNode(myPathNode.GetPathNodes());
        newNode.SetRoadSpeedLimit(changeSpeed ? pathSpeed : myPathNode.GetPathNodes()[0].GetRoadSpeedLimit());
        myPathNode.ReplaceConnectedNode(newNode);
        newNode.transform.SetParent(myPathNode.transform.parent);
        newNode.gameObject.name = pathName;
        return newNode.gameObject;
    }

    private GameObject ReplaceNodeMultiple(PathNode myPathNode)
    {
        PathNode newNode = myPathNode;
        Vector3 endPos = myPathNode.GetPathNodes()[0].transform.position;
        int nodesToCreate = Mathf.FloorToInt(Vector3.Distance(myPathNode.transform.position, myPathNode.GetPathNodes()[0].transform.position) / metersBetweenNodes);
        Vector3 direction = myPathNode.GetPathNodes()[0].transform.position - myPathNode.transform.position;
        direction = direction.normalized;
        Vector3 position = myPathNode.transform.position;
        PathNode previousNode = myPathNode;

        List<PathNode> originalConnections = new List<PathNode>();
        originalConnections.AddRange(myPathNode.GetPathNodes());
        for (int i = 0; i < originalConnections.Count; i++)
        {
            originalConnections[i].backwardNodes.Remove(myPathNode);
        }

        List<PathNode> createdNodes = new List<PathNode>();

        for (int i = 0; i < nodesToCreate; i++)
        {
            position += direction * metersBetweenNodes;
            if (Vector3.Distance(position, endPos) > metersBetweenNodes / 2)
            {
                newNode = Instantiate(Resources.Load<PathNode>("Prefabs/Road/Pathnode"), myPathNode.transform.position, myPathNode.transform.rotation);
                newNode.transform.position = position;
                newNode.SetRoadSpeedLimit(changeSpeed ? pathSpeed : myPathNode.GetPathNodes()[0].GetRoadSpeedLimit());
                newNode.transform.SetParent(myPathNode.transform.parent);
                newNode.gameObject.name = pathName;
                createdNodes.Add(newNode);
            }
        }
        myPathNode.ClearForwardConnections();
        myPathNode.AddConnectedNode(createdNodes[0]);
        for (int i = 0; i < createdNodes.Count - 1; i++)
        {
            createdNodes[i].AddConnectedNode(createdNodes[i + 1]);
        }
        //add the original nodes to the last created node
        newNode = createdNodes[createdNodes.Count - 1];
        newNode.AddConnectedNode(originalConnections);
        return newNode.gameObject;
    }

    private GameObject CreateRoadSection(PathNode myPathNode)
    {
        PathNode newNode = myPathNode;
        GameObject empty = new GameObject();
        Transform parent = Instantiate(empty).transform;
        DestroyImmediate(empty);
        parent.gameObject.name = "RoadSegment";
        parent.SetParent(myPathNode.gameObject.transform.parent);
        parent.transform.position = (myPathNode.transform.position + myPathNode.GetPathNodes()[0].transform.position) / 2;

        myPathNode.transform.LookAt(myPathNode.GetPathNodes()[0].transform);

        Vector3 startPos = myPathNode.transform.position;
        Vector3 endPos = myPathNode.GetPathNodes()[0].transform.position;
        int nodesToCreate = Mathf.FloorToInt(Vector3.Distance(myPathNode.transform.position, myPathNode.GetPathNodes()[0].transform.position) / metersBetweenNodes) + 1; //the start and finish nodes will be removed
        Vector3 direction = myPathNode.GetPathNodes()[0].transform.position - myPathNode.transform.position;
        direction = direction.normalized;
        Vector3 position = startPos;

        List<PathNode>[] lanes = new List<PathNode>[lanesToCreate];
        for (int i = 0; i < lanes.Length; i++)
        {
            lanes[i] = new List<PathNode>();
        }

        Vector3 offset = myPathNode.transform.right * (createExtraLanesToTheRight ? (float)metersBetweenLanes : (float)-metersBetweenLanes);

        for (int lane = 0; lane < lanes.Length; lane++)
        {
            PathNode previousNode = null;
            position = startPos + offset * lane;


            for (int node = 0; node < nodesToCreate; node++)
            {
                //last node
                if (node == nodesToCreate - 1)
                {
                    position = endPos + offset * lane;
                }

                newNode = Instantiate(Resources.Load<PathNode>("Prefabs/Road/Pathnode"), position, myPathNode.transform.rotation);

                newNode.SetRoadSpeedLimit(changeSpeed ? pathSpeed : myPathNode.GetPathNodes()[0].GetRoadSpeedLimit());

                if (previousNode != null)
                {
                    previousNode.AddConnectedNode(newNode);
                }

                string name = pathName;
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

                newNode.transform.SetParent(parent);

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
                    if (forbidLaneChangeInEnd && node >= (lanes[lane].Count - laneChangeDisallowanceNodes - 1))
                    {
                        continue;
                    }

                    if (lane + 1 < lanes.Length && node + 1 < lanes[lane + 1].Count)
                    {
                        lanes[lane][node].AddConnectedNode(lanes[lane + 1][node + 1]);
                    }
                    if (lane - 1 >= 0 && node + 1 < lanes[lane - 1].Count)
                    {
                        lanes[lane][node].AddConnectedNode(lanes[lane - 1][node + 1]);
                    }
                }
            }
        }

        GameObject.DestroyImmediate(myPathNode.GetPathNodes()[0].gameObject);
        GameObject.DestroyImmediate(myPathNode.gameObject);
        return newNode.gameObject;
    }


    ////Enable connectionmaking in sceneview
    //public Object obj;
    //private void OnSceneGUI()
    //{
    //    PathNode myPathNode = (PathNode)target;
    //    Vector3 position = myPathNode.transform.position;
    //    Rect square = EditorGUILayout.BeginHorizontal();

    //    obj = EditorGUILayout.ObjectField(obj, typeof(Object), true);
    //    EditorGUILayout.EndHorizontal();
    //}

    private void OnSceneGUI()
    {
        Event ev = Event.current;
        if (Event.current.type == EventType.KeyDown && !Event.current.shift && Event.current.keyCode == KeyCode.C)
        {
            if (PathNode.selectedNodeForConnection == null)
            {
                PathNode.selectedNodeForConnection = (PathNode)target;
                Debug.Log("Node to connect set");
            }
            else
            {
                PathNode.selectedNodeForConnection.AddConnectedNode((PathNode)target);
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
                Selection.activeGameObject = CreateNewNode(myPathNode);
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
        PathNode newNode = Instantiate(Resources.Load<PathNode>("Prefabs/Road/Pathnode"));
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
