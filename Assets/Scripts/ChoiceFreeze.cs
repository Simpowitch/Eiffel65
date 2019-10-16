using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public enum FastCalls { NoAnswer, OptionOne, OptionTwo, OptionThree, OptionFour, WaitingForCall }

public interface IFreezeChoice
{
	void RecieveFastCall(FastCalls f);
}

public class ChoiceFreeze : MonoBehaviour
{
	#region Field
	[SerializeField] float timeFrozen, timeScale;

	int pressedKey = 0;
	float timer, timeFrozenInv;
	FastCalls[] calls;
	IFreezeChoice caller;

	static FastCalls[] callOptions = new FastCalls[] { FastCalls.OptionOne, FastCalls.OptionTwo, FastCalls.OptionThree, FastCalls.OptionFour };
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
		timeFrozenInv = 1 / timeFrozen;
	}


	private void LateUpdate()
	{
		print(GameManager.instance.FillBarAmount);

		if (timer > 0)
		{
			getInputs();
			GameManager.instance.FillBarAmount = timer * timeFrozenInv;
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

	private void EndOptions()
	{
		Time.timeScale = 1;
		timer = 0;
		pressedKey = 0;
		GameManager.instance.DisplayFastChoice(false);
	}

	#endregion

	#region Public Methods

	/// <summary>
	///Starts the fast call screen with custom answers
	///</summary>
	public void FreezeCall(string[] callsOptions, IFreezeChoice caller)
	{
		if(callsOptions.Length > 4)
		{
			callsOptions = new string[] { callsOptions[0], callsOptions[1], callsOptions[2], callsOptions[3] };
			Debug.LogWarning("<color=red> TOO MANY FASTCALLS, ARRAY IS NOW CUT DOWN TO FOUR ELEMENTS </color>");
		}

		calls = new FastCalls[callsOptions.Length];

		for (int i = 0; i < calls.Length; i++)
		{
			calls[i] = callOptions[i];
		}
		
		this.caller = caller;
		timer = timeFrozen;
		Time.timeScale = timeScale;

		GameManager.instance.DisplayFastChoice(true, callsOptions);
	}
	#endregion
}


