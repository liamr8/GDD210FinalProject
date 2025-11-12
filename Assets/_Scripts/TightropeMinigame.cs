using System.Collections;
using TMPro;
using UnityEngine;

public class TightropeMinigame : MonoBehaviour
{
    public RectTransform playerCharacter;

    [Header("Timers")]
    [SerializeField]float losingBalanceTimer = 0;
    public float losingBalanceTimerLimit;
    [SerializeField] float winTimer = 0;
    public float winTimerThreshold;

    public TMP_Text timerText;

    [Header("Minigame Values")]
    public float tiltPowerMultiplier = 1;

    int initialDirection;
    [SerializeField] float tiltValue;

    public float minimumAmountBalanceIsRandomlyLost;  // this is for the timer that randomly makes the player slightly lose their balance
    public float maximumAmountBalanceIsRandomlyLost;

    public float rotationThresholdToLose;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialDirection = (Random.Range(0,2) > 0) ? 1 : -1; //picks random direction to start the player tilting in
        tiltValue = Random.Range(6,8) * initialDirection;
    }

    // Update is called once per frame
    void Update()
    {
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
        timerText.text = "BALANCE\n"+(winTimerThreshold - winTimer).ToString();
    
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
            tiltValue += Random.Range(minimumAmountBalanceIsRandomlyLost, maximumAmountBalanceIsRandomlyLost) * direction;
        losingBalanceTimer = 0;
    }

    bool IsPlayerBalancedOnRope()
    {
        //Debug.LogWarning(playerCharacter.localRotation.z);
        return !(Mathf.Abs(playerCharacter.localRotation.z) > rotationThresholdToLose);
    }

    Coroutine minigameEndSequence = null;
    void WinGame()
    {
        Debug.LogWarning("player won");
        minigameEndSequence ??= StartCoroutine(WinGameCoroutine());
    }
    IEnumerator WinGameCoroutine()
    {
        yield return new WaitForSeconds(3);
        GameManager.Instance.GetService<MinigameManager>().PlayerWinMinigame();
        Destroy(transform.parent.gameObject);
    }
    void LoseGame()
    {
        Debug.LogError("player lost");
        minigameEndSequence ??= StartCoroutine(LoseGameCoroutine());
    }
    IEnumerator LoseGameCoroutine()
    {
        yield return new WaitForSeconds(3);
        GameManager.Instance.GetService<MinigameManager>().PlayerLoseMinigame();
        Destroy(transform.parent.gameObject);
    }
    void AdvanceTimers()
    {
        winTimer += Time.deltaTime;
        losingBalanceTimer += Time.deltaTime;
    }
    
}
