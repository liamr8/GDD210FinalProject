
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using static GameManager;
using System.Linq;
using UnityEngine.SocialPlatforms.Impl;


public class GameManager : MonoBehaviour
{
    // Note: a lot of this manager code is recycled from my code in a previous project.
    // Mainly the service registration aspect to prevent NullReferenceExceptions (if my beginner understanding is correct)
    public static GameManager Instance { get; private set; }
    public GameState CurrentState;
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<Type> OnServiceRegistered;

    [Header("Saved Player Data")]
    int points = 0;
    int lives = 3;
    
    
    private void Awake()
    {
        Application.targetFrameRate = 144;
        // Check if another instance already exists
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Game Manager already exists! Destroying this instance: ");
            // Destroy the new instance if another exists
            Destroy(gameObject);
            return; // Early return to avoid further initialization
        }

        // Set the instance to the current instance
        Instance = this;

        // Ensure that the GameManager persists between scenes
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void Start()
    {
        
    }
    void OnDisable()
    {
        
    }
    void Update()
    {
        if (CurrentState == GameState.Minigame)
        {

        }
    }
    
    //Resets information the game is keeping track of across scenes. Usually related to personal player data, but could be various things.
    public void ResetGameManagerData()
    {
       
    }

    //Player object doesn't always exist across scenes
    public Transform FindPlayerTransform()
    {
        return GameObject.Find("Player").transform;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        
        switch (scene.name)
        {
            case "Menu":
                UpdateGameState(GameState.Menu);
                break;
            case "Tutorial":
                UpdateGameState(GameState.Tutorial);
                break;
            case "Scene0":
                UpdateGameState(GameState.MinigameTransition);
                break;
            default:
                // code block
                break;
        }
    }

    public void LoadSceneFromGameManager(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName);
    }
    public void ExitGame() { Application.Quit(); }
    
    public void UpdateGameState(GameState newState)
    {
        CurrentState = newState;
        switch (newState)
        {
            case GameState.Menu:
                break;
            case GameState.Pause:
                break;
            case GameState.Tutorial:
                break;
            case GameState.Minigame:
                break;
            case GameState.MinigameTransition:
                break;
            case GameState.Cutscene:
                break;
            case GameState.Win:
                break;
            case GameState.Lose:
                break;    
            

        }

        OnGameStateChanged?.Invoke(newState);
    }


    // Service Registration
    public void RegisterService<T>(T service) where T : class
    {
        var type = typeof(T);
        services[type] = service;
        OnServiceRegistered?.Invoke(type);
    }

    public T GetService<T>() where T : class
    {
        return services.TryGetValue(typeof(T), out var service) ? service as T : null;
    }
    public void DeregisterService<T>() where T : class
    {
        var type = typeof(T);
        if (services.ContainsKey(type))
        {
            services.Remove(type);
        }
    }
    private Dictionary<Type, object> services = new Dictionary<Type, object>();

}
