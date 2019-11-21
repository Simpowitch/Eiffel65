using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
	//Index 0 in sources is reserved for exclusive clips such as background music.
	List<AudioSource> sources;
	List<AudioSource> worldPositionSources;
	List<MovingAudioSource> movingAudioSources;

	private static AudioManager inst;


	public static AudioManager Instance
	{
		get { return inst; }
	}

	#region Private Methods
	void Awake()
	{
		if (inst != null)
			Destroy(gameObject);
		inst = this;
		sources = new List<AudioSource>(32);
		worldPositionSources = new List<AudioSource>(32);
		movingAudioSources = new List<MovingAudioSource>(32);
		sources.Add(gameObject.AddComponent<AudioSource>());
		sources[0].loop = true;
	}

	private void Update()
	{

		foreach (MovingAudioSource source in movingAudioSources)
		{
			if (source.audioSource.isPlaying)
			{
				source.transform.position = source.objectToFollow.position;
			}
			else if(source.audioSource.loop)
			{
				source.audioSource.loop = false;
			}
		}
	}
	#endregion

	#region Public Methods

	#region 2D sound
	/// <summary>
	/// play an AudioClip in an empty AudioSource. (Ignores top AudioSource), also returns the chosen AudioSource
	/// </summary>
	/// <param name="clip"></param>
	public AudioSource PlayClip(AudioClip clip)
	{
		for (int i = 1; i < sources.Count; i++)
		{
			if (!sources[i].isPlaying)
			{
				sources[i].clip = clip;
				sources[i].Play();
				return sources[i];
			}
		}
		AudioSource _source = gameObject.AddComponent<AudioSource>();
		sources.Add(_source);
		_source.clip = clip;
		_source.Play();
		return _source;
	}

	/// <summary>
	/// Play 'playclip' if 'otherClip' isn't currently playing (Ignores top AudioSource), also returns the chosen AudioSource
	/// </summary>
	/// <param name="playClip"></param>
	/// <param name="otherClip"></param>
	public AudioSource PlayClipIfNotPlaying(AudioClip playClip, AudioClip otherClip)
	{
		AudioSource _source = null;
		for (int i = 1; i < sources.Count; i++)
		{
			if (sources[i].isPlaying && sources[i].clip == otherClip)
			{
				return sources[i];
			}
			if (!sources[i].isPlaying && _source != null)
			{
				_source = sources[i];
			}
		}

		if (_source == null)
			_source = gameObject.AddComponent<AudioSource>();
		sources.Add(_source);
		_source.clip = playClip;
		_source.Play();
		return _source;
	}

	/// <summary>
	/// Changes the clip of the first AudioSource (slot for exclusive sounds like music), returns the used AudioSource (this audiosource is looping)
	/// </summary>
	/// <param name="clip"></param>
	public AudioSource SetBackgroundMusic(AudioClip clip)
	{
		sources[0].clip = clip;
		sources[0].Play();
		return sources[0];
	}

	public void PlayClip(AudioClip clip, int index)
	{
		if (sources.Count < index)
		{
			sources[index].clip = clip;
			sources[index].Play();
		}
	}

	#endregion

	#region 3D sound

	/// <summary>
	/// Play a sound in worldspace
	/// </summary>
	/// <param name="clip"></param>
	/// <param name="sourceOfAudio"></param>
	/// <returns></returns>
	public AudioSource PlayClip(AudioClip clip, Vector3 sourceOfAudio)
	{
		for (int i = 1; i < worldPositionSources.Count; i++)
		{
			if (!worldPositionSources[i].isPlaying)
			{
				worldPositionSources[i].transform.position = sourceOfAudio;
				worldPositionSources[i].clip = clip;
				worldPositionSources[i].Play();
				return worldPositionSources[i];
			}
		}
		GameObject temp = new GameObject();
		temp.transform.position = sourceOfAudio;
		AudioSource _source = temp.AddComponent<AudioSource>();
		worldPositionSources.Add(_source);
		_source.clip = clip;
		_source.spatialBlend = 1;
		_source.Play();
		return _source;
	}

	/// <summary>
	/// Play a sound in worldspace
	/// </summary>
	/// <param name="clip"></param>
	/// <param name="sourceOfAudio"></param>
	/// <returns></returns>
	public AudioSource PlayClip(AudioClip clip, Vector3 sourceOfAudio, bool loop)
	{
		if (!loop)
			PlayClip(clip, sourceOfAudio);
		for (int i = 1; i < worldPositionSources.Count; i++)
		{
			if (!worldPositionSources[i].isPlaying)
			{
				worldPositionSources[i].transform.position = sourceOfAudio;
				worldPositionSources[i].clip = clip;
				worldPositionSources[i].Play();
				return worldPositionSources[i];
			}
		}
		GameObject temp = new GameObject();
		temp.transform.position = sourceOfAudio;
		AudioSource _source = temp.AddComponent<AudioSource>();
		worldPositionSources.Add(_source);
		_source.clip = clip;
		_source.spatialBlend = 1;
		_source.loop = loop;
		_source.Play();
		return _source;
	}

	/// <summary>
	/// Play a sound that follows the given transform.
	/// </summary>
	/// <param name="clip"></param>
	/// <param name="fromObject"></param>
	/// <returns></returns>
	public AudioSource PlayClip(AudioClip clip, Transform fromObject)
	{
		for (int i = 1; i < worldPositionSources.Count; i++)
		{
			if (!worldPositionSources[i].isPlaying)
			{
				worldPositionSources[i].clip = clip;
				worldPositionSources[i].Play();
				return worldPositionSources[i];
			}
		}
		GameObject temp = new GameObject();
		AudioSource _source = temp.AddComponent<AudioSource>();
		MovingAudioSource _movingAudioSource = new MovingAudioSource(_source, temp.transform, fromObject);
		movingAudioSources.Add(_movingAudioSource);
		_source.clip = clip;
		_source.spatialBlend = 1;
		_source.Play();
		return _source;
	}

	#endregion


	#endregion
}

public class MovingAudioSource
{
	public AudioSource audioSource;
	public Transform transform;
	public Transform objectToFollow;

	public MovingAudioSource(AudioSource source, Transform myTransform, Transform objectToFollow)
	{
		audioSource = source;
		transform = myTransform;
		this.objectToFollow = objectToFollow;
	}
}
