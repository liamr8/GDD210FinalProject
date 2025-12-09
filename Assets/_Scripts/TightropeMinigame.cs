using System.Collections;
using TMPro;
using UnityEngine;

public class TightropeMinigame : MonoBehaviour
{
    public RectTransform playerCharacter;


    [Header("Difficulty Settings")]
    public int maximumAdditionalAmountBalanceIsRandomlyLostFromDifficulty = 7; //holy wrap it up buddy

    public float baseDifficultyTimeToWin = 9f;
    public float maximumDifficultyTimeToWin = 13f;
    public float baseDifficultyTimeToLoseBalance = 3f;
    public float maximumDifficultyTimeReductionToLoseBalance = 1f;

    public float baseDifficultyRotationThresholdToLose = 0.35f;
    public float maximumDifficultyRotationThresholdToLose = 0.22f;

    [Header("Timers")]
    [SerializeField]float losingBalanceTimer = 0;
    public float losingBalanceTimerLimit;
    [SerializeField] float winTimer = 0;
    public float winTimerThreshold;

    public UnityEngine.UI.Image timerBarFill;

    [Header("Minigame Values")]
    public float tiltPowerMultiplier = 1;

    int initialDirection;
    [SerializeField] float tiltValue;

    public float minimumAmountBalanceIsRandomlyLost;  // this is for the timer that randomly makes the player slightly lose their balance
    public float maximumAmountBalanceIsRandomlyLost;

    public float rotationThresholdToLose;

    bool minigameEndStateDebounce = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialDirection = (Random.Range(0,2) > 0) ? 1 : -1; //picks random direction to start the player tilting in
        tiltValue = Random.Range(6,8) * initialDirection;
        winTimerThreshold = Mathf.Lerp(baseDifficultyTimeToWin, maximumDifficultyTimeToWin, MinigameManager.GetCurrentDifficultyValue());
        losingBalanceTimerLimit = Random.Range(baseDifficultyTimeToLoseBalance, baseDifficultyTimeToLoseBalance - Mathf.Lerp(0,maximumDifficultyTimeReductionToLoseBalance, MinigameManager.GetCurrentDifficultyValue()));
        rotationThresholdToLose = Mathf.Lerp(baseDifficultyRotationThresholdToLose, maximumDifficultyRotationThresholdToLose, MinigameManager.GetCurrentDifficultyValue());
    }

    // Update is called once per frame
    void Update()
    {
        if(!minigameEndStateDebounce) {
            GetPlayerInput();
            TiltPlayer(tiltValue);
            if(losingBalanceTimer > losingBalanceTimerLimit)
            NudgePlayerInDirection();
            if (!IsPlayerBalancedOnRope())
                LoseGame();
            else if (winTimer > winTimerThreshold)
            {
                WinGame();
                return;
            }
            AdvanceTimers();
        }
        timerBarFill.fillAmount = Mathf.InverseLerp(0, winTimerThreshold, winTimerThreshold-winTimer);
    
    }
    float GetPlayerInput()
    {
        
        Vector3 phoneRotationVector = Input.gyro.rotationRate;
        float phoneTilt = phoneRotationVector.z;
        Debug.Log(phoneRotationVector);
        tiltValue += phoneTilt;
        return phoneTilt;
    }
    void TiltPlayer(float amountToTilt)
    {
        playerCharacter.Rotate(Vector3.forward * amountToTilt * tiltPowerMultiplier * Time.deltaTime, Space.Self);
    }

    void NudgePlayerInDirection()
    {
        float direction;
        if (Random.Range(0f, 1f) < 0.7f)  // "0.7f" determines the probability for which direction the player will lean in when they randomly lose balance
        {
            direction = tiltValue > 0 ? direction = 1 : direction = -1;
        }
        else
            direction = tiltValue <= 0 ? direction = 1 : direction = -1;
        tiltValue += Random.Range(minimumAmountBalanceIsRandomlyLost, maximumAmountBalanceIsRandomlyLost + 
        Random.Range(0f, maximumAdditionalAmountBalanceIsRandomlyLostFromDifficulty)) * direction;
        losingBalanceTimer = 0;
        losingBalanceTimerLimit = Random.Range(baseDifficultyTimeToLoseBalance, baseDifficultyTimeToLoseBalance - Mathf.Lerp(0,maximumDifficultyTimeReductionToLoseBalance, MinigameManager.GetCurrentDifficultyValue()));
    }

    bool IsPlayerBalancedOnRope()
    {
        //Debug.LogWarning(playerCharacter.localRotation.z);
        return !(Mathf.Abs(playerCharacter.localRotation.z) > rotationThresholdToLose);
    }


    Coroutine minigameEndSequence = null;
    void WinGame()
    {
        minigameEndStateDebounce = true;
        Debug.LogWarning("player won");
        minigameEndSequence ??= StartCoroutine(WinGameCoroutine());
    }
    
    void LoseGame()
    {
        minigameEndStateDebounce = true;
        Debug.LogError("player lost");
        minigameEndSequence ??= StartCoroutine(LoseGameCoroutine());
    }
    System.Collections.IEnumerator WinGameCoroutine()
    {
        MinigameManager mm = GameManager.Instance.GetService<MinigameManager>();
        yield return new WaitForSeconds(3);
        mm.PlayerWinMinigame();
        yield return new WaitUntil(() => mm.IsScreenTransitionFinished());
        Destroy(transform.parent.gameObject);
    }
    
    System.Collections.IEnumerator LoseGameCoroutine()
    {
        MinigameManager mm = GameManager.Instance.GetService<MinigameManager>();

        float gravityValue = -840f;
        float currentPlayerGravity = 300f;
        while(playerCharacter.anchoredPosition.y > -1200f)
        {
            currentPlayerGravity += gravityValue * Time.deltaTime;
            playerCharacter.anchoredPosition += new Vector2(-Mathf.Sign(playerCharacter.localRotation.z) * 350 * Time.deltaTime, currentPlayerGravity * Time.deltaTime); //for some reason this has to be negative kill me
            yield return null;
        }

        yield return new WaitForSeconds(1);
        mm.PlayerLoseMinigame();
        yield return new WaitUntil(() => mm.IsScreenTransitionFinished());
        Destroy(transform.parent.gameObject);
    }
    void AdvanceTimers()
    {
        winTimer += Time.deltaTime;
        losingBalanceTimer += Time.deltaTime;
    }
    
}
