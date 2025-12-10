using System.Collections;
using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    public MainMenuBehavior mainMenuBehavior;
    public AudioSource buttonSoundSource;
    public void StartGame()
    {
        mainMenuBehavior.StartGameFromGameManager();
        buttonSoundSource.pitch = 1.0f;
        buttonSoundSource.Play();
    }

    public void ExitGame()
    {
        mainMenuBehavior.ExitGameFromGameManager();
        buttonSoundSource.pitch = 0.8f;
        buttonSoundSource.Play();
    }   
}
