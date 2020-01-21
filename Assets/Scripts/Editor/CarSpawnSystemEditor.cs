using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CarSpawnSystem))]
public class CarSpawnSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        CarSpawnSystem system = (CarSpawnSystem)target;

        if (GUILayout.Button("Despawn All Cars"))
        {
            system.DespawnAllCars();
        }
    }
}

