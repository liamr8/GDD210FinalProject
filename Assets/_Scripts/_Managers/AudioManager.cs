using UnityEngine;
using System;
using UnityEngine.Assertions.Must;
using System.Collections;
using UnityEngine.Audio;
using UnityEngine.UI;
using Unity.VisualScripting;
public class AudioManager : MonoBehaviour
{
    public AudioMixer mainAudioMixer;

    [SerializeField] float currentSavedMasterPitch = 1.0f;

    public float GetCurrentSavedMasterPitch() {return currentSavedMasterPitch;}


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
        GameManager.OnGameStateChanged += ResetMasterParameters;
        
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
            MinigameManager.OnMinigameWonExit += ManageAudioOnCloudTransition;
            MinigameManager.OnMinigameLostExit += ManageAudioOnCloudTransition;
            MinigameManager.OnTransitionStarted += ManageAudioOnCloudTransition;
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
            MinigameManager.OnMinigameWonExit += ManageAudioOnCloudTransition;
            MinigameManager.OnMinigameLostExit += ManageAudioOnCloudTransition;
            MinigameManager.OnTransitionStarted += ManageAudioOnCloudTransition;
            eventHandlersSubscribed = true;
        }
        ManageSongPlayback();
        //Debug.LogError("Live audio source time: " + currentMusicSource.time +"\nSaved playback time: " + songPlaybackTime);
        //Debug.Log("musicSourceA is playing: "+ musicSourceA.isPlaying+"\nmusicSourceB is playing: " + musicSourceB.isPlaying);
    }


    Coroutine transitionAudioCoroutine = null;
    public void ManageAudioOnCloudTransition()
    {
        //Debug.Log("trying to muffle audio");
        transitionAudioCoroutine ??= StartCoroutine(MuffleAudio());
    }
    IEnumerator MuffleAudio()
    {
        while(mainAudioMixer.GetFloat("MasterLowPass", out float currentCutoff) && currentCutoff > 1600f
        && mainAudioMixer.GetFloat("MasterPitch", out float currentPitch) && currentPitch > currentSavedMasterPitch-0.1f)
        {
            mainAudioMixer.SetFloat("MasterLowPass", Mathf.Max(3000f, currentCutoff - 44000f * Time.unscaledDeltaTime));
            mainAudioMixer.SetFloat("MasterPitch", Mathf.Max(currentSavedMasterPitch-0.1f, currentPitch - 0.25f * Time.unscaledDeltaTime));
            yield return null;
        }
        mainAudioMixer.SetFloat("MasterLowPass", 3000f);
        mainAudioMixer.SetFloat("MasterPitch", currentSavedMasterPitch-0.1f);
        yield return new WaitUntil(() => _minigameManager.IsScreenTransitionFinished());

        while(mainAudioMixer.GetFloat("MasterLowPass", out float currentCutoff) && currentCutoff < 22000f 
        && mainAudioMixer.GetFloat("MasterPitch", out float currentPitch) && currentPitch < currentSavedMasterPitch)
        {
            mainAudioMixer.SetFloat("MasterLowPass", Mathf.Min(22000f, currentCutoff + 66000f * Time.unscaledDeltaTime));
            mainAudioMixer.SetFloat("MasterPitch", Mathf.Min(currentSavedMasterPitch, currentPitch + 0.6f * Time.unscaledDeltaTime));
            yield return null;
        }
        mainAudioMixer.SetFloat("MasterLowPass", 22000f);
        mainAudioMixer.SetFloat("MasterPitch", currentSavedMasterPitch);
        transitionAudioCoroutine = null;
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
        if(GameManager.Instance.GetService<MinigameManager>().GetScore() % 3 == 0)
            currentSavedMasterPitch = 1f + (GameManager.Instance.GetService<MinigameManager>().GetScore() / 3) * 0.025f;
        alarmClockSource.Stop();
    }
    
    public void ResetMasterParameters(GameState state)
    {
        if(state != GameState.Menu)
            return;
        mainAudioMixer.SetFloat("MasterLowPass", 22000f);
        mainAudioMixer.SetFloat("MasterPitch", 1.0f);
        currentSavedMasterPitch = 1.0f;
    }
    //Audio toggles
    public void ToggleSFXAudio(bool enabled)
    {
        if(enabled)
        {
            mainAudioMixer.SetFloat("SFXVolume", 0f);
        }
        else
        {
            mainAudioMixer.SetFloat("SFXVolume", -80f);
        }
    }
    public void ToggleMinigameAudio(bool enabled)
    {
        if(enabled)
        {
            mainAudioMixer.SetFloat("MinigameVolume", 0f);
        }
        else
        {
            mainAudioMixer.SetFloat("MinigameVolume", -80f);
        }
    }
    public void ToggleUIAudio(bool enabled)
    {
        if(enabled)
        {
            mainAudioMixer.SetFloat("UIVolume", 0f);
        }
        else
        {
            mainAudioMixer.SetFloat("UIVolume", -80f);
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
        MinigameManager.OnMinigameWonExit -= ManageAudioOnCloudTransition;
        MinigameManager.OnMinigameLostExit -= ManageAudioOnCloudTransition;
        MinigameManager.OnTransitionStarted -= ManageAudioOnCloudTransition;
    }

    private void OnDestroy()
    {
        GameManager.Instance.DeregisterService<AudioManager>();
    }
}
