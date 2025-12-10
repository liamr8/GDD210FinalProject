using UnityEngine;
using System;
using UnityEngine.Assertions.Must;
using System.Collections;
using UnityEngine.Audio;
using UnityEngine.UI;
public class AudioManager : MonoBehaviour
{
    public AudioMixer mainAudioMixer;

    public AudioSource musicSourceA;
    public AudioSource musicSourceB;

    
    [SerializeField] private AudioSource currentMusicSource;
    [Header("Current Playback Information")]
    [SerializeField]float songPlaybackTime;
    [SerializeField]float songSpeed;
    [SerializeField] AudioClip currentSong;
    [Header("Minigame Songs")]
    public AudioClip matchMinigame;
    public AudioClip heightsMinigame;
    public AudioClip spiderMinigame;
    public AudioClip defaultTrack; //placeholder track if the current minigame is missing a valid song
    [Header("Other Songs")]
    public AudioClip bedroomMusic;
    public AudioClip menuMusic;
    [Header("SFX")]
    public AudioSource alarmClockSource;
    public AudioClip[] alarmClockWinLoseSFX;

    bool managersFound = false;
    bool eventHandlersSubscribed = false;

    private MinigameManager _minigameManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.OnGameStateChanged += ManageGameStateChangedForAudio;
        GameManager.Instance.RegisterService(this);
        ManageServices();
        currentMusicSource = musicSourceA;
        if(managersFound)
        {
            ManageSongPlayback();
            MinigameManager.OnMinigameWon += ManageAudioOnMinigameWon;
            MinigameManager.OnMinigameLost += ManageAudioOnMinigameLost;
            MinigameManager.OnMinigameDestroyed += ManageAudioOnMinigameDestroyed;
            MinigameManager.OnMinigameStarted += ManageAudioOnMinigameStart;
            eventHandlersSubscribed = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!managersFound)
        {
            ManageServices();
        }
        else if(!eventHandlersSubscribed)
        {
            MinigameManager.OnMinigameWon += ManageAudioOnMinigameWon;
            MinigameManager.OnMinigameLost += ManageAudioOnMinigameLost;
            MinigameManager.OnMinigameDestroyed += ManageAudioOnMinigameDestroyed;
            MinigameManager.OnMinigameStarted += ManageAudioOnMinigameStart;
            eventHandlersSubscribed = true;
        }
        ManageSongPlayback();
        //Debug.LogError("Live audio source time: " + currentMusicSource.time +"\nSaved playback time: " + songPlaybackTime);
        //Debug.Log("musicSourceA is playing: "+ musicSourceA.isPlaying+"\nmusicSourceB is playing: " + musicSourceB.isPlaying);
    }

    public void ManageAudioOnMinigameStart()
    {
        ToggleMinigameAudio(true);
    }

    public void ManageAudioOnMinigameWon()
    {
        alarmClockSource.clip = alarmClockWinLoseSFX[0];
        alarmClockSource.pitch = UnityEngine.Random.Range(0.95f, 1.1f);
        alarmClockSource.Play();
    }
    public void ManageAudioOnMinigameLost()
    {
        alarmClockSource.clip = alarmClockWinLoseSFX[1];
        alarmClockSource.pitch = UnityEngine.Random.Range(0.90f, 1.05f);
        alarmClockSource.Play();
    }
    public void ManageAudioOnMinigameDestroyed()
    {
        ToggleMinigameAudio(false);
        alarmClockSource.Stop();
    }

    //Audio toggles
    public void ToggleSFXAudio(bool enabled)
    {
        if(enabled)
        {
            mainAudioMixer.SetFloat("SFX", 0f);
        }
        else
        {
            mainAudioMixer.SetFloat("SFX", -80f);
        }
    }
    public void ToggleMinigameAudio(bool enabled)
    {
        if(enabled)
        {
            mainAudioMixer.SetFloat("Minigame", 0f);
        }
        else
        {
            mainAudioMixer.SetFloat("Minigame", -80f);
        }
    }
    public void ToggleUIAudio(bool enabled)
    {
        if(enabled)
        {
            mainAudioMixer.SetFloat("UI", 0f);
        }
        else
        {
            mainAudioMixer.SetFloat("UI", -80f);
        }
    }
    

    //chatgpt hihihihihihihihi no time
    Coroutine crossfadeCoroutine = null;
    IEnumerator Crossfade(AudioSource from, AudioSource to, float duration)
    {
        currentMusicSource = to;
        to.volume = 0f;
        to.Play();

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duration;

            from.volume = Mathf.Lerp(1f, 0f, k);
            to.volume = Mathf.Lerp(0f, 1f, k);

            yield return null;
        }

        from.Stop();
        from.volume = 1f;
        crossfadeCoroutine = null;
    }
    void ChangeSong(AudioClip newSong)
    {
        if(currentMusicSource == musicSourceA)
        {
            musicSourceB.clip = newSong;
            musicSourceB.time = songPlaybackTime;
            crossfadeCoroutine = StartCoroutine(Crossfade(musicSourceA, musicSourceB, 0.05f));
        }
        else
        {
            musicSourceA.clip = newSong;
            musicSourceA.time = songPlaybackTime;
            crossfadeCoroutine = StartCoroutine(Crossfade(musicSourceB, musicSourceA, 0.05f));
        }
    }
    void ManageSongPlayback()
    {
        if(currentSong == null && musicSourceA.clip == null && musicSourceB.clip == null)
        {
            Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            currentSong = GetSongForCurrentGameState();
            currentMusicSource.clip = currentSong;
            songPlaybackTime = 0f;
            currentMusicSource.Play();
        }
        else
        {
            if(currentMusicSource == musicSourceA)
            {
                songPlaybackTime = musicSourceA.time;
            }
            else
            {
                songPlaybackTime = musicSourceB.time;
            }
        }
        
    }

    public void ManageGameStateChangedForAudio(GameState state)
    {
        if(GameManager.Instance.CurrentState != GameState.MinigameTransition && musicSourceA != null && musicSourceB != null) {  //this ensures when the game is transitioning the current song isn't changed
            currentSong = GetSongForCurrentGameState();
            Debug.LogWarning("!!!!!!!!!!!!!!! trying to play new song: " + currentSong);
            
            if(currentMusicSource == musicSourceA)
            {
                musicSourceB.clip = currentSong;
            }
            else
            {
                musicSourceA.clip = currentSong;
            }
            
            if(IsCurrentPlaybackTimeValidForSong(currentMusicSource.clip, songPlaybackTime))
            {
                Debug.Log("valid playback time");
                ChangeSong(currentSong);
            }
            else
            {
                Debug.Log("invalid playback time");
                currentMusicSource.Play();
            }
                
                    
        }
    }

    bool IsCurrentPlaybackTimeValidForSong(AudioClip song, float time)
    {
        return (song != null && song.length > time);
    }

    AudioClip GetSongForCurrentGameState()
    {
        AudioClip outputSong = null;
        switch (GameManager.Instance.CurrentState)
        {
            case GameState.Bedroom:
                if (bedroomMusic != null)
                    outputSong = bedroomMusic;
                break;
            case GameState.Minigame:
                outputSong = GetSongForMinigame();
                break;
            case GameState.Menu:
                if (menuMusic != null)
                    outputSong = menuMusic;
                break;
            case GameState.MinigameTransition:
                break;
            default:
                if (defaultTrack != null)
                    outputSong = defaultTrack;
                break;
        }
        return outputSong;
    }
    AudioClip GetSongForMinigame()
    {
        AudioClip outputSong = null;
        switch(_minigameManager.currentActiveMinigame)
        {
            case MinigameType.Darkness:
                outputSong = matchMinigame;
                break;
            case MinigameType.Tightrope:
                outputSong = heightsMinigame;
                break;
            case MinigameType.Spider:
                outputSong = spiderMinigame;
                break;
            default:
                outputSong = defaultTrack;
                break;
        }
        Debug.Log("Selected minigame song: " + outputSong);
        return outputSong;
    }
    //Service management
    private void ManageServices()
    {
        _minigameManager = GameManager.Instance.GetService<MinigameManager>();
        if (_minigameManager == null)
        {
            GameManager.OnServiceRegistered += HandleServiceRegistered;
        }
        else
        {
            managersFound = true;
        }
    }

    private void HandleServiceRegistered(Type type)
    {
        if (type == typeof(MinigameManager))
        {
            _minigameManager = GameManager.Instance.GetService<MinigameManager>();
        }
        managersFound = (_minigameManager != null);
        if(managersFound)
        {
           // ManageEvents();
        }
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= ManageGameStateChangedForAudio;
        MinigameManager.OnMinigameWon -= ManageAudioOnMinigameWon;
        MinigameManager.OnMinigameLost -= ManageAudioOnMinigameLost;
        MinigameManager.OnMinigameDestroyed -= ManageAudioOnMinigameDestroyed;
        MinigameManager.OnMinigameStarted -= ManageAudioOnMinigameStart;
    }

    private void OnDestroy()
    {
        GameManager.Instance.DeregisterService<AudioManager>();
    }
}
