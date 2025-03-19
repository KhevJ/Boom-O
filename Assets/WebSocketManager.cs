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

    public string topCard;
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

        InitializeSocketIO(); // would normally be triggered by first scene
    }

    void InitializeSocketIO()
    {
        var uri = new Uri(serverUrl);
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new Dictionary<string, string>
                {
                    {"token", "UNITY" },
                    { "playerName", "Khevin The Goat" }
                }
            ,
            EIO = 4
            ,
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
        };
        socket.OnReconnectAttempt += (sender, e) =>
        {
            Debug.Log($"{DateTime.Now} Reconnecting: attempt = {e}");
        };
        


        socket.On("welcome", (response) => {
            Debug.Log(response.GetValue<string>()); 
            
        });
        
        socket.On("drawnCard", (response) =>
        {
            Debug.Log(response.GetValue<string>()); 
            // Debug.Log("Server responded: Card drawn successfully!");
        });

        socket.On("deckSaved", (response) =>
        {
            Debug.Log(response.GetValue<string>());
            // Debug.Log("Server responded: Deck saved successfully!");
        });

        socket.On("playerCardsSaved", (response) =>
        {
            Debug.Log(response.GetValue<string>());
            // Debug.Log("Server responded: Player cards saved successfully!");
        });

        socket.On("topCardUpdate", (response) =>
        {
            Debug.Log(response.GetValue<string>());
            
            
            //trigger the update 
            topCard = response.GetValue<string>();
            
            
            // Debug.Log("Server responded: Player cards saved successfully!");
        });

        socket.On("roomLength", (response) =>
        {
            Debug.Log(response.GetValue<string>());
            
            
            //trigger the update 
            roomLength = response.GetValue<int>();
            
            
            // Debug.Log("Server responded: Player cards saved successfully!");
        });

        
        socket.Connect();
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

    public async void CreateRoom()
    {
        await socket.EmitAsync("createRoom",  response =>
        {
            if (response.Count > 0)
            {
                roomId = response.GetValue<string>();
                host = true;
                Debug.Log("Room created with ID: " + roomId);
                Debug.Log("Room created with ID: " + roomLength);
            }
            else
            {
                Debug.LogWarning("No roomId received from server!");
                
            }
        });
    }

    public async void JoinRoom(string room="Khevin's Room")
    {
        
        await socket.EmitAsync("joinRoom",  response =>
        {
            if (response.Count > 0)
            {
                roomId = response.GetValue<string>();
                if(roomId == "Error") Application.Quit();
                
                Debug.Log("Room joined with ID from join: " + roomId);
            }
            else
            {
                Debug.LogWarning("No roomId received from server!");
                
            }
        }, room);
    }



    private void OnApplicationQuit()
    {
        if (socket != null)
        {
            socket.Disconnect();
        }
    }
}

