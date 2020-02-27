using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public enum GameState {Playing, Unpausable ,Paused, Menu }

public class GameManager : MonoBehaviour
{
	[SerializeField] private GameObject[] menus = null; // 0 = FastChoice (for now)
	[SerializeField] private TextMeshProUGUI[] promptSpaces = null;
	[SerializeField] private Image fillBar = null;

	GameState state;
	float currentTimeScale;

	ChoiceFreeze cf;

	static GameManager inst;

	#region Properties

	public float FillBarAmount
	{
		get { return fillBar.fillAmount; }
		set { fillBar.fillAmount = value; }
	}

	public ChoiceFreeze choiceFreeze
	{
		get { return cf; }
	}

	public GameState gameState
	{
		get { return state; }
		set { OnStateExit(state, value);OnStateEnter(state, value); }
	}

	public float timeScale
	{
		get { return currentTimeScale; }
		set { currentTimeScale = value; Time.timeScale = currentTimeScale; }
	}

	public static GameManager instance
	{
		get { return inst; }
	}


	#endregion

	#region Private Methods
	// Start is called before the first frame update
	private void Awake()
    {
		if (instance == null || instance == this)
			inst = this;
		else
			Destroy(gameObject);
		cf = GetComponent<ChoiceFreeze>();
    }

	private void OnStateEnter(GameState from, GameState to)
	{
		switch (to)
		{
			case GameState.Menu:
				break;
			case GameState.Paused:
				if(from == GameState.Unpausable)
				{
					return;
				}
				Time.timeScale = 0;
				break;
			case GameState.Playing:
				Time.timeScale = currentTimeScale;
				break;
			case GameState.Unpausable:
				
				break;


		}
		state = to;
	}

	private void OnStateExit(GameState from, GameState to)
	{
		switch (to)
		{
			case GameState.Menu:
				break;
			case GameState.Paused:
				if (from == GameState.Unpausable)
				{
					return;
				}
				break;
			case GameState.Playing:
				break;
			case GameState.Unpausable:

				break;


		}
	}

	#endregion

	#region Public Methods

	public void DisplayFastChoice(bool enabled)
	{
		menus[0].SetActive(enabled);
	}

	public void DisplayFastChoice(bool enabled, FastCall[] prompts)
	{
		//Resets the prompts on the canvas
		foreach(TextMeshProUGUI t in promptSpaces)
		{
			t.text = "";
		}

		menus[0].SetActive(enabled);

		for (int i = 0; i < prompts.Length; i++)
		{
			promptSpaces[i].text = (i+1) + ". " + prompts[i].callText;
		}
	}

	public void ChangeScene(int index)
	{
		SceneManager.LoadScene(0);
	}

	public void ChangeScene(string sceneName)
	{
		SceneManager.LoadScene(sceneName);
	}

	public void ReloadScene()
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

	#endregion
}
