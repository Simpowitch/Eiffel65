using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scenario/Message")]
[System.Serializable]
public class Message : ScriptableObject
{
    public string sender;
    [TextArea(4, 4)]
    public string message;
}