using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianTrafficLight : MonoBehaviour
{
    public bool greenLight = true;


    Color allowedToPassColor = Color.green;
    Color notAllowedToPassColor = Color.red;
    Vector3 cube = new Vector3(2, 3, 2);
    private void OnDrawGizmos()
    {
        //Draw sphere
        Gizmos.color = greenLight ? allowedToPassColor : notAllowedToPassColor;
        Gizmos.DrawWireCube(this.transform.position, cube);
    }
}
