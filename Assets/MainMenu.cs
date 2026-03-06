using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject continueButton;

    void Start()
    {
        // Only show continue button if a save file exists
        continueButton.SetActive(SaveSystem.SaveExists());
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("lOADING");
    }

    public void ContinueGame()
    {
        SceneManager.LoadScene("lOADING");
    }

    public void QuitGame()
    {
        Debug.Log("Goodbye Traitor For Quitting");
        Application.Quit();
    }
}
