using UnityEngine;

public class PathNodeSystem : MonoBehaviour
{
    PathNode[] allNodes;

    private void GetAllNodes()
    {
        allNodes = GetComponentsInChildren<PathNode>();
    }

    public void ValidateRoadNetwork()
    {
        GetAllNodes();

        foreach (var item in allNodes)
        {
            item.AnalyzeAndValidate();
        }

        //Double check
        foreach (var item in allNodes)
        {
            item.AnalyzeAndValidate();
        }
    }
}
