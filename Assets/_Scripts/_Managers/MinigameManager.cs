using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using UnityEngine.SceneManagement;
using TMPro;

public class MinigameManager : MonoBehaviour
{
    public MinigameType currentActiveMinigame;
    public MinigameType nextMinigame;

    public Transform minigameParent;
 
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public Transform playerLivesDisplay;
    public Sprite heartFull, heartEmpty;
    
    

    [SerializeField] float miniGameTransitionTimer = 0;

    [SerializeField] Quaternion startingGyroRotation;

    [Header("Timers")]
    float timerToTransition = 0;
    float timeInBedroomScene;

    [Header("Saved Player Data")]
    [SerializeField]int score = 0;
    [SerializeField]int lives = 3;
    [SerializeField]int maxLives = 3;

    [Header("Minigame Prefabs")]
    public GameObject darknessMinigame;
    public GameObject tightRopeMinigame;


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
            if (!(minigameParent.childCount > 0))
            {
                minigameParent.gameObject.SetActive(false);
                if (miniGameTransitionTimer > 3)
                {

                    PickNewRandomMinigame();
                    ChangeCurrentMinigame();
                }
                miniGameTransitionTimer += Time.deltaTime;
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
        GameObject newMinigame = Instantiate(GetMinigamePrefab(currentActiveMinigame), minigameParent);
        newMinigame.transform.SetSiblingIndex(0);
        minigameParent.gameObject.SetActive(true);
        ChangingMinigameCoroutine = null;
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

    public void PlayerWinMinigame(){ score++; }
    public void PlayerLoseMinigame(){ lives--; }

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
