using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{

    public GameObject waitPopup;
    
    public void playGame(){
        waitPopup.SetActive(true);
    }

    public void closePopup(){
        waitPopup.SetActive(false);
    }

    public void quitGame(){
        Application.Quit();
        Debug.Log("quit game");
    }
}
