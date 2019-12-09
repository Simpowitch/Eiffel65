using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//http://gram.gs/gramlog/creating-node-based-editor-unity/

//Simon Voss
public class ScenarioEditor : EditorWindow
{
    public static List<Node> allNodes = new List<Node>();
    private static List<Connection> allConnections = new List<Connection>();

    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle inPointStyle;
    private GUIStyle outPointStyle;

    private static ConnectionPoint selectedNodeInPoint;
    private static ConnectionPoint selectedNodeOutPoint;

    private Vector2 offset;
    private Vector2 drag;

    public static Scenario openScenario;



    public static void OpenWindow(Scenario input)
    {
        ScenarioEditor window = GetWindow<ScenarioEditor>();
        window.titleContent = new GUIContent("Scenario-Window - " + input.name);
        openScenario = input;
        allNodes.Clear();
        allConnections.Clear();
    }


    private void OnEnable()
    {
        SetStyles();
    }

    private void SetStyles()
    {
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
        nodeStyle.border = new RectOffset(12, 12, 12, 12);

        selectedNodeStyle = new GUIStyle();
        selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
        selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);

        inPointStyle = new GUIStyle();
        inPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left.png") as Texture2D;
        inPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left on.png") as Texture2D;
        inPointStyle.border = new RectOffset(4, 4, 12, 12);

        outPointStyle = new GUIStyle();
        outPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right.png") as Texture2D;
        outPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right on.png") as Texture2D;
        outPointStyle.border = new RectOffset(4, 4, 12, 12);
    }


    private void OnGUI()
    {
        DrawGrid(20, 0.2f, Color.gray);
        DrawGrid(100, 0.4f, Color.gray);

        DrawNodes();
        DrawConnections();
        DrawButtons();

        DrawConnectionLine(UnityEngine.Event.current);

        ProcessNodeEvents(UnityEngine.Event.current);
        ProcessEvents(UnityEngine.Event.current);

        if (GUI.changed)
        {
            Repaint();
        }
    }

    //Draw the graphics of nodes
    private void DrawNodes()
    {
        if (allNodes != null)
        {
            for (int i = 0; i < allNodes.Count; i++)
            {
                if (allNodes[i] is EventNode)
                {
                    EventNode node = allNodes[i] as EventNode;
                    node.Draw();
                }
                else if (allNodes[i] is ChoiceNode)
                {
                    ChoiceNode node = allNodes[i] as ChoiceNode;
                    node.Draw();
                }
                else if (allNodes[i] is ScenarioEndNode)
                {
                    ScenarioEndNode node = allNodes[i] as ScenarioEndNode;
                    node.Draw();
                }
            }
        }
    }
    //Draw the graphics of connections
    private void DrawConnections()
    {
        if (allConnections != null)
        {
            for (int i = 0; i < allConnections.Count; i++)
            {
                allConnections[i].Draw();
            }
        }
    }
    //Draw the graphics of connection lines
    private void DrawConnectionLine(UnityEngine.Event e)
    {
        if (selectedNodeInPoint != null && selectedNodeOutPoint == null)
        {
            Handles.DrawBezier(
                selectedNodeInPoint.rect.center,
                e.mousePosition,
                selectedNodeInPoint.rect.center + Vector2.left * 50f,
                e.mousePosition - Vector2.left * 50f,
                Color.white,
                null,
                2f
            );

            GUI.changed = true;
        }

        if (selectedNodeOutPoint != null && selectedNodeInPoint == null)
        {
            Handles.DrawBezier(
                selectedNodeOutPoint.rect.center,
                e.mousePosition,
                selectedNodeOutPoint.rect.center - Vector2.left * 50f,
                e.mousePosition + Vector2.left * 50f,
                Color.white,
                null,
                2f
            );

            GUI.changed = true;
        }
    }
    //Draw the graphics of the grid
    private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
    {
        int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
        int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

        offset += drag * 0.5f;
        Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);

        for (int i = 0; i < widthDivs; i++)
        {
            Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset, new Vector3(gridSpacing * i, position.height, 0f) + newOffset);
        }

        for (int j = 0; j < heightDivs; j++)
        {
            Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset, new Vector3(position.width, gridSpacing * j, 0f) + newOffset);
        }

        Handles.color = Color.white;
        Handles.EndGUI();
    }
    //Draw buttons WITH functions
    private void DrawButtons()
    {
        if (GUILayout.Button("Save Scenario"))
        {
            SaveScenario();
        }

        if (GUILayout.Button("Load Scenario-Data"))
        {
            LoadScenario();
        }
    }

    //Process mouse input and mouseevents
    private void ProcessEvents(UnityEngine.Event e)
    {
        drag = Vector2.zero;

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 1)
                {
                    ProcessContextMenu(e.mousePosition);
                }
                break;
            case EventType.MouseDrag:
                if (e.button == 0)
                {
                    OnDrag(e.delta);
                }
                break;
        }
    }
    private void ProcessNodeEvents(UnityEngine.Event e)
    {
        if (allNodes != null)
        {
            for (int i = allNodes.Count - 1; i >= 0; i--)
            {
                bool guiChanged = allNodes[i].ProcessEvents(e);

                if (guiChanged)
                {
                    GUI.changed = true;
                }
            }
        }
    }

    //Rightclicking creates menues to create nodes and such
    private void ProcessContextMenu(Vector2 mousePosition)
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Add event node"), false, () => OnClickAddEventNode(mousePosition));
        genericMenu.AddItem(new GUIContent("Add choice node"), false, () => OnClickAddChoiceNode(mousePosition));
        genericMenu.AddItem(new GUIContent("Add scenario-end-node"), false, () => OnClickAddScenarioEndNode(mousePosition));

        genericMenu.ShowAsContext();
    }

    //Create Eventnode
    private void OnClickAddEventNode(Vector2 mousePosition)
    {
        if (allNodes == null)
        {
            allNodes = new List<Node>();
        }

        allNodes.Add(new EventNode(mousePosition, nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, OnClickInPoint, OnClickOutPoint, OnClickRemoveNode));
    }

    //Create Choicenode
    private void OnClickAddChoiceNode(Vector2 mousePosition)
    {
        if (allNodes == null)
        {
            allNodes = new List<Node>();
        }

        allNodes.Add(new ChoiceNode(mousePosition, nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, OnClickInPoint, OnClickOutPoint, OnClickRemoveNode));
    }

    //Create EndScenarioNode
    private void OnClickAddScenarioEndNode(Vector2 mousePosition)
    {
        if (allNodes == null)
        {
            allNodes = new List<Node>();
        }

        allNodes.Add(new ScenarioEndNode(mousePosition, nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, OnClickInPoint, OnClickOutPoint, OnClickRemoveNode));
    }


    private void OnClickInPoint(ConnectionPoint inPoint)
    {
        selectedNodeInPoint = inPoint;

        if (selectedNodeOutPoint != null)
        {
            if (selectedNodeOutPoint.node != selectedNodeInPoint.node)
            {
                CreateConnection();
                ClearConnectionSelection();
            }
            else
            {
                ClearConnectionSelection();
            }
        }
    }

    private void OnClickOutPoint(ConnectionPoint outPoint)
    {
        selectedNodeOutPoint = outPoint;

        if (selectedNodeInPoint != null)
        {
            if (selectedNodeOutPoint.node != selectedNodeInPoint.node)
            {
                CreateConnection();
                ClearConnectionSelection();
            }
            else
            {
                ClearConnectionSelection();
            }
        }
    }

    //Remove connection
    private void OnClickRemoveConnection(Connection connection)
    {
        allConnections.Remove(connection);
    }

    //Create connection
    private void CreateConnection()
    {
        //Return and dont allow connection if the inpoint selected node is not allowed by the selected outpoint node
        if (!selectedNodeOutPoint.node.allowedConnectionTypes.Contains(selectedNodeInPoint.node.typeOfNode))
        {
            return;
        }

        if (allConnections == null)
        {
            allConnections = new List<Connection>();
        }

        allConnections.Add(new Connection(selectedNodeOutPoint, selectedNodeInPoint, OnClickRemoveConnection));
    }

    private void ClearConnectionSelection()
    {
        selectedNodeInPoint = null;
        selectedNodeOutPoint = null;
    }
    private void OnClickRemoveNode(Node node)
    {
        if (allConnections != null)
        {
            List<Connection> connectionsToRemove = new List<Connection>();

            for (int i = 0; i < allConnections.Count; i++)
            {
                for (int j = 0; j < node.outPoints.Count; j++)
                {
                    if (allConnections[i].inPoint == node.outPoints[j])
                    {
                        connectionsToRemove.Add(allConnections[i]);
                    }
                }
                if (allConnections[i].outPoint == node.inPoint)
                {
                    connectionsToRemove.Add(allConnections[i]);
                }
            }

            for (int i = 0; i < connectionsToRemove.Count; i++)
            {
                allConnections.Remove(connectionsToRemove[i]);
            }

            connectionsToRemove = null;
        }
        allNodes.Remove(node);
    }
    private void OnDrag(Vector2 delta)
    {
        drag = delta;

        if (allNodes != null)
        {
            for (int i = 0; i < allNodes.Count; i++)
            {
                allNodes[i].Drag(delta);
            }
        }

        GUI.changed = true;
    }


    //Main function to call when saving the scenario from the window
    public void SaveScenario()
    {
        if (!openScenario)
        {
            Debug.LogError("No open scenario");
            return;
        }

        //Find startnode and secure that there is only one
        EventNode startNode = null;
        for (int i = 0; i < allNodes.Count; i++)
        {
            if (allNodes[i] is EventNode)
            {
                EventNode test = allNodes[i] as EventNode;

                if (test.isStartNode)
                {
                    if (startNode == null)
                    {
                        startNode = test;
                    }
                    else
                    {
                        Debug.LogError("2 nodes marked as startnode, save will be corrupted");
                        return;
                    }
                }
            }
        }
        if (startNode == null)
        {
            Debug.LogError("No startnode found, Save Failed");
            return;
        }

        bool endNodeFound = false;
        //Secure that we have atleast one endnode
        for (int i = 0; i < allNodes.Count; i++)
        {
            if (allNodes[i].typeOfNode == Node.NodeType.ScenarioEndNode)
            {
                endNodeFound = true;
                break;
            }
        }
        if (!endNodeFound)
        {
            Debug.LogError("No end node found. Atleast 1 is needed to save");
            return;
        }

        openScenario.startEvent = startNode.myEvent;
        openScenario.startEvent.choices.Clear();

        //Iterative method of saving information to the scenario
        SaveOutputsFromEvent(openScenario.startEvent, startNode as EventNode);


        //Clear old lists and add the active nodes and connections to them instead
        openScenario.editorConnections.Clear();
        openScenario.editorChoiceNodes.Clear();
        openScenario.editorEventNodes.Clear();
        openScenario.editorScenarioEndNodes.Clear();
        openScenario.events.Clear();

        for (int i = 0; i < allNodes.Count; i++)
        {
            if (allNodes[i] is EventNode)
            {
                EventNode node = allNodes[i] as EventNode;
                openScenario.editorEventNodes.Add(node);
            }
            else if (allNodes[i] is ChoiceNode)
            {
                ChoiceNode node = allNodes[i] as ChoiceNode;
                openScenario.editorChoiceNodes.Add(node);
            }
            else if (allNodes[i] is ScenarioEndNode)
            {
                ScenarioEndNode node = allNodes[i] as ScenarioEndNode;
                openScenario.editorScenarioEndNodes.Add(node);
            }
        }
        for (int i = 0; i < allConnections.Count; i++)
        {
            openScenario.editorConnections.Add(allConnections[i]);
        }

        for (int i = 0; i < openScenario.editorEventNodes.Count; i++)
        {
            openScenario.events.Add(openScenario.editorEventNodes[i].myEvent);
        }

        EditorUtility.SetDirty(openScenario);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Save successfull");
    }

    //New method that saves the output from events
    private void SaveOutputsFromEvent(ScenarioEvent inputEvent, EventNode eventNode)
    {
        List<ChoiceNode> newChoiceNodes = new List<ChoiceNode>();

        for (int i = 0; i < allConnections.Count; i++)
        {
            for (int j = 0; j < eventNode.outPoints.Count; j++)
            {
                if (allConnections[i].inPoint.id == eventNode.outPoints[j].id)
                {
                    switch (allConnections[i].outPoint.node.typeOfNode)
                    {
                        case Node.NodeType.ChoiceNode:
                            ChoiceNode newChhoiceNode = allConnections[i].outPoint.node as ChoiceNode;
                            newChoiceNodes.Add(newChhoiceNode);
                            inputEvent.choices.Add(newChhoiceNode.myChoice);
                            break;
                        case Node.NodeType.EventNode:
                            Debug.LogError("EventNode connected to EventNode in Save");
                            break;
                    }
                }
            }
        }

        //Continue save if found outputs
        for (int i = 0; i < newChoiceNodes.Count; i++)
        {
            SaveOutputsFromChoiceNode(inputEvent.choices[i], newChoiceNodes[i]);
        }
    }

    //New method that saves the output from choicenodes
    private void SaveOutputsFromChoiceNode(Choice inputChoice, ChoiceNode choiceNode)
    {
        ScenarioEndNode newEndNode = null;
        EventNode newEventNode = null;
        ScenarioEvent newEvent = null;
        for (int i = 0; i < allConnections.Count; i++)
        {
            for (int j = 0; j < choiceNode.outPoints.Count; j++)
            {
                if (allConnections[i].inPoint.id == choiceNode.outPoints[j].id)
                {
                    switch (allConnections[i].outPoint.node.typeOfNode)
                    {
                        case Node.NodeType.ChoiceNode:
                            Debug.LogError("Choicenode connected to Choicenode in Save");
                            break;
                        case Node.NodeType.EventNode:
                            newEventNode = allConnections[i].outPoint.node as EventNode;
                            newEventNode.myEvent.choices.Clear();
                            newEvent = newEventNode.myEvent;
                            //inputChoice.nextEvent = newEvent;
                            inputChoice.nextEventID = newEvent.id;
                            break;
                        case Node.NodeType.ScenarioEndNode:
                            newEndNode = allConnections[i].outPoint.node as ScenarioEndNode;
                            inputChoice.nextScenario = newEndNode.nextScenario;
                            //inputChoice.nextSceneIndex = newEndNode.nextScene;
                            inputChoice.nextScene = newEndNode.nextScene;
                            break;
                    }
                }
            }
        }

        //Continue save if found output
        if (newEventNode != null && newEvent != null)
        {
            SaveOutputsFromEvent(newEvent, newEventNode);
        }
        else if (newEndNode == null)
        {
            Debug.LogWarning("A choice node exists without any further events or ends, this is not intended and will hardstuck the player. Please check node: " + inputChoice.choiceText);
        }
    }


    //Main function for loading data
    private void LoadScenario()
    {
        allNodes.Clear();
        allConnections.Clear();

        //Load all editor event nodes
        for (int i = 0; i < openScenario.editorEventNodes.Count; i++)
        {
            //allNodes.Add(openScenario.editorEventNodes[i]);
            allNodes.Add(new EventNode(openScenario.editorEventNodes[i].rect.center, nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, OnClickInPoint, OnClickOutPoint, OnClickRemoveNode));
            EventNode loadedNode = allNodes[allNodes.Count - 1] as EventNode;
            loadedNode.LoadEventNode(openScenario.editorEventNodes[i]);
        }

        //Load all editor schoice nodes
        for (int i = 0; i < openScenario.editorChoiceNodes.Count; i++)
        {
            //allNodes.Add(openScenario.editorChoiceNodes[i]);
            allNodes.Add(new ChoiceNode(openScenario.editorChoiceNodes[i].rect.center, nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, OnClickInPoint, OnClickOutPoint, OnClickRemoveNode));
            ChoiceNode loadedNode = allNodes[allNodes.Count - 1] as ChoiceNode;
            loadedNode.LoadChoiceNode(openScenario.editorChoiceNodes[i]);
        }

        //Load all editor scenarioEndNodes
        for (int i = 0; i < openScenario.editorScenarioEndNodes.Count; i++)
        {
            //allNodes.Add(openScenario.editorScenarioEndNodes[i]);
            allNodes.Add(new ScenarioEndNode(openScenario.editorScenarioEndNodes[i].rect.center, nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, OnClickInPoint, OnClickOutPoint, OnClickRemoveNode));
            ScenarioEndNode loadedNode = allNodes[allNodes.Count - 1] as ScenarioEndNode;
            loadedNode.LoadScenarioEndNode(openScenario.editorScenarioEndNodes[i]);
        }

        //Add RemoveMethods to all Nodes
        for (int i = 0; i < allNodes.Count; i++)
        {
            allNodes[i].OnRemoveNode = OnClickRemoveNode;
        }


        //Add all editor connections
        for (int i = 0; i < openScenario.editorConnections.Count; i++)
        {
            allConnections.Add(openScenario.editorConnections[i]);
            //Add method for removing connections
            allConnections[i].OnClickRemoveConnection = OnClickRemoveConnection;
        }


        //Add connectionpoints to editor connections
        for (int i = 0; i < allConnections.Count; i++)
        {
            allConnections[i].OnClickRemoveConnection = OnClickRemoveConnection;
            for (int j = 0; j < allNodes.Count; j++)
            {
                //Setup inpoints
                if (allConnections[i].outPoint.id == allNodes[j].inPoint.id)
                {
                    allConnections[i].outPoint = allNodes[j].inPoint;
                }
                allNodes[j].inPoint.OnClickConnectionPoint = OnClickInPoint;

                //Set outpoints
                for (int k = 0; k < allNodes[j].outPoints.Count; k++)
                {
                    if (allConnections[i].inPoint.id == allNodes[j].outPoints[k].id)
                    {
                        allConnections[i].inPoint = allNodes[j].outPoints[k];
                    }
                    allNodes[j].outPoints[k].OnClickConnectionPoint = OnClickOutPoint;
                }
            }
        }

        GUI.changed = true;
    }
}