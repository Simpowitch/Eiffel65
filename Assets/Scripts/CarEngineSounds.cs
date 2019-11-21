using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEngineSounds : MonoBehaviour
{
	private Object[] audioFiles;
	[SerializeField] private AudioClip[] engineSounds;
	private WheelDrive wheels;
	private int gear, previousGear;
	private float inverse;

    // Start is called before the first frame update
    void Start()
    {
		audioFiles = Resources.LoadAll("Audio/Engine_Loops", typeof(AudioClip));
		engineSounds = new AudioClip[audioFiles.Length];
		for (int i = 0; i < audioFiles.Length; i++)
		{
			engineSounds[i] = (AudioClip) audioFiles[i];
		}

		wheels = GetComponent<WheelDrive>();
		previousGear = -1;
		inverse = 1/(wheels.MaxTorque / engineSounds.Length); //Calculating the inverse at start to avoid division in update.
    }

    // Update is called once per frame
    void Update()
    {
		gear = Mathf.FloorToInt(wheels.EngineTorque * inverse);

        if(gear != previousGear)
		{
			gear = gear == engineSounds.Length ? engineSounds.Length-1 : gear;
			AudioManager.Instance.PlayClip(engineSounds[gear], transform);
		}
		previousGear = gear;
    }
}
