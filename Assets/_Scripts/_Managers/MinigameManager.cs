using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using UnityEngine.SceneManagement;
using TMPro;
using System.Runtime;
using System.Collections.Generic;

public class MinigameManager : MonoBehaviour
{
    public MinigameType lastMinigame;
    public MinigameType currentActiveMinigame;
    public MinigameType nextMinigame;

    public Transform minigameParent;
    public Transform transitionScreenParent;
 
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public Transform playerLivesDisplay;
    public Sprite heartFull, heartEmpty;
    


    //Events also i have no idea wtf a delegate is, so if this is terrible practice that's why.
    //unfortunately i don't have time to learn what they are rn! i do like events tho :3

    public delegate void OnTransition();
    public delegate void OnMinigameEvent();
    public static event OnTransition OnMinigameWonExit;
    public static event OnTransition OnMinigameLostExit;

    public static event OnMinigameEvent OnMinigameWon;
    public static event OnMinigameEvent OnMinigameLost;
    public static event OnMinigameEvent OnMinigameDestroyed;
    public static event OnMinigameEvent OnMinigameStarted;

    public void ManageMinigameWonExit()
    {
        //if(currentActiveMinigame == MinigameType.None)
        timerToTransition = 0;
        GameObject newTransitionScreen = Instantiate(GetTransitionScreenPrefab(TransitionScreenType.Won), transitionScreenParent);
        GameManager.Instance.UpdateGameState(GameState.MinigameTransition);
        transitionDebounce = false;
    }
    public void ManageMinigameLostExit()
    {
        timerToTransition = 0;
        GameObject newTransitionScreen = Instantiate(GetTransitionScreenPrefab(TransitionScreenType.Won), transitionScreenParent);
        GameManager.Instance.UpdateGameState(GameState.MinigameTransition);
        transitionDebounce = false;
    }
    

    [SerializeField] float miniGameTransitionTimer = 0;

    [SerializeField] Quaternion startingGyroRotation;

    [Header("Timers")]
    [SerializeField] float timerToTransition = 0;
    [SerializeField]float timeInBedroomScene;
    [SerializeField]float timeTransitioningMinigame;

    [Header("Saved Player Data")]
    [SerializeField]int score = 0;
    [SerializeField]int lives = 3;
    [SerializeField]int maxLives = 3;

    [SerializeField]int maximumPointDifficulty = 20; //This is the maximum point value used for increasing the difficulty of the minigames. Anything past this number won't become any harder.

    [Header("Minigame Prefabs")]
    public GameObject darknessMinigame;
    public GameObject tightRopeMinigame;
    public GameObject spiderMinigame;

    [Header("ScreenTransition Settings")]
    public Animator bedroomTransitionAnimator;
    public Animator screenTransitionAnimator;
    [Header("ScreenTransition Prefabs")]
    public GameObject menuToGame;
    public GameObject minigameToNightTerror;
    public GameObject minigameToSurvived;
    public GameObject toLost;
    public GameObject toWon;

    [Header("Testing Tools (warning: nonfunctional currently. ignore.)")]
    public List<MinigameType> minigamesToExclude;

    bool managersFound = false;

    bool loadingSceneDebounce = false; //to prevent multiple scene load calls (i.e. loading the menu)

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Input.gyro.enabled = true;
        Input.compensateSensors = true;
        Input.multiTouchEnabled = true;

        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;

        Screen.orientation = ScreenOrientation.LandscapeLeft;

        ResetGyro();

        GameManager.Instance.RegisterService(this);

        OnMinigameWonExit += ManageMinigameWonExit;
        OnMinigameLostExit += ManageMinigameLostExit;
        
        if(transitionScreenParent.childCount > 0)
            screenTransitionAnimator = transitionScreenParent.GetChild(0).GetComponent<Animator>();
        ManageServices();
        if(managersFound)
        {

        }
        GameObject _transitionObject = Instantiate(GetTransitionScreenPrefab(TransitionScreenType.Won), transitionScreenParent);
        _transitionObject.GetComponent<Animator>().Play("transitionStartMinigameInGame", 0, 0f);
    }
    bool transitionDebounce = true; //to prevent code logic from running code multiple times when a transition just finished
    //MUST BE SET TO TRUE BY DEFAULT, OTHERWISE THE FIRST SCREEN TRANSITION TRIGGERS BEHAVIOR THAT SHOULDN'T HAPPEN UNTIL A MINIGAME FIRST ENDS

    // Update is called once per frame
    void Update()
    {
        if (!managersFound)
        {
            ManageServices();
        }
        UpdateBedroomStats();

        if(transitionScreenParent.childCount > 0)
        {
            screenTransitionAnimator = transitionScreenParent.GetChild(0).GetComponent<Animator>();
        }

        if (IsPlayerAlive())
        {
            if (!(minigameParent.childCount > 0))
            {
                ManageTransitions();
                if (!bedroomTransitionAnimator.GetBool("AnimationTriggered"))
                {
                    bedroomTransitionAnimator.SetBool("AnimationTriggered", true);
                    bedroomTransitionAnimator.Play("BedroomShrug", 0, 0f);
                    bedroomTransitionAnimator.Update(0f);
                }
                
                    
                minigameParent.gameObject.SetActive(false);
                if(IsScreenTransitionFinished())
                {
                    if(!transitionDebounce) {
                        transitionDebounce = true;
                        //Debug.LogError("Modified value");
                        lastMinigame = currentActiveMinigame;
                        currentActiveMinigame = MinigameType.None;
                        GameManager.Instance.UpdateGameState(GameState.Bedroom);
                    }
                    if (miniGameTransitionTimer > 3 && nextMinigame == MinigameType.None && currentActiveMinigame == MinigameType.None)
                    {
                        PickNewRandomMinigame();
                        ChangeCurrentMinigame();
                        //Debug.LogWarning("tried to pick minigame");
                        bedroomTransitionAnimator.SetBool("AnimationTriggered", false);
                    }
                }
                //ManageTransitions();
            }
        }
        else if (!(minigameParent.childCount > 0))
        {
            GameManager.playerHighscore = score;
            if(!loadingSceneDebounce)
            {
                loadingSceneDebounce = true;
                GameManager.Instance.LoadSceneFromGameManagerAsync("Menu");
            }
            if (IsScreenTransitionFinished())
            {
                GameManager.Instance.ActivateScene(); // loads the scene the moment the transition finishes
            }
        }
        
        if(IsScreenTransitionFinished() && screenTransitionAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
        {
            Destroy(transitionScreenParent.GetChild(0).gameObject);
            //Debug.LogError("attempt to destroy transition");
        }
        
    }
    public void ManageTransitions()
    {
        if(timerToTransition > timeTransitioningMinigame || nextMinigame == MinigameType.None) //added else
        {
            if(!(transitionScreenParent.childCount > 0) && miniGameTransitionTimer > 2.8f) //magic num 2.8f
            {
                GameObject newTransitionScreen = Instantiate(GetTransitionScreenPrefab(TransitionScreenType.Won), transitionScreenParent);
                GameManager.Instance.UpdateGameState(GameState.MinigameTransition);
            }

            miniGameTransitionTimer += Time.deltaTime;
        }
        if(GameManager.Instance.CurrentState == GameState.MinigameTransition)
            timerToTransition += Time.deltaTime;

    }
    public float GetAdjustedLength(AnimationClip clip, Animator animator, float stateSpeed)
    {
        return clip.length / (stateSpeed * animator.speed);
    }
    public void PickNewRandomMinigame()
    {
        Array minigameTypes = MinigameType.GetValues(typeof(MinigameType));

        int failsafeValue = 0;
        do
        {
            nextMinigame = (MinigameType)minigameTypes.GetValue(UnityEngine.Random.Range(2, minigameTypes.Length)); //starting at 2 to skip none and tutorial values
            failsafeValue++;
            Debug.Log(minigamesToExclude.Contains(nextMinigame) + " " + nextMinigame + " " +"failsafe"+(failsafeValue<99));
        }
        while (nextMinigame == lastMinigame/* && minigamesToExclude.Contains(nextMinigame) &&
        failsafeValue < 99*/);  ///// FIX THIS LATER THIS IS REALLY HELPFUL FOR DEBUGGING!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ////////////////////////////////////
        ////////////////////////////////////
    }
    public float GetCurrentDifficultyValue() //returns a float with 
    {
        return Mathf.InverseLerp(0, maximumPointDifficulty, score);
    }

    //Coroutine ChangingMinigameCoroutine = null;
    public bool IsScreenTransitionFinished() // Returns the point where the screen transition animation covers the entire screen to allow smooth transitioning between game sections
    {
        if(screenTransitionAnimator != null)
        {
           // Debug.LogWarning(screenTransitionAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        }
        return screenTransitionAnimator != null && 
                        transitionScreenParent.childCount > 0 &&
                        screenTransitionAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Last");
                        //&& !(screenTransitionAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)));
        // this disgusting return statement checks whether or not to transition animation is at the end of the first half of it's animation
        // or the beginning of the second half of it's animation (denoted with tags "First" (first half) and "Last" (second half))
    }
    public void ChangeCurrentMinigame()
    {
        //Debug.LogError("ChangingMinigameCoroutine is null: " + ChangingMinigameCoroutine == null);
        currentActiveMinigame = nextMinigame;
        nextMinigame = MinigameType.None;
        miniGameTransitionTimer = 0;
        GameManager.Instance.UpdateGameState(GameState.Minigame);
        GameObject newMinigame = Instantiate(GetMinigamePrefab(currentActiveMinigame), minigameParent);
        newMinigame.transform.SetSiblingIndex(0);
        
        minigameParent.gameObject.SetActive(true);
        OnMinigameStarted?.Invoke();
        //ChangingMinigameCoroutine = null;
        //ChangingMinigameCoroutine ??= StartCoroutine(IChangeCurrentMinigame());
    }
    /*public IEnumerator IChangeCurrentMinigame()
    {
        Debug.LogWarning("Entering coroutine");
        yield return new WaitUntil(() => GetMinigamePrefab(currentActiveMinigame) != null);
        miniGameTransitionTimer = 0;
        GameManager.Instance.UpdateGameState(GameState.Minigame);
        GameObject newMinigame = Instantiate(GetMinigamePrefab(currentActiveMinigame), minigameParent);
        newMinigame.transform.SetSiblingIndex(0);
        
        minigameParent.gameObject.SetActive(true);
        ChangingMinigameCoroutine = null;
    }*/ 
    //KEEPING THIS HERE JUST IN CASE FOR SOME REASON IT WORKS WITHOUT IT NOW????????



    private GameObject GetTransitionScreenPrefab(TransitionScreenType screenType)
    {
        switch(screenType)
        {
            case TransitionScreenType.None:
                return null;
            case TransitionScreenType.NightTerror:
                return minigameToNightTerror;
            case TransitionScreenType.Survived:
                return minigameToSurvived;
            case TransitionScreenType.Lost:
                return toLost;
            case TransitionScreenType.Won:
                return toWon;
            default:
                return null;
        }
    }

    private GameObject GetMinigamePrefab(MinigameType minigame)
    {
        switch (minigame)
        {
            case MinigameType.None:
                break;
            case MinigameType.Tutorial:
                break;
            case MinigameType.Darkness:
                return darknessMinigame;
            case MinigameType.Tightrope:
                return tightRopeMinigame;
            case MinigameType.Spider:
                return spiderMinigame;
            default:
                break;
        }
        return null;
    }

    void ResetGyro()
    {
        startingGyroRotation = Quaternion.Inverse(GyroToUnity(Input.gyro.attitude));
    }
    public static Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }


    public void OnMinigameDestroyedInvoke(){
        OnMinigameDestroyed?.Invoke();
    }

    public void OnPlayerWinMinigame(){
        OnMinigameWon?.Invoke();
    }
    public void OnPlayerLoseMinigame(){
        OnMinigameLost?.Invoke();
    }

    public void PlayerWinExitMinigame(){
        OnMinigameWonExit?.Invoke();
        score++;
    }
    public void PlayerLoseExitMinigame(){ 
        OnMinigameLostExit?.Invoke();
        lives--;
    }

    bool IsPlayerAlive()
    {
        return lives > 0;
    }
    
    void UpdateBedroomStats()
    {
        Debug.LogError("fuck");
        timerText.text = (Mathf.Ceil(3 - miniGameTransitionTimer)).ToString();
        scoreText.text = "Nights Survived: "+score.ToString();
        for (int i = maxLives-1; i > lives-1; i--)
        {
            playerLivesDisplay.GetChild(i).GetComponent<UnityEngine.UI.Image>().sprite = heartEmpty;
        }
    }
    //Service management
    private void ManageServices()
    {
        /*_phoneManager = GameManager.Instance.GetService<PhoneManager>();
        _appManager = GameManager.Instance.GetService<AppManager>();
        _enemyManager = GameManager.Instance.GetService<EnemyManager>();
        _conversationManager = GameManager.Instance.GetService<ConversationManager>();
        if (_phoneManager == null || _appManager == null || _conversationManager == null)
        {
            GameManager.OnServiceRegistered += HandleServiceRegistered;
        }
        else
        {
            managersFound = true;
        }*/
    }

    private void HandleServiceRegistered(Type type)
    {
        /*if (type == typeof(PhoneManager))
        {
            _phoneManager = GameManager.Instance.GetService<PhoneManager>();
        }
        else if (type == typeof(AppManager))
        {
            _appManager = GameManager.Instance.GetService<AppManager>();
        }
        else if (type == typeof(EnemyManager))
        {
            _enemyManager = GameManager.Instance.GetService<EnemyManager>();
        }
        else if (type == typeof(ConversationManager))
        {
            _conversationManager = GameManager.Instance.GetService<ConversationManager>();
        }
        managersFound = (_phoneManager != null || _appManager != null || _conversationManager != null);
        if(managersFound)
        {
           // ManageEvents();
        }*/
    }
    private void OnDestroy()
    {
        GameManager.Instance.DeregisterService<MinigameManager>();
    }
}
