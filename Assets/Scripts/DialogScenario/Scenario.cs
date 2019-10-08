using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scenario
{
    List<Dialog> dialogs;
}

public class Dialog
{

    public string location;
    public string dialogText;
    List<Choice> choices;
    Dialog(string location, string dialogText, List<Choice> choices)
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
