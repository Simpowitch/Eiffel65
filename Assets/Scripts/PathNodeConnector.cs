using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;

[ExecuteInEditMode]
public class PathNodeConnector : MonoBehaviour, IPointerDownHandler
{
    //Currently not working at all
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("Test");
    }

    private void OnMouseDown()
    {
        Debug.Log("Test");
    }
}
