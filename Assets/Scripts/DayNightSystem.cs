using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TimeOfDay { Morning, Midday, Afternoon, Evening, Night }
public class DayNightSystem : MonoBehaviour
{
    public static TimeOfDay actualTimeOfDay;
    public bool automaticDayNightCycle = false;
    [SerializeField] float timeOfDaySeconds = 0;
    public float daySpeedMultiplier = 1;

    float[] timeOfDayChanges = new float[] { 60, 120, 180, 240, 300 };

    public Light sun = null;
    public float[] sunIntensity = new float[] { 0.75f, 1, 0.75f, 0.5f, 0.75f };
    public Color[] sunColor = new Color[5];

    [SerializeField] TimeOfDay startTimeOfDay;


    private void Awake()
    {
        SetDayTime(startTimeOfDay, true);
    }

    public void SetDayTime(TimeOfDay newTimeOfDay, bool startUp)
    {
        sun.intensity = sunIntensity[(int)newTimeOfDay];
        sun.color = sunColor[(int)newTimeOfDay];
        timeOfDaySeconds = timeOfDayChanges[(int)newTimeOfDay];
        //Set streetlamps on off etc.
        switch (newTimeOfDay)
        {
            case TimeOfDay.Morning:
                break;
            case TimeOfDay.Midday:
                break;
            case TimeOfDay.Afternoon:
                break;
            case TimeOfDay.Evening:

                break;
            case TimeOfDay.Night:
                timeOfDaySeconds = -60;
                break;
        }
        if (CarSpawnSystem.spawnedCars != null && CarSpawnSystem.spawnedCars.Count > 0)
        {
            if ((actualTimeOfDay != TimeOfDay.Night && newTimeOfDay == TimeOfDay.Night) || startUp)
            {
                foreach (var item in CarSpawnSystem.spawnedCars)
                {
                    if (item != null)
                    {
                        item.GetComponentInChildren<LightRig>().SetLightGroup(true, LightGroup.Headlights);
                    }
                }
            }
            else if ((actualTimeOfDay == TimeOfDay.Night && newTimeOfDay != TimeOfDay.Night) || startUp)
            {
                foreach (var item in CarSpawnSystem.spawnedCars)
                {
                    if (item != null)
                    {
                        item.GetComponentInChildren<LightRig>().SetLightGroup(false, LightGroup.Headlights);
                    }
                }
            }
        }

        actualTimeOfDay = newTimeOfDay;
        startTimeOfDay = newTimeOfDay;
    }


    private void Update()
    {
        if (automaticDayNightCycle)
        {
            timeOfDaySeconds += Time.deltaTime * daySpeedMultiplier;

            switch (actualTimeOfDay)
            {
                case TimeOfDay.Morning:
                    if (timeOfDaySeconds > timeOfDayChanges[(int)actualTimeOfDay + 1])
                    {
                        SetDayTime(TimeOfDay.Midday, false);
                    }
                    break;
                case TimeOfDay.Midday:
                    if (timeOfDaySeconds > timeOfDayChanges[(int)actualTimeOfDay + 1])
                    {
                        SetDayTime(TimeOfDay.Afternoon, false);
                    }
                    break;
                case TimeOfDay.Afternoon:
                    if (timeOfDaySeconds > timeOfDayChanges[(int)actualTimeOfDay + 1])
                    {
                        SetDayTime(TimeOfDay.Evening, false);
                    }
                    break;
                case TimeOfDay.Evening:
                    if (timeOfDaySeconds > timeOfDayChanges[(int)actualTimeOfDay + 1])
                    {
                        SetDayTime(TimeOfDay.Night, false);
                    }
                    break;
                case TimeOfDay.Night:
                    if (timeOfDaySeconds > timeOfDayChanges[0])
                    {
                        SetDayTime(TimeOfDay.Morning, false);
                    }
                    break;
            }
        }
    }
}
