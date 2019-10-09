using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FastCalls { NoAnswer, Yes, No, ExampleOne, ExampleTwo, ExampleThree, ExampleFour, WaitingForCall }

public interface IFreezeChoice
{
	void RecieveFastCall(FastCalls f);
}

public class ChoiceFreeze : MonoBehaviour
{
	#region Field
	[SerializeField] float timeFrozen, timeScale;

	int pressedKey = 0;
	float timer;
	FastCalls[] calls;
	IFreezeChoice caller;
	Dictionary<FastCalls, string> FastCallMessage;

	static ChoiceFreeze inst;

	public static ChoiceFreeze instance
	{
		get { return inst; }
	}
	#endregion

	#region Private methods


	private void Start()
	{
		if (inst != null)
		{
			Destroy(gameObject);
		}
		inst = this;

		FastCallMessage.Add(FastCalls.Yes, "Yes");
		FastCallMessage.Add(FastCalls.No, "No");
		FastCallMessage.Add(FastCalls.ExampleOne, "Send backup");
		FastCallMessage.Add(FastCalls.ExampleTwo, "Set up trap");
		FastCallMessage.Add(FastCalls.ExampleThree, "Hire hitman");
		FastCallMessage.Add(FastCalls.ExampleFour, "Change radio station");
	}


	private void LateUpdate()
	{
		if (timer > 0)
		{
			getInputs();

			if(pressedKey != 0)
			{
				caller.RecieveFastCall(calls[pressedKey-1]);
				EndOptions();
				return;
			}

			timer -= Time.unscaledDeltaTime;
			if(timer <= 0)
			{
				EndOptions();
				caller.RecieveFastCall(FastCalls.NoAnswer);
			}
		}
	}

	private void getInputs()
	{
		
		if (Input.anyKey)
		{
			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				pressedKey = 1;
			}
			else if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				pressedKey = 2;
			}
			else if (Input.GetKeyDown(KeyCode.Alpha3))
			{
				pressedKey = 3;
			}
			else if (Input.GetKeyDown(KeyCode.Alpha4))
			{
				pressedKey = 4;
			}

			if (pressedKey == 0 || pressedKey > calls.Length)
			{
				pressedKey = 0;
				return;
			}
		}
	}

	private void StartOptions()
	{
		for (int i = 0; i < calls.Length; i++)
		{

		}
	}

	private void EndOptions()
	{
		Time.timeScale = 1;
		timer = 0;
		pressedKey = 0;
	}

	#endregion

	#region Public Methods
	
	//Starts the fast call screen with only yes or no answers 
	public void FreezeCall(IFreezeChoice caller)
	{
		FastCalls[] _tempArr = new FastCalls[] { FastCalls.Yes, FastCalls.No };
		FreezeCall(_tempArr, caller);
	}

	//Starts the fast call screen with custom answers.
	public void FreezeCall(FastCalls[] callsOptions, IFreezeChoice caller)
	{
		if(callsOptions.Length > 4)
		{
			callsOptions = new FastCalls[] { callsOptions[0], callsOptions[1], callsOptions[2], callsOptions[3] };
			Debug.LogError("<color=red> TOO MANY FASTCALLS, ARRAY IS NOW CUT DOWN TO FOUR ELEMENTS </color>");
		}

		calls = callsOptions;
		this.caller = caller;
		timer = timeFrozen;
		Time.timeScale = timeScale;
	}
	#endregion
}


