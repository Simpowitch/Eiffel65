using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialog Scenario/Scenario")]
public class Scenario : ScriptableObject
{
    List<Dialog> dialogs;
}

public class Dialog
{

    public string location;
    public string dialogText;
    public Texture2D image;
    List<Choice> choices;
    public Dialog(string location, string dialogText, List<Choice> choices)
    {
        this.location = location;
        this.dialogText = dialogText;
        this.choices = choices;
    }
}

public class Choice
{
    string choiceText;
    Dialog nextDialog;

    Choice(string choiceText, Dialog nextDialog)
    {
        this.choiceText = choiceText;
        this.nextDialog = nextDialog;
    }
}
