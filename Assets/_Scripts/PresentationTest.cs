using UnityEngine;

public class PresentationTest : MonoBehaviour
{
    public GameObject minigame;
    [SerializeField]float timer = 0;
    public float timeDuration = 5;
    private bool debounce = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!debounce)
            ManagePresentation();
        else if (debounce && !minigame.activeSelf)
        {
            timer = 0;
            debounce = false;
        }
    }

    void ManagePresentation(){
        if(timer > timeDuration){
            if (!minigame.activeSelf)
                minigame.SetActive(true);
            debounce = true;
            return;
        }
        timer += Time.deltaTime;
    }
}
