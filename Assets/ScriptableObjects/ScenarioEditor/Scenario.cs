using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//Simon Voss
[CreateAssetMenu(menuName = "Scenario/Scenario")]
public class Scenario : ScriptableObject
{
    public ScenarioEvent startEvent = null;

#if UNITY_EDITOR
    public List<ChoiceNode> editorChoiceNodes = new List<ChoiceNode>();
    public List<EventNode> editorEventNodes = new List<EventNode>();
    public List<Connection> editorConnections = new List<Connection>();
    public List<ScenarioEndNode> editorScenarioEndNodes = new List<ScenarioEndNode>();
#endif
    public List<ScenarioEvent> events = new List<ScenarioEvent>();

    public ScenarioEvent FindNextEvent(Choice usedChoice)
    {
        for (int i = 0; i < events.Count; i++)
        {
            if (usedChoice.nextEventID == events[i].id)
            {
                return events[i];
            }
        }
        Debug.Log("New event not found");
        return null;
    }
}

[System.Serializable]
public class ScenarioEvent
{
    public string locationText;
    public string description;
    public Sprite image;
    public List<Choice> choices = new List<Choice>();

    public double id;
    public static List<double> usedIDs = new List<double>();

    public ScenarioEvent(string locationText, string description)
    {
        this.locationText = locationText;
        this.description = description;
        id = GetUniqueID();
    }

    public double GetUniqueID()
    {
        System.Random rng = new System.Random();
        double newID = rng.NextDouble();
        if (!usedIDs.Contains(newID))
        {
            usedIDs.Add(newID);
            return newID;
        }
        else
        {
            return GetUniqueID();
        }
    }
}

//Contains data of choices and it's effects on the world
[System.Serializable]
public class Choice
{
    public string choiceText = "";

    //Reward/skillincrease


    //outputs
    //public Event nextEvent = null;
    public double nextEventID;
    public Scenario nextScenario = null;
    public string nextScene = "";

    public Choice(string choiceText)
    {
        this.choiceText = choiceText;
    }
}
