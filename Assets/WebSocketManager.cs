using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;


public class WebSocketManager : MonoBehaviour
{
    private static WebSocketManager instance;
    public static WebSocketManager Instance => instance;

    private SocketIOUnity socket;
    public bool connected = false;

    public int roomLength = 0;
    public bool host = false;

    public string roomId;

    public string playerName;

    public string topCard;

    public bool updateTopCard = false;

    public bool updateDeck = false; // when card is drawn

    public int wildcardColor = -1; //double check whether -1 is a color in card.cs

    public bool wildcardPlaced = false; // when wilcard everyone needs to know the color

    public bool allowedTurn = false;

    public List<string> deck;

    public List<string> playerCards;
    private string serverUrl = "http://localhost:3000/client";
    private Dictionary<int, string> serverDictionary = new()
    {
        { 4, "http://localhost:3000/client" }, //Khevin's Server
        { 3, "http://localhost:3001/client" },
        { 2, "http://localhost:3002/client" },
        { 1, "http://localhost:3003/client" },
        { 0, "http://localhost:3004/client" }
    };

   
    private int currentServerId = 4;

    public Dictionary<string,int> CardCounts;
    //public event Action<Dictionary<string, int>> OnCardCountsUpdated;

    void Awake()
    {
        //Debug.Log("Hello from WebSocketManager");
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
        socket.OnPing += (sender, e) =>
        {
            //Debug.Log("Ping");
        };
        socket.OnPong += (sender, e) =>
        {
            //Debug.Log("Pong: " + e.TotalMilliseconds);
        };
        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Log("disconnect: " + e);
            //socket = null;
            SwapServer();

            //swap server
            //socket = null;
            // have global variable in WebsocketManager called serverId(4,3,2,1)
            // set that value to 3 say 4 crashed just decrementing
            // have a dictionary/ hash map that will hash the server id to the url
            // 
            // swapServer();

        };
        // socket.OnReconnectAttempt += (sender, e) =>
        // {
        //     Debug.Log($"{DateTime.Now} Reconnecting: attempt = {e}");
        // };



        socket.On("welcome", (response) =>
        //return currentServerId that will get from server
        // global var serverId from client (3) == currentId from server(2)
        // do the swap again
        // what if we have a controller
        // there is like a socket in between client and server
        // when there is a disconnection , socket becomes null
        // we can't talk to anyone

        {
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

        socket.On("drawnCard", (response) =>
        {
            Debug.Log(response.GetValue<string>());
            updateDeck = true;
            // Debug.Log("Server responded: Card drawn successfully!");
        });

        socket.On("deckSaved", (response) =>
        {

            deck = response.GetValue<List<string>>();
            Debug.Log("Server responded: Deck saved successfully! " + deck.Count);
        });

        socket.On("playerCardsSaved", (response) =>
        {

            playerCards = response.GetValue<List<string>>();
            Debug.Log("Server responded: Play Cards saved successfully! " + playerCards.Count);
            // Debug.Log("Server responded: Player cards saved successfully!");
        });

        socket.On("topCardUpdate", (response) =>
        {
            //Debug.Log(response.GetValue<string>());


            //trigger the update 
            topCard = response.GetValue<string>();
            updateTopCard = true;


            Debug.Log("Server responded: Top card saved successfully! " + topCard);
        });

        socket.On("roomLength", (response) =>
        {

            //trigger the update 
            roomLength = response.GetValue<int>();


            // Debug.Log("Server responded: Player cards saved successfully!");
        });


        socket.On("wildcardColor", (response) => {
            wildcardColor = response.GetValue<int>();
            wildcardPlaced = true;
            Debug.Log(wildcardColor);
        });

        socket.On("allowedTurn", (response) => {
            allowedTurn = true;
            Debug.Log("Hi from allowedTurn");
        });

        socket.On("updateCardCounts", (response) =>{
            CardCounts = response.GetValue<Dictionary<string, int>>();
            Debug.Log("Received updated card counts");
            //OnCardCountsUpdated?.Invoke(CardCounts);
        });

        socket.Connect();

        
    }

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

    private int GetNextServerId(int current)
    {
        int next = current - 1;
        if (next < 0)
            next = 4; // wrap-around to highest id
        return next;
    }

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



    private void OnApplicationQuit()
    {
        if (socket != null)
        {
            socket.Disconnect();
        }
    }
}


//public int serverId = -1; 
//public HashMap<int id, string url>


//Pseudocode for reconnection

//? welcome for socket socket.On("Welcome")
// ! if the serverId in welcome is -1  you just set the serverId to what the id the server sent

// ring algo did not announce the new leader to 3 yet
// and we are connecting to 3 on client
// 3 won't have the updated currentleader which wil be 4
// id from the server will be 4
// global var that will have be 4
// 4(serverId from client) == 4(currentLeader) a recursion till the id is different and not -1
// ! if the id(4) is the same as the one(4)(serverId -> client variable) that crashes do it recursively till the id is different
// if serverId is not -1 and diffent , then you call the swapServer  function again



// if there is an error do that maybe
//socket.onDisconnect{
//  socket = null;
//  swapServer();
//}

// swapServer(){
// 
//  InitializeSocket(int id);
//  serverId
// }

// oh i see what you can do now,
// on connection have a temporary varialbe

// if the leader crashes,
// ring algo starts
// if the ring is not done yet, the id that will sent to the client is not gonna be updated to the new value  of the new leader
// it is still gonna be crashed leader's id
// the UI is also gonna know 

