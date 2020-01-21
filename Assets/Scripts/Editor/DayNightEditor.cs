using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DayNightSystem))]
public class DayNightEditor : Editor
{
    TimeOfDay timeOfday;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        DayNightSystem system = (DayNightSystem)target;

        timeOfday = (TimeOfDay) EditorGUILayout.EnumPopup(timeOfday);

        if (GUILayout.Button("Set Time"))
        {
            system.SetDayTime(timeOfday, false);
        }
    }
}
