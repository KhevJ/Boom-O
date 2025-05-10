using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;

/// <summary>
/// our websocket logic
/// made as a singleton
/// </summary>
public class WebSocketManager : MonoBehaviour
{
    private static WebSocketManager instance; // singleton instance  
    public static WebSocketManager Instance => instance; // attribute to get instance

    private SocketIOUnity socket; // the socket
    public bool connected = false; // check if initial connection is set up properly

    public int roomLength = 0; // number of players in  the room
    public bool host = false; // if you are the room creator or not

    public string roomId; // id of room created or joined

    public string playerName; // name of player

    public string topCard; // discard pile card /top card of table

    public bool updateTopCard = false; // if we need to update top card

    public bool updateDeck = false; // when card is drawn

    public int wildcardColor = -1; //double check whether -1 is a color in card.cs

    public bool wildcardPlaced = false; // when wilcard everyone needs to know the color

    public bool allowedTurn = false; // if it is player's turn

    public List<string> deck;

    public List<string> playerCards;
    private string serverUrl = "http://localhost:3000/client";
    private Dictionary<int, string> serverDictionary = new() 
    {
        { 4, "http://localhost:3000/client" }, 
        { 3, "http://localhost:3001/client" },
        { 2, "http://localhost:3002/client" },
        { 1, "http://localhost:3003/client" },
        { 0, "http://localhost:3004/client" }
    }; // list of backend servers

   
    private int currentServerId = 4; // initial leader server 

    public Dictionary<string,int> CardCounts;
    

   // when class is instantiated
    void Awake()
    {
        
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize the socket using the current serverUrl.
        InitializeSocketIO(serverUrl);
    }


    /// <summary>
    /// initialize the sockets with socket client endpoints
    /// </summary>
    /// <param name="url"></param>
    void InitializeSocketIO(string url)
    {
        serverUrl = url;
        var uri = new Uri(serverUrl);
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new Dictionary<string, string>
                {
                    {"token", "UNITY" },
                    { "playerName", "Player" }
                },
            EIO = 4,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        })
        {

            JsonSerializer = new NewtonsoftJsonSerializer() // turns objects into json
        };

        ///// reserved socketio events
        socket.OnConnected += (sender, e) =>
        {
            connected = true;
            Debug.Log("socket.OnConnected" + e);
        };

        //receives a ping
        socket.OnPing += (sender, e) =>
        {
            //Debug.Log("Ping"); 
        };

        //receives a pong
        socket.OnPong += (sender, e) =>
        {
            //Debug.Log("Pong: " + e.TotalMilliseconds);
        };

        // when no pong is received
        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Log("disconnect: " + e);
            //socket = null;
            SwapServer();

            

        };
        


        // when a backend connection is made to acknowledge connection
        socket.On("welcome", (response) =>
        
        {
             var data = new Dictionary<string, object>
                {
                    { "roomId", roomId },
                    { "playerName", playerName }
                };

            Debug.Log("currnet server id " + currentServerId);
            if(currentServerId != -1   && currentServerId != 4){

                WelcomeBack(data);
            }
            
            int serverIdFromServer = response.GetValue<int>();
            
            if (currentServerId == -1)
            {
                currentServerId = serverIdFromServer;
            }
            else
            {
                // If the welcome event returns a different id, update the current server id.

               if (currentServerId != serverIdFromServer)
               {
                    SwapServer();

               }

            }
            // Log the welcome message.
            Debug.Log("Switched to new leader. Server id from welcome: " + currentServerId);
        });


        // when other player have drawn card , backend will tell you to update the deck
        socket.On("drawnCard", (response) =>
        {
            Debug.Log(response.GetValue<string>());
            updateDeck = true;
           
        });

         // when cards have been distributed across all players and the remaining cards is sent
        socket.On("deckSaved", (response) =>
        {

            deck = response.GetValue<List<string>>();
            Debug.Log("Server responded: Deck saved successfully! " + deck.Count);
        });

        // when you receive your hands
        socket.On("playerCardsSaved", (response) =>
        {

            playerCards = response.GetValue<List<string>>();
            Debug.Log("Server responded: Play Cards saved successfully! " + playerCards.Count);
           
        });

        // when other places a card on table
        socket.On("topCardUpdate", (response) =>
        {
            


            //trigger the update 
            topCard = response.GetValue<string>();
            updateTopCard = true;


            Debug.Log("Server responded: Top card saved successfully! " + topCard);
        });

        // to know if all players have joined the room
        socket.On("roomLength", (response) =>
        {

            //trigger the update 
            roomLength = response.GetValue<int>();


           
        });


        // when other players have played a wildcard, you need to know the color
        socket.On("wildcardColor", (response) => {
            wildcardColor = response.GetValue<int>();
            wildcardPlaced = true;
            Debug.Log(wildcardColor);
        });

        // when it is your turn to play
        socket.On("allowedTurn", (response) => {
            allowedTurn = true;
            Debug.Log("Hi from allowedTurn");
        });


        // to get other player's hand
        socket.On("updateCardCounts", (response) =>{
            CardCounts = response.GetValue<Dictionary<string, int>>();
            Debug.Log("Received updated card counts");
            //OnCardCountsUpdated?.Invoke(CardCounts);
        });

        socket.Connect();

        
    }


    /// <summary>
    /// code to swap server when leader dies
    /// </summary>
    private void SwapServer()
    {
        int nextServerId = GetNextServerId(currentServerId);
        if (serverDictionary.TryGetValue(nextServerId, out string nextUrl))
        {
            Debug.Log("Switching server from id " + currentServerId + " to " + nextServerId + " at URL: " + nextUrl);
            currentServerId = nextServerId;
            InitializeSocketIO(nextUrl);
        }
        else
        {
            Debug.LogWarning("No server URL mapped for server id " + nextServerId);
        }
    }

    /// <summary>
    ///  get the next server id
    /// </summary>
    /// <param name="current"></param>
    /// <returns></returns>
    private int GetNextServerId(int current)
    {
        int next = current - 1;
        if (next < 0)
            next = 4; // wrap-around to highest id
        return next;
    }


     public async void WelcomeBack(object data)
    {
        Debug.Log("In welcome back");
        await socket.EmitAsync("welcomeBack", response =>
        {
            if (response.Count > 0)
            {
                
                
            }
            else
            {
                Debug.LogWarning("No roomId received from server!");

            }
        }, data);
    }



    /// <summary>
    /// universal function to send data containing action and data whether string, int or object
    /// </summary>
    /// <param name="action"></param>
    /// <param name="data"></param>
    public void SendData(string action, object data)
    {

        if (socket != null && connected)
        {
            socket.Emit(action, data);
        }
        else
        {
            Debug.LogWarning("Socket.IO is not connected yet!");
        }
    }

    
    // send to backend create room selection with room id
    public async void CreateRoom(string roomID)
    {
        await socket.EmitAsync("createRoom", response =>
        {
            if (response.Count > 0)
            {
                roomId = response.GetValue<string>();
                playerName = response.GetValue<string>(1);
                Debug.Log("player Name " + playerName);
                host = true;
                Debug.Log("Room created with ID: " + roomId);
                //Debug.Log("Room created with ID: " + roomLength);
            }
            else
            {
                Debug.LogWarning("No roomId received from server!");

            }
        },roomID);
    }

    
    // send to backend join room selection with room id
    public async void JoinRoom(string roomID)
    {

        await socket.EmitAsync("joinRoom", response =>
        {
            if (response.Count > 0)
            {
                roomId = response.GetValue<string>();
                playerName = response.GetValue<string>(1);
                Debug.Log("player Name " + playerName);
                Debug.Log("Room joined with ID: " + roomId);
            }
            else
            {
                Debug.LogWarning("No roomId received from server!");

            }
        }, roomID);
    }



    // when application is closed , close the sockets
    private void OnApplicationQuit()
    {
        if (socket != null)
        {
            socket.Disconnect();
        }
    }
}

