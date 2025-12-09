using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuBehavior : MonoBehaviour
{
    public Transform screenTransitionParent;
    public GameObject screenTransitionObject;

    public Button[] mainMenuButtons;

    GameObject transitionObject = null;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ManageMainMenuTransition("transitionStartMinigameInGame");
    }

    Coroutine transitionCoroutine;
    void ManageMainMenuTransition(String animationStateName)
    {
        transitionObject = Instantiate(screenTransitionObject, screenTransitionParent);
        transitionObject.GetComponent<Animator>().Play(animationStateName, 0, 0f);
        transitionCoroutine ??= StartCoroutine(OnMenuFirstLoadSequence());
    }

    public void StartGameFromGameManager()
    {
        transitionObject = Instantiate(screenTransitionObject, screenTransitionParent);
        SetButtonsEnabled(false);
        GameManager.Instance.LoadSceneFromGameManagerAsync("Scene0");
        transitionCoroutine ??= StartCoroutine(OnMenuButtonClickSequence(() => GameManager.Instance.ActivateScene()));
    }
    
    public void ExitGameFromGameManager()
    {
        transitionObject = Instantiate(screenTransitionObject, screenTransitionParent);
        SetButtonsEnabled(false);
        transitionCoroutine ??= StartCoroutine(OnMenuButtonClickSequence(() => GameManager.Instance.ExitGame()));
    }
    bool IsScreenTransitionFinished()
    {
        Animator animator = transitionObject.GetComponent<Animator>();
        return animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f;
    }
    IEnumerator OnMenuFirstLoadSequence()
    {
        yield return new WaitUntil(() => IsScreenTransitionFinished()); // Waits until transition animation finishes
        Destroy(transitionObject);
        SetButtonsEnabled(true);
        transitionCoroutine = null;
    }
    IEnumerator OnMenuButtonClickSequence(Action buttonBehavior)
    {
        Animator anim = transitionObject.GetComponent<Animator>();
        anim.Play("transitionStartMinigameFromBed", 0, 0f);
        anim.SetBool("PauseBeforeTransition", true);
        yield return new WaitUntil(() => IsScreenTransitionFinished());
        buttonBehavior?.Invoke();
        transitionCoroutine = null;
    }

    void SetButtonsEnabled(bool enabled)
    {
        foreach (Button btn in mainMenuButtons)
        {
            btn.interactable = enabled;
        }
    }

}
