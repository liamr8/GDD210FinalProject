using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using UnityEngine.SceneManagement;
using TMPro;
using System.Runtime;

public class MinigameManager : MonoBehaviour
{
    public MinigameType currentActiveMinigame;
    public MinigameType nextMinigame;

    public Transform minigameParent;
    public Transform transitionScreenParent;
 
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public Transform playerLivesDisplay;
    public Sprite heartFull, heartEmpty;
    
    public delegate void OnTransition();
    public static event OnTransition OnMinigameWon;
    public static event OnTransition OnMinigameLost;
    public void ManageMinigameWon()
    {
        //if(currentActiveMinigame == MinigameType.None)
        timerToTransition = 0;
        GameManager.Instance.UpdateGameState(GameState.MinigameTransition);

    }
    public void ManageMinigameLost()
    {
        timerToTransition = 0;
        GameManager.Instance.UpdateGameState(GameState.MinigameTransition);
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

    bool managersFound = false;

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
        OnMinigameWon += ManageMinigameWon;
        OnMinigameLost += ManageMinigameLost;
        if(transitionScreenParent.childCount > 0)
            screenTransitionAnimator = transitionScreenParent.GetChild(0).GetComponent<Animator>();
        ManageServices();
        if(managersFound)
        {

        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!managersFound)
        {
            ManageServices();
        }

        if (IsPlayerAlive())
        {
            if(transitionScreenParent.childCount > 0)
            {
                screenTransitionAnimator = transitionScreenParent.GetChild(0).GetComponent<Animator>();
            }
                    

            if (!(minigameParent.childCount > 0))
            {
                if (!bedroomTransitionAnimator.GetBool("AnimationTriggered"))
                {
                    bedroomTransitionAnimator.SetBool("AnimationTriggered", true);
                    bedroomTransitionAnimator.Play("BedroomShrug", 0, 0f);
                    bedroomTransitionAnimator.Update(0f);
                }
                
                    
                minigameParent.gameObject.SetActive(false);
                if (miniGameTransitionTimer > 3 && IsScreenTransitionFinished())
                {
                    PickNewRandomMinigame();
                    ChangeCurrentMinigame();
                    bedroomTransitionAnimator.SetBool("AnimationTriggered", false);
                }
                ManageTransitions();
                    
            }
            
            if(IsScreenTransitionFinished() && screenTransitionAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
                {
                    Destroy(transitionScreenParent.GetChild(0).gameObject);
                    Debug.LogError("attempt to destroy transition");
            }

        }
        else
        {
            if (miniGameTransitionTimer > 3)
            {
                SceneManager.LoadScene("Menu");
            }
            miniGameTransitionTimer += Time.deltaTime;
        }
        UpdateBedroomStats();
    }
    public void ManageTransitions()
    {
        if(timerToTransition > timeTransitioningMinigame || nextMinigame == MinigameType.None) //added else
        {
            if(!(transitionScreenParent.childCount > 0) && miniGameTransitionTimer > 2.8f) //magic num 2.8f
            {
                GameObject newTransitionScreen = Instantiate(GetTransitionScreenPrefab(TransitionScreenType.Won), transitionScreenParent);
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

        do
        {
            nextMinigame = (MinigameType)minigameTypes.GetValue(UnityEngine.Random.Range(2, minigameTypes.Length)); //starting at 2 to skip none and tutorial values
        }
        while (nextMinigame == currentActiveMinigame);
    }


    Coroutine ChangingMinigameCoroutine = null;
    public bool IsScreenTransitionFinished()
    {
        if(screenTransitionAnimator != null)
        {
            Debug.LogWarning(screenTransitionAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        }
        return screenTransitionAnimator != null &&
                        transitionScreenParent.childCount > 0 &&
                        screenTransitionAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Last");
    }
    public void ChangeCurrentMinigame()
    {
        Debug.LogError("ChangingMinigameCoroutine is null: " + ChangingMinigameCoroutine == null);
        ChangingMinigameCoroutine ??= StartCoroutine(IChangeCurrentMinigame());
    }
    public IEnumerator IChangeCurrentMinigame()
    {
        Debug.LogWarning("Entering coroutine");
        currentActiveMinigame = nextMinigame;
        nextMinigame = MinigameType.None;
        yield return new WaitUntil(() => GetMinigamePrefab(currentActiveMinigame) != null);
        miniGameTransitionTimer = 0;
        GameManager.Instance.UpdateGameState(GameState.Minigame);
        GameObject newMinigame = Instantiate(GetMinigamePrefab(currentActiveMinigame), minigameParent);
        newMinigame.transform.SetSiblingIndex(0);
        
        minigameParent.gameObject.SetActive(true);
        ChangingMinigameCoroutine = null;
    }



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

    public void PlayerWinMinigame(){
        OnMinigameWon.Invoke();
        score++;
    }
    public void PlayerLoseMinigame(){ 
        OnMinigameLost.Invoke();
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
