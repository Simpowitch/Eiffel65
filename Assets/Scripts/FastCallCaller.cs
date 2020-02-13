using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Consequense { NoConsequenses, Demoted, Fired, Praised, Promoted, Rekt } //Most of these are examples
public delegate void consequense();

public class FastCallCaller : MonoBehaviour, IFreezeChoice
{
	[SerializeField] AudioClip[] clips = null;
	[SerializeField] private FastCall[] Consequences = new FastCall[4];
	[SerializeField] private Consequense NoAnswer = Consequense.NoConsequenses;
	[SerializeField] private WheelDrive.DriveControls triggerdBy = WheelDrive.DriveControls.Player;


	Consequense[] consequenseEnums;
	consequense[] consequenseMethods;
	Dictionary<Consequense, consequense> ConsDic;

	// Start is called before the first frame update
	void Start()
	{
		consequenseEnums = new Consequense[] { Consequense.NoConsequenses, Consequense.Demoted, Consequense.Fired, Consequense.Praised, Consequense.Promoted, Consequense.Rekt };
		consequenseMethods = new consequense[] { NoConsequenses, Demoted, Fired, Praised, Promoted, Rekt };
		ConsDic = new Dictionary<Consequense, consequense>();

		for (int i = 0; i < consequenseMethods.Length; i++)
		{
			ConsDic.Add(consequenseEnums[i], consequenseMethods[i]);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Vehicle")
		{
			
			WheelDrive _vehicle = other.transform.parent.GetComponent<WheelDrive>();
			print(other.name);
			if (_vehicle != null && _vehicle.pilotedBy == triggerdBy)
			{
				print("HERE I AM2");
				GameManager.instance.choiceFreeze.FreezeCall(Consequences, this);
			}

		}
	}

	public void RecieveFastCall(int call)
	{
		consequense _fastCallCons;

		if (call > 0 && ConsDic.TryGetValue(Consequences[call - 1].consequense, out _fastCallCons))
		{
			_fastCallCons();
		}
		else if (call == 0 && ConsDic.TryGetValue(NoAnswer, out _fastCallCons))
		{
			_fastCallCons();
		}
		else
		{
			Debug.LogWarning("Make sure that 'call' is in the range of 0 to 4, it is: " + call);
			Debug.LogError("Make sure that the consequence " + Consequences[call] + " exists");
		}

	}

	#region Delegate Methods

	private void NoConsequenses()
	{
		print("Your action led to no consequenses");
	}

	private void Demoted()
	{
		print("Your're demoted :(");
	}

	private void Fired()
	{
		print("Police no more :( You're <color=red>FIRED </color>");
		AudioManager.Instance.PlayClip(clips[0]);
	}

	private void Praised()
	{
		print("Local hero!");
	}

	private void Promoted()
	{
		print("Time to buy youself that new plasma-TV :D You're <color=green>PROMOTED </color>");
	}

	private void Rekt()
	{
		print("<color=black>#</color><color=red>R</color><color=green>E</color><color=blue>K</color><color=pink>T</color>");
	}

	#endregion
}

[System.Serializable]
public struct FastCall
{
	public string callText;
	public Consequense consequense;
}
