using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathNodeNetwork : MonoBehaviour
{
    public List<PathNode> pathNodes = new List<PathNode>();
    public List<DirectionChoice> directionChoices = new List<DirectionChoice>();

    public void SavePathnodesAndConnections()
    {
        pathNodes.Clear();
        directionChoices.Clear();
        PathNode[] newNodes = GetComponentsInChildren<PathNode>();

        foreach (var pathNode in newNodes)
        {
            pathNodes.Add(pathNode);
            foreach (var directionChoice in pathNode.GetOutChoices())
            {
                directionChoices.Add(directionChoice);
            }
        }

        foreach (var item in pathNodes)
        {
            item.ValidateConnections();
            item.ValidateSetup();
        }

        Debug.Log("Network saved");
    }
}
