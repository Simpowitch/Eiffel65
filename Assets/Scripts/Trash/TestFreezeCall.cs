using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestFreezeCall : MonoBehaviour, IFreezeChoice
{
	FastCalls call;


    // Start is called before the first frame update
    void Start()
    {
		call = FastCalls.WaitingForCall;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
		{
			string[] _exampleCalls = new string[] { "Throw flashbang", "Call for backup", "Run licenceplate through database" , "Try shooting the criminal [DANGEROUS]" }; //List can not be longer than 4
			ChoiceFreeze.instance.FreezeCall(_exampleCalls, this);
		}

		if(call != FastCalls.WaitingForCall)
		{
			print("The chosen option was: <color=red>" + call.ToString() + "</color>");
			call = FastCalls.WaitingForCall;
		}
    }

	public void RecieveFastCall(FastCalls call)
	{
		this.call = call;
	}

}
