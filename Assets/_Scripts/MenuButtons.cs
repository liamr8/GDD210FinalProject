using System.Collections;
using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    public MainMenuBehavior mainMenuBehavior;
    public void StartGame()
    {
        mainMenuBehavior.StartGameFromGameManager();
    }

    public void ExitGame()
    {
        mainMenuBehavior.ExitGameFromGameManager();
    }   
}
