using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{

    public GameObject waitPopup;
    
    public void playGame(){
        waitPopup.SetActive(true);
        // Load the game scene after other players join **not done yet
        SceneManager.LoadScene("SampleScene");
    }

    public void closePopup(){
        waitPopup.SetActive(false);
    }

    public void quitGame(){
        Application.Quit();
        Debug.Log("quit game");
    }
}
