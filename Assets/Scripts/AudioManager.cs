using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
	List<AudioSource> sources;

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
		sources = new List<AudioSource>(64);
		sources.Add(gameObject.AddComponent<AudioSource>());
	}
	#endregion

	#region Public Methods
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
	/// Changes the clip of the first AudioSource (slot for exclusive sounds like music), returns the used AudioSource
	/// </summary>
	/// <param name="clip"></param>
	public AudioSource PlayTopAudioSource(AudioClip clip)
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
}
