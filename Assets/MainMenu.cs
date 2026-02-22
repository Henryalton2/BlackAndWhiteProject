using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
{
    SceneManager.LoadScene("lOADING"); // must match exactly
}
        public void QuitGame()
    {
        Debug.Log("Goodbye Traitor For Quitting");
        Application.Quit();
    }
}
