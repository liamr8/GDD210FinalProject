using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Runtime.InteropServices.WindowsRuntime;
using System;
using NUnit.Framework;
using TMPro;
using Unity.VisualScripting;
public class DarknessMinigame : MonoBehaviour
{

    //TEMPORARILY STORING TOUCH LOGIC IN THIS 
    private Vector2 startTouchPosition;
    private Vector2 currentTouchPosition;
    private Vector2 touchDelta = Vector2.zero;
    private bool isSwiping;
    ///////////////////////////////////
    public RectTransform matchStick, matchBox;
    public RectTransform matchStickTip, matchBoxStrikerStrip;

    public EventSystem uiEventSystem;
    private Canvas UI;

    [Header("Timer")]
    float loseTimer = 0;
    public float timeToLose;
    public TMP_Text timerText;
    bool minigameEndStateDebounce = false;

    [Header("Match Stick Information")]
    private bool isTouchingMatch = false;  // is true if the player has touched the match and not taken their finger off the screen.
    private bool isStriking = false;
    public int currentNumberOfValidMatchStrikes;
    public int minimumValidMatchStrikesToWin;

    public float minimumValidMatchStrikeValue;
    [SerializeField] private float currentMatchStrikeValue = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        uiEventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();
        UI = GameObject.Find("UI").GetComponent<Canvas>();
        timerText = GameObject.Find("MinigamePanel").transform.GetChild(0).GetComponent<TMP_Text>();
    }
    
    // Update is called once per frame
    void Update()
    {
        ManageMatchInput();
        if(!minigameEndStateDebounce)
        {
            TimeSwipe();
            ManageMatchAndStrikerInteraction();
            if (loseTimer > timeToLose)
            {
                LoseGame();
            }
            else if(CheckIfPlayerWon())
            {
                WinGame();
                Debug.LogError("minigame completed");
            }
            AdvanceTimers();
            timerText.text = "IGNITE\n"+(timeToLose-loseTimer).ToString();
        }
        ParticleSystem ps = matchStickTip.transform.GetComponent<ParticleSystem>();
        var psEmission = ps.emission;
        psEmission.rateOverTime = Mathf.Lerp(0, 45f, Mathf.InverseLerp(0, minimumValidMatchStrikeValue, currentMatchStrikeValue));
        /*testText.text = currentNumberOfValidMatchStrikes.ToString()  + "\n" + currentMatchStrikeValue.ToString() +
         "\n" + IsMatchTipIsTouchingStrikerStrip() + "\n" + Mathf.Abs(touchDelta.magnitude).ToString()
         + "\n" + Mathf.Clamp(GetDotProductOfMatchAndStrikerStrip(),0,1).ToString();*/
    }



    void ManageMatchInput()
    {
        if (IsPlayerTouchingMatchStick() || isTouchingMatch)
        {
            //Debug.LogWarning(GetDotProductOfMatchAndStrikerStrip());
            UpdateMatchStickPosition();
        }
    }

    bool CheckIfPlayerWon()
    {
        return currentNumberOfValidMatchStrikes >= minimumValidMatchStrikesToWin;
    }
    public float strikeSpeedMultiplier = 30f;
    void ManageMatchAndStrikerInteraction()
    {
        float amountPlayerIsSwipingInValidDirection = Mathf.Clamp(GetDotProductOfMatchAndStrikerStrip(), 0f, 1f);
        if (IsMatchTipIsTouchingStrikerStrip())
        {
            Debug.LogError("attempting to add striker value");
            isStriking = true;
            //Debug.LogError((strikeSpeedMultiplier * Mathf.Abs(touchDelta.magnitude)) * Time.deltaTime);
            float swipeSpeedValue = swipeTimer != 0 ? Mathf.Abs(touchDelta.magnitude) / swipeTimer : 0;  //to avoid divide by zero error
            currentMatchStrikeValue += amountPlayerIsSwipingInValidDirection * (strikeSpeedMultiplier * swipeSpeedValue) * Time.deltaTime;

        }
        else if (isStriking && !IsMatchTipIsTouchingStrikerStrip() && isTouchingMatch)
        {
            isStriking = false;
            Debug.LogError("finished strike attempt. checking if player striked correctly");
            if (currentMatchStrikeValue > minimumValidMatchStrikeValue)
            {
                currentNumberOfValidMatchStrikes++;
            }
            currentMatchStrikeValue = 0;
        }
        
        
    }
    
    bool IsMatchTipIsTouchingStrikerStrip()
    {
        Vector2 screenSpacePositionOfMatchStickTip = RectTransformUtility.WorldToScreenPoint(UI.worldCamera, matchStickTip.position);
        var data = new PointerEventData(uiEventSystem) { position = screenSpacePositionOfMatchStickTip };
        var results = new List<RaycastResult>();
        UI.GetComponent<GraphicRaycaster>().Raycast(data, results);
        Debug.Log("found "+results.Count+" hits!");
        foreach (var r in results)
        {
            Debug.Log(r.gameObject.name);
            if (r.gameObject.transform == matchBoxStrikerStrip.transform ||
                r.gameObject.transform.IsChildOf(matchBoxStrikerStrip))
            {
                Debug.Log("Clicked UI!");
                return true;
            }
        }
        Debug.Log("MATCHTIP IS NOT TOUCHING");
        return false;
    }
    void UpdateMatchStickPosition()
    {
        if (Input.touchCount > 0)
        {
            if (Input.GetTouch(0).phase != TouchPhase.Ended &&
            Input.GetTouch(0).phase != TouchPhase.Canceled)
            {
                Vector2 screenPoint = Input.GetTouch(0).position;
                Vector2 localPoint;
                // Convert screen point to local point relative to parentRectTransform
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    GetComponent<RectTransform>(), //entire minigame transform
                    screenPoint,
                    UI.worldCamera, // Important: pass your canvas's camera
                    out localPoint))
                {
                    // Set anchored position of the target relative to parent
                    matchStick.anchoredPosition = localPoint;
                }
            }
        }
    }
    private bool IsPlayerTouchingMatchStick()
    {
        PointerEventData pointerData;
        if (IsPlayerTouchingScreen())
        {
            Debug.Log("Player touching screen");
            // Create a pointer event
            pointerData = new PointerEventData(uiEventSystem)
            {
                position = Input.GetTouch(0).position
            };
        }
        else
        {
            isTouchingMatch = false;
            return false;
        }

        // Raycast for UI hits
        List<RaycastResult> results = new List<RaycastResult>();
        UI.GetComponent<GraphicRaycaster>().Raycast(pointerData, results);

        // Check what was hit
        foreach (RaycastResult result in results)
        {
            //Debug.Log(result.gameObject.name);

            if (result.gameObject.CompareTag("Interactable"))
            {
                isTouchingMatch = true;
                return true;
            }
        }
        //Debug.LogError("couldn't find matchstick");
        return false;
    }
    
    private const float kMinSwipeSqr = 4f; // small deadzone, ~2px^2


    private bool IsPlayerTouchingScreen()
    {
        if (Input.touchCount > 0)
        {
            //Debug.LogWarning("player touched screen "+ Input.touchCount);
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    startTouchPosition = touch.position;
                    swipeTimerEnabled = false;
                    isSwiping = false;
                    return true;
                case TouchPhase.Moved:
                    // per-frame movement (screen space)
                    touchDelta = touch.deltaPosition;
                    // ignore tiny jitter
                    if (touchDelta.sqrMagnitude >= kMinSwipeSqr)
                    {
                        isSwiping = true;
                        return true;
                    }
                    break;
                case TouchPhase.Stationary:
                    swipeTimerEnabled = false;
                    isSwiping = false;
                    currentTouchPosition = touch.position;
                    //touchDelta = Vector2.zero;
                    return true;
                case TouchPhase.Ended:
                    swipeTimerEnabled = false;
                    isSwiping = false;
                    break;
                case TouchPhase.Canceled:
                    swipeTimerEnabled = false;
                    isSwiping = false;
                    touchDelta = Vector2.zero;
                    break;
            }
        }
        return false;
    }
    [SerializeField] private float swipeTimer = 0;
    [SerializeField]private bool swipeTimerEnabled;
    private void TimeSwipe()
    {
        if (isSwiping && isStriking && GetDotProductOfMatchAndStrikerStrip() > 0f) swipeTimerEnabled = true;
        else swipeTimerEnabled = false;
        if (swipeTimerEnabled)
            swipeTimer += Time.deltaTime;
        else
        {
            swipeTimer = 0;
        }
    }
    private float GetDotProductOfMatchAndStrikerStrip() //checks if player's swipe is in the same direction as the striker strip
    {
        // Get two world points: the origin of the UI and a point slightly up along its local "up" axis
        Vector3 worldPos = matchBoxStrikerStrip.position;
        Vector3 worldUpPoint = worldPos + matchBoxStrikerStrip.up * 50f; // arbitrary distance  ///IMPORTANT!!!!!!!!!!!!!!! GetChild(0) to get the striker instead of the wall

        // Convert both to screen space
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(UI.worldCamera, worldPos);
        Vector3 screenUpPos = RectTransformUtility.WorldToScreenPoint(UI.worldCamera, worldUpPoint);

        // Get the screen-space up vector
        Vector2 screenUpVector = (screenUpPos - screenPos).normalized;

        return Vector2.Dot(touchDelta.normalized, screenUpVector.normalized);
    }

    Coroutine minigameEndSequence = null;
    void WinGame()
    {
        minigameEndStateDebounce = true;
        Debug.LogWarning("player won");
        minigameEndSequence ??= StartCoroutine(WinGameCoroutine());
    }
    System.Collections.IEnumerator WinGameCoroutine()
    {
        yield return new WaitForSeconds(3);
        GameManager.Instance.GetService<MinigameManager>().PlayerWinMinigame();
        Destroy(transform.parent.gameObject);
    }
    void LoseGame()
    {
        minigameEndStateDebounce = true;
        Debug.LogError("player lost");
        minigameEndSequence ??= StartCoroutine(LoseGameCoroutine());
    }
    System.Collections.IEnumerator LoseGameCoroutine()
    {
        yield return new WaitForSeconds(3);
        GameManager.Instance.GetService<MinigameManager>().PlayerLoseMinigame();
        Destroy(transform.parent.gameObject);
    }
    void AdvanceTimers()
    {
        loseTimer += Time.deltaTime;
    }
}
