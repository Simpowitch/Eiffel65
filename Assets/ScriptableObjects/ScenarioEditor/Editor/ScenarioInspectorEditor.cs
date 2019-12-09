using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Scenario))]
public class ScenarioInspectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Editor"))
        {
            ScenarioEditor.OpenWindow((Scenario)target);
        }

        base.OnInspectorGUI();
    }
}
