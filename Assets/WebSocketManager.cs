// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using NativeWebSocket;
// public class WebSocketManager : MonoBehaviour
// {

//     private static WebSocketManager instance; // the singleton
//     public static WebSocketManager Instance => instance; //function to get the instance

//     private WebSocket webSocket;
//     public bool connected = false;
//     private string serverUrl = "ws://localhost:3000";
//     void Awake()
//     {
//         Debug.Log("Hello from websock");
//         if (instance == null)
//         {
//             Debug.Log("Hello from websocket");
//             instance = this;
//             DontDestroyOnLoad(gameObject);  //make this object persistent when we change scemes
//         }
//         else
//         {
//             Destroy(gameObject);
//             return;
//         }

//         StartCoroutine(InitializeWebSocket());
//     }

//     IEnumerator InitializeWebSocket()
//     {
//         webSocket = new WebSocket(serverUrl);
//         connected = false;

//         webSocket.OnOpen += () =>
//         {
//             Debug.Log("Connected to WebSocket server!");
//             connected = true;
//             SendConnectionMessage();
//         };

//         webSocket.OnError += (error) => Debug.LogError("WebSocket error: " + error);
//         webSocket.OnClose += (e) => Debug.Log("WebSocket closed!");

//         webSocket.OnMessage += (bytes) =>
//         {
//             string message = System.Text.Encoding.UTF8.GetString(bytes);
//             Debug.Log("Message from server: " + message);
//             HandleServerResponse(message);
//         };

//         webSocket.Connect();

//         while (!connected)
//         {
//             yield return null;
//         }
//     }

//     void Update()
//     {
//         if (webSocket != null)
//         {
//             webSocket.DispatchMessageQueue();
//         }
//     }

//     public void SendText(string jsonData)
//     {
//         if (webSocket != null && connected)
//         {
//             webSocket.SendText(jsonData);
//         }
//         else
//         {
//             Debug.LogWarning("WebSocket is not connected yet!");
//         }
//     }

//     private void SendConnectionMessage()
//     {
//         var connectionData = new
//         {
//             action = "connect",
//             playerName = "Khevin The Goat"
//         };
//         string jsonData = JsonUtility.ToJson(connectionData);
//         SendText(jsonData);
//     }


//     [System.Serializable]
//     public class ServerResponse
//     {
//         public string action;
//         public string message;
//     }

//     // Handle server response
//     void HandleServerResponse(string message)
//     {

//         var response = JsonUtility.FromJson<ServerResponse>(message);

//         if (response.action == "cardDrawn")
//         {
//             Debug.Log("Server responded: Card drawn successfully!");

//         }
//         if (response.action == "deckSaved")
//         {
//             Debug.Log("Server responded: Deck Saved successfully!");

//         }
//         if (response.action == "playerCardsSaved")
//         {
//             Debug.Log("Server responded: PlayerCards Saved successfully!");

//         }
//         else if (response.action == "error")
//         {
//             Debug.LogError("Error from server: " + response.message);
//         }
//     }


//     private async void OnApplicationQuit()
//     {
//         if (webSocket != null)
//         {
//             await webSocket.Close();
//         }
//     }
// }


using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;
// using Newtonsoft.Json.Linq;





public class WebSocketManager : MonoBehaviour
{
    private static WebSocketManager instance;
    public static WebSocketManager Instance => instance;

    private SocketIOUnity socket;
    public bool connected = false;
    private readonly string serverUrl = "http://localhost:3000";

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

        InitializeSocketIO();
    }

    void InitializeSocketIO()
    {
        var uri = new Uri(serverUrl);
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new Dictionary<string, string>
                {
                    {"token", "UNITY" }
                }
            ,
            EIO = 4
            ,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        })
        {
            // Handle Connection
            JsonSerializer = new NewtonsoftJsonSerializer()
        };

        ///// reserved socketio events
        socket.OnConnected += (sender, e) =>
        {
            connected = true;
            SendConnectionMessage();
            Debug.Log("socket.OnConnected" + e);
        };
        socket.OnPing += (sender, e) =>
        {
            Debug.Log("Ping");
        };
        socket.OnPong += (sender, e) =>
        {
            Debug.Log("Pong: " + e.TotalMilliseconds);
        };
        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Log("disconnect: " + e);
        };
        socket.OnReconnectAttempt += (sender, e) =>
        {
            Debug.Log($"{DateTime.Now} Reconnecting: attempt = {e}");
        };
        ////

        Debug.Log("Connecting...");
        socket.Connect();

        // socket.OnUnityThread("spin", (data) =>
        // {
        //     rotateAngle = 0;
        // });

        // ReceivedText.text = "";
        // socket.OnAnyInUnityThread((name, response) =>
        // {
        //     ReceivedText.text += "Received On " + name + " : " + response.GetValue().GetRawText() + "\n";
        // });

        // Listen for messages
        socket.On("cardDrawn", (response) =>
        {
            Debug.Log("Server responded: Card drawn successfully!");
        });

        socket.On("deckSaved", (response) =>
        {
            Debug.Log("Server responded: Deck saved successfully!");
        });

        socket.On("playerCardsSaved", (response) =>
        {
            Debug.Log("Server responded: Player cards saved successfully!");
        });

        // Connect to the server
        socket.Connect();
    }

    public void SendText(string action, object data)
    {
        // Debug.Log(data);
        // Debug.Log(action);
        //string jsonData = JsonUtility.ToJson(data);
        if (socket != null && connected)
        {
            socket.EmitAsync("drawCard", "world");
        }
        else
        {
            Debug.LogWarning("Socket.IO is not connected yet!");
        }
    }

    private void SendConnectionMessage()
    {
        var connectionData = new
        {
            action = "connect",
            playerName = "Khevin The Goat"
        };
        SendText("connect", "Khevin The Goat");
    }

    private void OnApplicationQuit()
    {
        if (socket != null)
        {
            socket.Disconnect();
        }
    }
}



// // Start is called before the first frame update
// void Start()
// {
//     //let's go singleton pattern
// }

// // Update is called once per frame
// void Update()
// {

// }
//
