using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LightGroup { Headlights, Breaklights, Reverselights, LeftBlinkers, RightBlinkers }
public class LightRig : MonoBehaviour
{
    [SerializeField] Light[] forwardSpotLights = null;
    [SerializeField] Light[] forwardPointLights = null;
    [SerializeField] Light[] breakPointlights = null;
    [SerializeField] Light[] breakSpotlights = null;

    [SerializeField] Light[] reverseLights = null;
    [SerializeField] Light[] leftBlinkers = null;
    [SerializeField] Light[] rightBlinkers = null;

    //Ranges + intensity

    //Forward headlights (spot)
    float[] forwardSpotIntensity = new float[] { 2, 4 };
    float[] forwardSpotRange = new float[] { 10, 40 };

    //Forward headlights (point)
    float[] forwardPointIntensity = new float[] { 4, 8 };
    float[] forwardPointRange = new float[] { 0.5f, 1 };

    //Breaklights (spot)
    float[] breakSpotIntensity = new float[] { 2, 4 };
    float[] breakSpotRange = new float[] { 5, 20 };

    //Breaklights (point)
    float[] breakPointIntensity = new float[] { 4, 8 };
    float[] breakPointRange = new float[] { 0.5f, 1 };

    //Color
    [SerializeField] Color headlightColor = Color.white;
    [SerializeField] Color blinkersColor = Color.yellow;
    [SerializeField] Color breakColor = Color.red;
    [SerializeField] Color reverseColor = Color.white;

    private void Start()
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

        SetLightgroup(false, LightGroup.Headlights);
        SetLightgroup(false, LightGroup.Breaklights);
        SetLightgroup(false, LightGroup.Reverselights);
        SetLightgroup(false, LightGroup.LeftBlinkers);
        SetLightgroup(false, LightGroup.RightBlinkers);
    }

    bool headlightsOn = true;
    bool leftBlinkersOn = true;
    bool rightBlinkersOn = true;
    bool reverseLightOn = true;
    bool brakeLightOn = true;

    public void SetLightgroup(bool on, LightGroup group)
    {
        int index = on ? 1 : 0;

        switch (group)
        {
            case LightGroup.Headlights:
                headlightsOn = on;
                StartCoroutine(ChangeLightSetting(forwardSpotLights, forwardSpotIntensity[index], forwardSpotRange[index], 0.01f, 2));
                StartCoroutine(ChangeLightSetting(forwardPointLights, forwardPointIntensity[index], forwardPointRange[index], 0.01f, 2));
                break;
            case LightGroup.Breaklights:
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
                leftBlinkersOn = on;
                foreach (var item in leftBlinkers)
                {
                    item.GetComponent<Animator>().SetBool("On", on);
                }
                break;
            case LightGroup.RightBlinkers:
                rightBlinkersOn = on;
                foreach (var item in rightBlinkers)
                {
                    item.GetComponent<Animator>().SetBool("On", on);
                }
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
