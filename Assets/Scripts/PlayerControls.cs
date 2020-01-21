using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControls : MonoBehaviour
{
    [SerializeField] LightRig lightSystem = null;
    [SerializeField] PoliceVehicle policeVehicle = null;

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.H))
        {
            lightSystem.SetLightGroup(LightGroup.Headlights);
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            lightSystem.SetLightGroup(LightGroup.LeftBlinkers);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            lightSystem.SetLightGroup(LightGroup.RightBlinkers);
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            lightSystem.SetLightGroup(LightGroup.PoliceLights);
            policeVehicle.usingSirens = !policeVehicle.usingSirens;
        }
    }
}
