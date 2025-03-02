// AudioManager.cs - Simple audio manager for handling sound effects and music
using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
    }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Sound Effects")]
    public List<Sound> sounds = new List<Sound>();

    [Header("Music")]
    public List<Sound> music = new List<Sound>();

    private Dictionary<string, Sound> soundDictionary = new Dictionary<string, Sound>();
    private Dictionary<string, Sound> musicDictionary = new Dictionary<string, Sound>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Fill dictionaries for quick access
        foreach (Sound s in sounds)
        {
            soundDictionary[s.name] = s;
        }

        foreach (Sound m in music)
        {
            musicDictionary[m.name] = m;
        }
    }

    public void PlaySound(string name)
    {
        if (soundDictionary.TryGetValue(name, out Sound sound))
        {
            sfxSource.pitch = sound.pitch;
            sfxSource.volume = sound.volume;
            sfxSource.PlayOneShot(sound.clip);
        }
        else
        {
            Debug.LogWarning("Sound " + name + " not found!");
        }
    }

    public void PlayMusic(string name)
    {
        if (musicDictionary.TryGetValue(name, out Sound music))
        {
            musicSource.clip = music.clip;
            musicSource.volume = music.volume;
            musicSource.pitch = music.pitch;
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning("Music " + name + " not found!");
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        musicSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }
}