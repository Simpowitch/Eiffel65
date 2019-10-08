using System;
using UnityEngine;

public class DriftCamera : MonoBehaviour
{
    enum CameraPosition { Normal, Inside}

    [Serializable]
    public class AdvancedOptions
    {
        public bool updateCameraInUpdate;
        public bool updateCameraInFixedUpdate = true;
        public bool updateCameraInLateUpdate;
    }

    public float defaultSmoothing = 6f;
    float smoothing = 6f;

    public AdvancedOptions advancedOptions;

    public Transform carToFollow;
    private Transform camRig;
    private Transform lookAtTarget;
    private Transform camPositionParent;

    private int currentCam = 0;
    private VehicleCamera[] cameras;

    private void Start()
    {
        camRig = carToFollow.Find("CamRig");
        lookAtTarget = camRig.GetChild(0);
        camPositionParent = camRig.GetChild(1);
        cameras = camPositionParent.GetComponentsInChildren<VehicleCamera>();
        GetComponent<GraphOverlay>().vehicleBody = carToFollow.GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (advancedOptions.updateCameraInFixedUpdate)
            UpdateCamera();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            currentCam++;
            currentCam = currentCam == cameras.Length ? 0 : currentCam;
        }

        if (advancedOptions.updateCameraInUpdate)
            UpdateCamera();
    }

    private void LateUpdate()
    {
        if (advancedOptions.updateCameraInLateUpdate)
            UpdateCamera();
    }

    private void UpdateCamera()
    {
        smoothing = cameras[currentCam].cameraType == VechicleCameraType.Interior ? 100 : defaultSmoothing;

        transform.position = Vector3.Lerp(transform.position, cameras[currentCam].transform.position, Time.deltaTime * smoothing);

        transform.LookAt(lookAtTarget);
    }
}
