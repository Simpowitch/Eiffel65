using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedTrafficlights : MonoBehaviour
{
    [SerializeField] PathNode[] lightGroup1;
    [SerializeField] PathNode[] lightGroup2;

    // Start is called before the first frame update
    void Start()
    {
        //Sets the opposite of default to the lightgroup1
        for (int i = 0; i < lightGroup1.Length; i++)
        {
            lightGroup1[i].SwitchAllowedToPass();
        }
    }

    float timer;
    [SerializeField] float timeBetweenLightswitch = 10f;

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= timeBetweenLightswitch)
        {
            timer -= timeBetweenLightswitch;
            ReverseLights();
        }
    }

    

    private void ReverseLights()
    {
        for (int i = 0; i < lightGroup1.Length; i++)
        {
            lightGroup1[i].SwitchAllowedToPass();
        }

        for (int i = 0; i < lightGroup2.Length; i++)
        {
            lightGroup2[i].SwitchAllowedToPass();
        }
    }
}
