using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// when player logs in, he will be asked to choose or join a room
public class Menu : MonoBehaviour
{

    public GameObject waitPopup; // wait popup when choice is made and waiting for game to start
    public GameObject createRoomPopup; // wait popup when create choice is made and waiting for game to start
    public GameObject joinRoomPopup;// wait popup when join choice is made and waiting for game to start
    public InputField createRoomInput; //when user sets room id while creating
    public InputField joinRoomInput;//when user enters room id to join

    
    // function run at the start of the menu
    void Start()
    {
        StartCoroutine(InitializeGame()); // will check if websocket connection is made
        
    }

    IEnumerator InitializeGame()
    {
         // wait for connection
        while (WebSocketManager.Instance == null || !WebSocketManager.Instance.connected)
        {
            yield return null; // wait till connection is made
        }
       
    }

    //runs every frame checking if there are 2 players in a room
    void Update(){
        if(WebSocketManager.Instance.roomLength == 2){
            waitPopup.SetActive(false); //2 players have joined (can be changed for 4)
            SceneManager.LoadScene("SampleScene"); // will load actual game
        } 
    }   

    // will show create room popup with room id
    public void ShowCreateRoomPopup(){
        createRoomPopup.SetActive(true);
    }

    // will show join room popup with rooom id
    public void ShowJoinRoomPopup(){
        joinRoomPopup.SetActive(true);
    }

    // when create room is selected
    public void CreateRoom(){
        string roomID = createRoomInput.text; //prompts user for input to name a room
        if(!string.IsNullOrEmpty(roomID)){
            InitializeGame();
            WebSocketManager.Instance.CreateRoom(roomID);
            createRoomPopup.SetActive(false);
            waitPopup.SetActive(true);
        }

       
    }

    // when join room is selected 
    public void JoinRoom(){
        string roomID = joinRoomInput.text; // prompts user to input the room name
        if(!string.IsNullOrEmpty(roomID)){
            InitializeGame();
            WebSocketManager.Instance.JoinRoom(roomID);
            joinRoomPopup.SetActive(false);
            waitPopup.SetActive(true);
        }
        
    }

    // closing create popups
    public void closeCreateRoomPopup(){
        createRoomPopup.SetActive(false);
    }

    // closing join room popups
    public void closeJoinRoomPopup(){
        joinRoomPopup.SetActive(false);
    }
}
