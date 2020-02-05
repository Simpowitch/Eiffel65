using UnityEngine;
using System;

[Serializable]
public enum DriveType
{
    RearWheelDrive,
    FrontWheelDrive,
    AllWheelDrive
}

public class WheelDrive : MonoBehaviour
{
    public enum DriveControls { AI, Player };
    public DriveControls pilotedBy;

    [Tooltip("Maximum steering angle of the wheels")]
    public float maxAngle = 30f;
    [Tooltip("Maximum torque applied to the driving wheels")]
    public float maxTorque = 300f;
    [Tooltip("Maximum brake torque applied to the driving wheels")]
    public float brakeTorque = 30000f;
    [Tooltip("If you need the visual wheels to be attached automatically, drag the wheel shape here.")]
    public GameObject wheelShape;

    [Tooltip("The vehicle's speed when the physics engine can use different amount of sub-steps (in m/s).")]
    public float criticalSpeed = 5f;
    [Tooltip("Simulation sub-steps when the speed is above critical.")]
    public int stepsBelow = 5;
    [Tooltip("Simulation sub-steps when the speed is below critical.")]
    public int stepsAbove = 1;

    [Tooltip("The vehicle's drive type: rear-wheels drive, front-wheels drive or all-wheels drive.")]
    public DriveType driveType;

    LightRig lightRig = null;

    private WheelCollider[] m_Wheels;

    private float speed;

    //How quickly the wheels turn into position
    [SerializeField] float wheelChangeSpeed = 10f;


    // Find all the WheelColliders down in the hierarchy.
    void Start()
    {
        lightRig = GetComponentInChildren<LightRig>(); 

        m_Wheels = GetComponentsInChildren<WheelCollider>();

        for (int i = 0; i < m_Wheels.Length; ++i)
        {
            var wheel = m_Wheels[i];

            // Create wheel shapes only when needed.
            if (wheelShape != null)
            {
                var ws = Instantiate(wheelShape);
                ws.transform.parent = wheel.transform;
            }
        }
        normalGrip = m_Wheels[0].sidewaysFriction.extremumSlip;
    }



    float steeringAngle;
    float engineTorque;
    float brakingTorque;
    float targetSteerAngleAI;

    float normalGrip;
    float slideGrip = 1;

	public float EngineTorque
	{
		get { return engineTorque; }
	}
	public float MaxTorque
	{
		get { return maxTorque; }
	}
	public float Speed
	{
		get { return speed; }
	}

    public void AIDriver(float steering, float torque, bool braking)
    {
        targetSteerAngleAI = steering * maxAngle;
        engineTorque = torque * maxTorque;
        brakingTorque = braking ? brakeTorque : 0;
    }

    private void LerpToSteeringTarget()
    {
        steeringAngle = Mathf.Lerp(steeringAngle, targetSteerAngleAI, Time.fixedDeltaTime * wheelChangeSpeed);
        if (float.IsNaN(steeringAngle))
        {
            steeringAngle = 0;
            Debug.LogWarning("Value was NaN");
        }
    }

    private void FixedUpdate()
    {
        LerpToSteeringTarget();
    }


    // This is a really simple approach to updating wheels.
    // We simulate a rear wheel drive car and assume that the car is perfectly symmetric at local zero.
    // This helps us to figure our which wheels are front ones and which are rear.
    void Update()
    {
        speed = GetComponent<Rigidbody>().velocity.magnitude * 3.6f;

        m_Wheels[0].ConfigureVehicleSubsteps(criticalSpeed, stepsBelow, stepsAbove);

        if (pilotedBy == DriveControls.Player)
        {
            steeringAngle = maxAngle * Input.GetAxis("Horizontal");
            engineTorque = maxTorque * Input.GetAxis("Vertical");

            if (engineTorque < 0 && speed > 15)
            {
                brakingTorque = brakeTorque;
            }
            else
            {
                brakingTorque = Input.GetKey(KeyCode.Space) ? brakeTorque : 0;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                foreach (var item in m_Wheels)
                {
                    WheelFrictionCurve curve = item.sidewaysFriction;
                    curve.extremumSlip = slideGrip;
                    item.sidewaysFriction = curve;
                }
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                foreach (var item in m_Wheels)
                {
                    WheelFrictionCurve curve = item.sidewaysFriction;
                    curve.extremumSlip = normalGrip;
                    item.sidewaysFriction = curve;
                }
            }

            lightRig.SetLightGroup(brakingTorque > 0, LightGroup.BrakeLights);
        }
        foreach (WheelCollider wheel in m_Wheels)
        {
            // A simple car where front wheels steer while rear ones drive.
            if (wheel.transform.localPosition.z > 0)
                wheel.steerAngle = steeringAngle;

            //if (wheel.transform.localPosition.z < 0)
            //{
            wheel.brakeTorque = brakingTorque;
            //}

            if (wheel.transform.localPosition.z < 0 && driveType != DriveType.FrontWheelDrive)
            {
                wheel.motorTorque = engineTorque;
            }

            if (wheel.transform.localPosition.z >= 0 && driveType != DriveType.RearWheelDrive)
            {
                wheel.motorTorque = engineTorque;
            }

            // Update visual wheels if any.
            if (wheelShape)
            {
                Quaternion q;
                Vector3 p;
                wheel.GetWorldPose(out p, out q);

                // Assume that the only child of the wheelcollider is the wheel shape.
                Transform shapeTransform = wheel.transform.GetChild(0);

                if (wheel.name == "a0l" || wheel.name == "a1l" || wheel.name == "a2l")
                {
                    shapeTransform.rotation = q * Quaternion.Euler(0, 180, 0);
                    shapeTransform.position = p;
                }
                else
                {
                    shapeTransform.position = p;
                    shapeTransform.rotation = q;
                }
            }
        }
    }
}
