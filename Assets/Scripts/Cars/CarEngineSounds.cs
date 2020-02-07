using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEngineSounds : MonoBehaviour
{
	private Object[] audioFiles;
	private float[] speedThresholds = {int.MinValue, 1, 10, 30, 50, 70, 90, 100, 120, 150, 175, 200 };

	[SerializeField] private AudioClip[] engineSounds;
	private AudioSource previousAudioSource;
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
    private void Update()
    {
		gear = 11;
		for (int i = 0; i < engineSounds.Length-1; i++)
		{
			if (IsInRange(wheels.Speed * Input.GetAxis("Vertical"), speedThresholds[i], speedThresholds[i+1]))
			{
				gear = i;
			}
		}
        if(gear != previousGear)
		{
			if(previousAudioSource != null && previousAudioSource.isPlaying)
			{
				previousAudioSource.Stop();
			}
			previousAudioSource = AudioManager.Instance.PlayClip(engineSounds[gear], transform, true);
		}
		previousGear = gear;
		print(gear);
    }

	/// <summary>
	/// Checks if number is between the two given numbers, where 'over' is inclusive and 'under' is exclusive
	/// </summary>
	/// <param name="over"></param>
	/// <param name="under"></param>
	/// <param name="number"></param>
	/// <returns></returns>
	private bool IsInRange(float number, float over, float under)
	{
		if (number >= over && number < under)
			return true;		
		return false;
	}
}
