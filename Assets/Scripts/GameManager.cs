using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	[SerializeField] private GameObject[] menus; // 0 = FastChoice (for now)
	[SerializeField] private Text[] promptSpaces;
	[SerializeField] private Image fillBar;



	static GameManager inst;

	public float FillBarAmount
	{
		get { return fillBar.fillAmount; }
		set { fillBar.fillAmount = value; }
	}


	public static GameManager instance
	{
		get { return inst; }
	}


    // Start is called before the first frame update
    private void Start()
    {
		if (instance == null || instance == this)
			inst = this;
		else
			Destroy(gameObject);
    }

	public void DisplayFastChoice(bool enabled)
	{
		menus[0].SetActive(enabled);
	}

	public void DisplayFastChoice(bool enabled, string[] prompts)
	{
		//Resets the prompts on the canvas
		foreach(Text t in promptSpaces)
		{
			t.text = "";
		}

		menus[0].SetActive(enabled);

		for (int i = 0; i < prompts.Length; i++)
		{
			promptSpaces[i].text = i + ". " + prompts[i];
		}
	}
}
