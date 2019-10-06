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

    public override void OnInspectorGUI()
    {
        PathNode myPathNode = (PathNode)target;

        DrawDefaultInspector();


        GUILayout.Label("\nNew node options");

        PathNode newNode = myPathNode; //needed to compile down below


        pathName = EditorGUILayout.TextField("PathName", pathName);
        pathSpeed = EditorGUILayout.FloatField("New path speed", pathSpeed);
        changeSpeed = EditorGUILayout.Toggle("New node has new speed", changeSpeed);

        Event ev = Event.current;
        if (GUILayout.Button("Create new connected node (Shift + C)") || (ev.type == EventType.KeyDown && ev.shift && ev.keyCode == KeyCode.C))
        {
            newNode = Instantiate(Resources.Load<PathNode>("Prefabs/Road/Pathnode"), myPathNode.transform.position, myPathNode.transform.rotation);
            myPathNode.AddConnectedNode(newNode);
            newNode.transform.SetParent(myPathNode.transform.parent);
            newNode.gameObject.name = pathName;
            newNode.SetRoadSpeedLimit(changeSpeed ? pathSpeed : myPathNode.GetRoadSpeedLimit());
            Selection.activeGameObject = newNode.transform.gameObject;
        }

        if (GUILayout.Button("Replace connected node with new node (Shift + R)") || (ev.type == EventType.KeyDown && ev.shift && ev.keyCode == KeyCode.R))
        {
            newNode = Instantiate(Resources.Load<PathNode>("Prefabs/Road/Pathnode"), myPathNode.transform.position, myPathNode.transform.rotation);
            newNode.transform.position = (myPathNode.transform.position + myPathNode.GetPathNodes()[0].transform.position) / 2;
            newNode.AddConnectedNode(myPathNode.GetPathNodes());
            myPathNode.ReplaceConnectedNode(newNode);
            newNode.transform.SetParent(myPathNode.transform.parent);
            newNode.gameObject.name = pathName;
            newNode.SetRoadSpeedLimit(changeSpeed ? pathSpeed : myPathNode.GetRoadSpeedLimit());
            Selection.activeGameObject = newNode.transform.gameObject;
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
