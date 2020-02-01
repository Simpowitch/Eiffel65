using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    [SerializeField] Transform playerCar = null;
    [SerializeField] RectTransform minimap = null;


    public float mapXSize = 1000;
    public float mapZSize = 1000;


    

    private void Update()
    {
        SetMinimapRotationAndPosition();

        
    }

    private void SetMinimapRotationAndPosition()
    {
        float playerXPercentage = playerCar.position.x / mapXSize;
        float playerYPercentage = playerCar.position.z / mapZSize;

        minimap.pivot = new Vector2(playerXPercentage, playerYPercentage);

        minimap.eulerAngles = new Vector3(0, 0, playerCar.eulerAngles.y);
    }
}
