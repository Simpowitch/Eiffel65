using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LightGroup { Headlights, BrakeLights, Reverselights, LeftBlinkers, RightBlinkers, PoliceLights }
public class LightRig : MonoBehaviour
{
    [SerializeField] Light[] forwardSpotLights = null;
    [SerializeField] Light[] forwardPointLights = null;
    [SerializeField] Light[] breakPointlights = null;
    [SerializeField] Light[] breakSpotlights = null;

    [SerializeField] Light[] reverseLights = null;
    [SerializeField] Light[] leftBlinkers = null;
    [SerializeField] Light[] rightBlinkers = null;

    [SerializeField] Light[] policeLights = null;
    [SerializeField] Animator policeLightAnimator = null;

    //Ranges + intensity

    //Forward headlights (spot)
    float[] forwardSpotIntensity = new float[] { 200, 600 };
    float[] forwardSpotRange = new float[] { 5, 40 };

    //Forward headlights (point)
    float[] forwardPointIntensity = new float[] { 200, 600 };
    float[] forwardPointRange = new float[] { 0.25f, 0.5f };

    //Breaklights (spot)
    float[] breakSpotIntensity = new float[] { 10, 100 };
    float[] breakSpotRange = new float[] { 3, 10 };

    //Breaklights (point)
    float[] breakPointIntensity = new float[] { 10, 50 };
    float[] breakPointRange = new float[] { 0.1f, 0.5f };

    //Color
    [SerializeField] Color headlightColor = Color.white;
    [SerializeField] Color blinkersColor = Color.yellow;
    [SerializeField] Color breakColor = Color.red;
    [SerializeField] Color reverseColor = Color.white;
    [SerializeField] Color policeColor = Color.blue;


    private void Awake()
    {
        foreach (var item in forwardSpotLights)
        {
            item.color = headlightColor;
        }
        foreach (var item in forwardPointLights)
        {
            item.color = headlightColor;
        }
        foreach (var item in breakSpotlights)
        {
            item.color = breakColor;
        }
        foreach (var item in breakPointlights)
        {
            item.color = breakColor;
        }
        foreach (var item in reverseLights)
        {
            item.color = reverseColor;
        }
        foreach (var item in leftBlinkers)
        {
            item.color = blinkersColor;
        }
        foreach (var item in rightBlinkers)
        {
            item.color = blinkersColor;
        }
        foreach (var item in policeLights)
        {
            item.color = policeColor;
        }

        SetLightGroup(false, LightGroup.Headlights);
        SetLightGroup(false, LightGroup.BrakeLights);
        SetLightGroup(false, LightGroup.Reverselights);
        SetLightGroup(false, LightGroup.LeftBlinkers);
        SetLightGroup(false, LightGroup.RightBlinkers);
        if (policeLightAnimator != null)
        {
            SetLightGroup(false, LightGroup.PoliceLights);
        }
    }

    bool headlightsOn = true;
    bool leftBlinkersOn = true;
    bool rightBlinkersOn = true;
    bool reverseLightOn = true;
    bool brakeLightOn = true;
    bool policeLightOn = true;


    public void SetLightGroup(LightGroup group)
    {
        switch (group)
        {
            case LightGroup.Headlights:
                SetLightGroup(!headlightsOn, LightGroup.Headlights);
                break;
            case LightGroup.LeftBlinkers:
                SetLightGroup(!leftBlinkersOn, LightGroup.LeftBlinkers);
                break;
            case LightGroup.RightBlinkers:
                SetLightGroup(!rightBlinkersOn, LightGroup.RightBlinkers);
                break;
            case LightGroup.PoliceLights:
                SetLightGroup(!policeLightOn, LightGroup.PoliceLights);
                break;
            case LightGroup.BrakeLights:
            case LightGroup.Reverselights:
                Debug.LogWarning("No Function");
                break;
        }
    }

    public void SetLightGroup(bool on, LightGroup group)
    {
        int index = on ? 1 : 0;

        switch (group)
        {
            case LightGroup.Headlights:
                headlightsOn = on;
                StartCoroutine(ChangeLightSetting(forwardSpotLights, forwardSpotIntensity[index], forwardSpotRange[index], 0.01f, 2));
                StartCoroutine(ChangeLightSetting(forwardPointLights, forwardPointIntensity[index], forwardPointRange[index], 0.01f, 2));
                break;
            case LightGroup.BrakeLights:
                if (on == brakeLightOn)
                {
                    return;
                }
                brakeLightOn = on;
                StartCoroutine(ChangeLightSetting(breakPointlights, breakPointIntensity[index], breakPointRange[index], 0.01f, 2));
                StartCoroutine(ChangeLightSetting(breakSpotlights, breakSpotIntensity[index], breakSpotRange[index], 0.01f, 2));
                break;
            case LightGroup.Reverselights:
                if (on == reverseLightOn)
                {
                    return;
                }
                reverseLightOn = on;
                ChangeLightSetting(reverseLights, on);
                break;
            case LightGroup.LeftBlinkers:
                if (on == leftBlinkersOn)
                {
                    return;
                }
                leftBlinkersOn = on;
                foreach (var item in leftBlinkers)
                {
                    item.GetComponent<Animator>().SetBool("On", on);
                }
                break;
            case LightGroup.RightBlinkers:
                if (on == rightBlinkersOn)
                {
                    return;
                }
                rightBlinkersOn = on;
                foreach (var item in rightBlinkers)
                {
                    item.GetComponent<Animator>().SetBool("On", on);
                }
                break;
            case LightGroup.PoliceLights:
                policeLightOn = on;
                policeLightAnimator.SetBool("On", on);
                break;
        }
    }

    IEnumerator ChangeLightSetting(Light[] group, float newIntensity, float newRange, float timeToChange, int substeps)
    {
        float intensityChange = newIntensity - group[0].intensity;
        float rangeChange = newRange - group[0].range;

        intensityChange /= substeps;
        rangeChange /= substeps;

        for (int i = 0; i < substeps; i++)
        {
            foreach (var item in group)
            {
                item.intensity += intensityChange;
                item.range += rangeChange;
            }
            yield return new WaitForSeconds(timeToChange / substeps);
        }
    }

    private void ChangeLightSetting(Light[] group, bool on)
    {
        foreach (var item in group)
        {
            item.enabled = on;
        }
    }
}
