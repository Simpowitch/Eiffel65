using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{
    public Transform objectToFollow = null;

    float height = 50;
    float offset = 60;

    // Update is called once per frame
    void Update()
    {
        if (objectToFollow)
        {
            this.transform.position = objectToFollow.transform.position + new Vector3(0, height, 0) + objectToFollow.transform.forward * offset;
            this.transform.eulerAngles = new Vector3(90, 0, -objectToFollow.eulerAngles.y);
        }
    }
}
