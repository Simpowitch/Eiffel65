using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedTrafficlights : MonoBehaviour
{
    private enum State { Group1, Group2 }
    State state = State.Group1;
    [SerializeField] PathNode[] carLightgroup1 = null;
    [SerializeField] PedestrianTrafficLight[] pedestrianLightgroup1 = null;
    [SerializeField] PathNode[] carLightgroup2 = null;
    [SerializeField] PedestrianTrafficLight[] pedestrianLightgroup2 = null;
    bool redForAll = false;

    // Start is called before the first frame update
    void Start()
    {
        //Sets the opposite of default to the lightgroup1
        for (int i = 0; i < carLightgroup2.Length; i++)
        {
            carLightgroup2[i].SetAllowedToPass(false);
        }
        for (int i = 0; i < pedestrianLightgroup2.Length; i++)
        {
            pedestrianLightgroup2[i].greenLight = false;
        }
    }

    float timer;
    [SerializeField] float greenLightTime = 10f;
    [SerializeField] float timeBetweenLightSwitches = 3f;

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (!redForAll)
        {
            if (timer >= greenLightTime)
            {
                timer -= greenLightTime;
                TurnAllRed();
            }
        }
        else
        {
            if (timer >= timeBetweenLightSwitches)
            {
                timer -= timeBetweenLightSwitches;
                redForAll = false;
                ReverseLights();
            }
        }
    }

    private void TurnAllRed()
    {
        redForAll = true;

        if (state == State.Group1)
        {
            for (int i = 0; i < carLightgroup1.Length; i++)
            {
                carLightgroup1[i].SetAllowedToPass(false);
            }
            for (int i = 0; i < pedestrianLightgroup1.Length; i++)
            {
                pedestrianLightgroup1[i].greenLight = false;
            }
        }
        else
        {
            for (int i = 0; i < carLightgroup2.Length; i++)
            {
                carLightgroup2[i].SetAllowedToPass(false);
            }
            for (int i = 0; i < pedestrianLightgroup2.Length; i++)
            {
                pedestrianLightgroup2[i].greenLight = false;
            }
        }

    }

    private void ReverseLights()
    {
        if (state == State.Group1)
        {
            state = State.Group2;
            for (int i = 0; i < carLightgroup2.Length; i++)
            {
                carLightgroup2[i].SetAllowedToPass(true);
            }
            for (int i = 0; i < pedestrianLightgroup2.Length; i++)
            {
                pedestrianLightgroup2[i].greenLight = true;
            }
        }
        else
        {
            state = State.Group1;
            for (int i = 0; i < carLightgroup1.Length; i++)
            {
                carLightgroup1[i].SetAllowedToPass(true);
            }
            for (int i = 0; i < pedestrianLightgroup1.Length; i++)
            {
                pedestrianLightgroup1[i].greenLight = true;
            }
        }
    }
}
