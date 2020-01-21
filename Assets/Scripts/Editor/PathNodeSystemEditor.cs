using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathNodeSystem))]
public class PathNodeSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Validate road network"))
        {
            PathNodeSystem system = (PathNodeSystem)target;
            system.ValidateRoadNetwork();
            Debug.Log("Network analyzed");
        }
    }

}
