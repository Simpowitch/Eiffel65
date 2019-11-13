using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Node
{
    public Rect rect;

    public string title;
    public bool isDragged;
    public bool isSelected;

    public ConnectionPoint inPoint;
    public ConnectionPoint choiceA;
    public ConnectionPoint choiceB;
    public ConnectionPoint choiceC;
    public ConnectionPoint choiceD;

    public GUIStyle style;
    public GUIStyle defaultNodeStyle;
    public GUIStyle selectedNodeStyle;

    public Action<Node> OnRemoveNode;

    public const float 
        IMAGEOFFSET = 15,
        LOCATIONOFFSET = IMAGEOFFSET + 90,
        DIALOGTEXTOFFSET = LOCATIONOFFSET + 30,

        CHOICEAOFFSET = 15,
        CHOICEBOFFSET = CHOICEAOFFSET + 30,
        CHOICECOFFSET = CHOICEBOFFSET + 30,
        CHOICEDOFFSET = CHOICECOFFSET + 30;

    Rect imageRect;
    Rect locationText;
    Rect textRect;

    Rect choiceARect;
    Rect choiceBRect;
    Rect choiceCRect;
    Rect choiceDRect;

    Dialog dialog;
    

    public Node(Vector2 position, float width, float height, GUIStyle nodeStyle, GUIStyle selectedStyle, GUIStyle inPointStyle, GUIStyle outPointStyle, Action<ConnectionPoint> OnClickInPoint, Action<ConnectionPoint> OnClickOutPoint, Action<Node> OnClickRemoveNode)
    {

        rect = new Rect(position.x, position.y, width, height);
        imageRect = new Rect(position.x + 10, position.y + IMAGEOFFSET, 180, 80);
        locationText = new Rect(position.x + 10, position.y + LOCATIONOFFSET, 180, 20);

        textRect = new Rect(position.x + 10, position.y + DIALOGTEXTOFFSET, 370, 70);

        choiceARect = new Rect(position.x + 200, position.y + CHOICEAOFFSET, 180, 20);
        choiceBRect = new Rect(position.x + 200, position.y + CHOICEBOFFSET, 180, 20);
        choiceCRect = new Rect(position.x + 200, position.y + CHOICECOFFSET, 180, 20);
        choiceDRect = new Rect(position.x + 200, position.y + CHOICEDOFFSET, 180, 20);


        style = nodeStyle;
        inPoint = new ConnectionPoint(this, ConnectionPointType.In, inPointStyle, OnClickInPoint, 0f);
        choiceA = new ConnectionPoint(this, ConnectionPointType.Out, outPointStyle, OnClickOutPoint, CHOICEAOFFSET);
        choiceB = new ConnectionPoint(this, ConnectionPointType.Out, outPointStyle, OnClickOutPoint, CHOICEBOFFSET);
        choiceC = new ConnectionPoint(this, ConnectionPointType.Out, outPointStyle, OnClickOutPoint, CHOICECOFFSET);
        choiceD = new ConnectionPoint(this, ConnectionPointType.Out, outPointStyle, OnClickOutPoint, CHOICEDOFFSET);

        defaultNodeStyle = nodeStyle;
        selectedNodeStyle = selectedStyle;
        OnRemoveNode = OnClickRemoveNode;
        dialog = new Dialog("Swamp", "", new List<Choice>());
    }

    public void Drag(Vector2 delta)
    {
        rect.position += delta;
        textRect.position += delta;
        imageRect.position += delta;
        locationText.position += delta;
        choiceARect.position += delta;
        choiceBRect.position += delta;
        choiceCRect.position += delta;
        choiceDRect.position += delta;

    }
    public void Draw()
    {
        EditorStyles.textField.wordWrap = true;
        inPoint.Draw();
        choiceA.Draw();
        choiceB.Draw();
        choiceC.Draw();
        choiceD.Draw();
        GUI.Box(rect, title, style);

        dialog.dialogText = EditorGUI.TextField(textRect, "Dialog Text");
        dialog.image = (Texture2D)EditorGUI.ObjectField(imageRect, dialog.image, typeof(Texture2D), false);
        dialog.location = EditorGUI.TextField(locationText, "Location Text");
        EditorGUI.TextField(choiceARect, "Choice A");
        EditorGUI.TextField(choiceBRect, "Choice B");
        EditorGUI.TextField(choiceCRect, "Choice C");
        EditorGUI.TextField(choiceDRect, "Choice D");


    }

    public bool ProcessEvents(Event e)
    {
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    if (rect.Contains(e.mousePosition))
                    {
                        isDragged = true;
                        GUI.changed = true;
                        isSelected = true;
                        style = selectedNodeStyle;
                    }
                    else
                    {
                        GUI.changed = true;
                        isSelected = false;
                        style = defaultNodeStyle;
                    }
                }
                if (e.button == 1 && isSelected && rect.Contains(e.mousePosition))
                {
                    ProcessContextMenu();
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                isDragged = false;
                break;

            case EventType.MouseDrag:
                if (e.button == 0 && isDragged)
                {
                    Drag(e.delta);
                    e.Use();
                    return true;
                }
                break;
        }
        return false;
    }
    private void ProcessContextMenu()
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Remove node"), false, OnClickRemoveNode);
        genericMenu.ShowAsContext();
    }

    private void OnClickRemoveNode()
    {
        if (OnRemoveNode != null)
        {
            OnRemoveNode(this);
        }
    }
}
