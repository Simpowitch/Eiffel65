using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianTrafficLight : MonoBehaviour
{
    public bool greenLight = true;


    Color allowedToPassColor = Color.green;
    Color notAllowedToPassColor = Color.red;
    private void OnDrawGizmos()
    {
        //Draw cube
        Vector3 cube = transform.localScale;

        Gizmos.color = greenLight ? allowedToPassColor : notAllowedToPassColor;
        Gizmos.DrawWireCube(this.transform.position, cube);
    }
}
