using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{

    public GameObject waitPopup;

    

    void Start()
    {
        StartCoroutine(InitializeGame());
        
    }

    IEnumerator InitializeGame()
    {
        // yield return StartCoroutine(InitializeWebSocketCoroutine()); // wait for connection
        while (WebSocketManager.Instance == null || !WebSocketManager.Instance.connected)
        {
            yield return null; // wait till connection is made
        }
       
    }

    void Update(){
        if(WebSocketManager.Instance.roomLength == 2){
            waitPopup.SetActive(false); //players have joined
            SceneManager.LoadScene("SampleScene");
        } 
    }   

    public void playGame()
    {
        InitializeGame();
        WebSocketManager.Instance.CreateRoom();
        waitPopup.SetActive(true);
        // Load the game scene after other players join **not done yet Done now
        // SceneManager.LoadScene("SampleScene");
    }

    public void closePopup()
    {
        //waitPopup.SetActive(false);
    }

    // think of this as join
    public void quitGame()
    {
        InitializeGame();
        WebSocketManager.Instance.JoinRoom();
        waitPopup.SetActive(true);
        // Application.Quit();
        Debug.Log("quit game");
    }
}
