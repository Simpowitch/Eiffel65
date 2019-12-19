using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PathNodeNetwork))]
public class PathNodeNetworkEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PathNodeNetwork network = (PathNodeNetwork)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Save network"))
        {
            network.SavePathnodesAndConnections();
        }
    }
}
