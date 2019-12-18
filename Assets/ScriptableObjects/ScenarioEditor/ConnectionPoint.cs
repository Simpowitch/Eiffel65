using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ConnectionPointType { In, Out }

#if UNITY_EDITOR

//Simon Voss
[System.Serializable]
public class ConnectionPoint
{
    public static List<double> usedIDs = new List<double>();

    public double id;
    public Rect rect;

    public ConnectionPointType type;

    public Node node;

    public GUIStyle style;

    public float YOffset;

    public Action<ConnectionPoint> OnClickConnectionPoint;

    public static double GetUniqueID()
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

    public ConnectionPoint(Node node, ConnectionPointType type, GUIStyle style, Action<ConnectionPoint> OnClickConnectionPoint, float YOffset)
    {
        id = GetUniqueID();
        this.node = node;
        this.type = type;
        this.style = style;
        this.YOffset = YOffset;
        this.OnClickConnectionPoint = OnClickConnectionPoint;
        rect = new Rect(0, 0, 10f, 20f);
    }

    public void Draw(Node _node)
    {
        node = _node;
        rect.y = node.rect.y + (node.rect.height * 0.5f) - rect.height * 0.5f;

        switch (type)
        {
            case ConnectionPointType.In:
                rect.x = node.rect.x - rect.width + 8f;
                break;

            case ConnectionPointType.Out:
                rect.x = node.rect.x + node.rect.width - 8f;
                rect.y = node.rect.y + YOffset;
                break;
        }

        if (GUI.Button(rect, "", style))
        {
            if (OnClickConnectionPoint != null)
            {
                OnClickConnectionPoint(this);
            }
        }
    }
}
#endif