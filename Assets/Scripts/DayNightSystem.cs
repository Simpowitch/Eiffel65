using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TimeOfDay { Morning, Midday, Afternoon, Evening, Night }
public class DayNightSystem : MonoBehaviour
{
    TimeOfDay actualTimeOfDay;
    public bool automaticDayNightCycle = false;
    float timeOfDaySeconds = 0;
    public float daySpeedMultiplier = 1;

    float[] timeOfDayChanges = new float[] { 60, 120, 180, 240, 300 };

    public Light sun = null;
    public float[] sunIntensity = new float[] { 0.75f, 1, 0.75f, 0.5f, 0.75f };
    public Color[] sunColor = new Color[5];



    public void SetDayTime(TimeOfDay newTimeOfDay)
    {
        sun.intensity = sunIntensity[(int)newTimeOfDay];
        sun.color = sunColor[(int)newTimeOfDay];
        timeOfDaySeconds = timeOfDayChanges[(int)newTimeOfDay];
        actualTimeOfDay = newTimeOfDay;
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
                        SetDayTime(TimeOfDay.Midday);
                    }
                    break;
                case TimeOfDay.Midday:
                    if (timeOfDaySeconds > timeOfDayChanges[(int)actualTimeOfDay + 1])
                    {
                        SetDayTime(TimeOfDay.Afternoon);
                    }
                    break;
                case TimeOfDay.Afternoon:
                    if (timeOfDaySeconds > timeOfDayChanges[(int)actualTimeOfDay + 1])
                    {
                        SetDayTime(TimeOfDay.Evening);
                    }
                    break;
                case TimeOfDay.Evening:
                    if (timeOfDaySeconds > timeOfDayChanges[(int)actualTimeOfDay + 1])
                    {
                        SetDayTime(TimeOfDay.Night);
                    }
                    break;
                case TimeOfDay.Night:
                    if (timeOfDaySeconds > timeOfDayChanges[0])
                    {
                        SetDayTime(TimeOfDay.Morning);
                    }
                    break;
            }
        }
    }
}
