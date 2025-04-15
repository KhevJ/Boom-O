using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Menu : MonoBehaviour
{

    public GameObject waitPopup;
    public GameObject createRoomPopup;
    public GameObject joinRoomPopup;
    public InputField createRoomInput; //when user sets room id while creating
    public InputField joinRoomInput;//when user enters room id to join

    

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
            waitPopup.SetActive(false); //2 players have joined (can be changed later for 4)
            SceneManager.LoadScene("SampleScene");
        } 
    }   

    public void ShowCreateRoomPopup(){
        createRoomPopup.SetActive(true);
    }
    public void ShowJoinRoomPopup(){
        joinRoomPopup.SetActive(true);
    }

    public void CreateRoom(){
        string roomID = createRoomInput.text;
        if(!string.IsNullOrEmpty(roomID)){
            InitializeGame();
            WebSocketManager.Instance.CreateRoom(roomID);
            createRoomPopup.SetActive(false);
            waitPopup.SetActive(true);
        }

        //InitializeGame();
    }

    public void JoinRoom(){
        string roomID = joinRoomInput.text;
        if(!string.IsNullOrEmpty(roomID)){
            InitializeGame();
            WebSocketManager.Instance.JoinRoom(roomID);
            joinRoomPopup.SetActive(false);
            waitPopup.SetActive(true);
        }
        //InitializeGame();
    }

    public void closeCreateRoomPopup(){
        createRoomPopup.SetActive(false);
    }
    public void closeJoinRoomPopup(){
        joinRoomPopup.SetActive(false);
    }
}
