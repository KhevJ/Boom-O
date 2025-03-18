using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
public class WebSocketManager : MonoBehaviour
{

    private static WebSocketManager instance; // the singleton
    public static WebSocketManager Instance => instance; //function to get the instance

    private WebSocket webSocket;
    public bool connected = false;
    private string serverUrl = "ws://35.90.5.38:3000/:3000";
    void Awake()
    {
        Debug.Log("Hello from websock");
        if (instance == null)
        {
            Debug.Log("Hello from websocket");
            instance = this;
            DontDestroyOnLoad(gameObject);  //make this object persistent when we change scemes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        StartCoroutine(InitializeWebSocket());
    }

    IEnumerator InitializeWebSocket()
    {
        webSocket = new WebSocket(serverUrl);
        connected = false;

        webSocket.OnOpen += () =>
        {
            Debug.Log("Connected to WebSocket server!");
            connected = true;
            SendConnectionMessage();
        };

        webSocket.OnError += (error) => Debug.LogError("WebSocket error: " + error);
        webSocket.OnClose += (e) => Debug.Log("WebSocket closed!");

        webSocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Message from server: " + message);
            HandleServerResponse(message);
        };

        webSocket.Connect();

        while (!connected)
        {
            yield return null;
        }
    }

    void Update()
    {
        if (webSocket != null)
        {
            webSocket.DispatchMessageQueue();
        }
    }

    public void SendText(string jsonData)
    {
        if (webSocket != null && connected)
        {
            webSocket.SendText(jsonData);
        }
        else
        {
            Debug.LogWarning("WebSocket is not connected yet!");
        }
    }

    private void SendConnectionMessage()
    {
        var connectionData = new
        {
            action = "connect",
            playerName = "Khevin The Goat"
        };
        string jsonData = JsonUtility.ToJson(connectionData);
        SendText(jsonData);
    }


    [System.Serializable]
    public class ServerResponse
    {
        public string action;
        public string message;
    }

    // Handle server response
    void HandleServerResponse(string message)
    {

        var response = JsonUtility.FromJson<ServerResponse>(message);

        if (response.action == "cardDrawn")
        {
            Debug.Log("Server responded: Card drawn successfully!");

        }
        if (response.action == "deckSaved")
        {
            Debug.Log("Server responded: Deck Saved successfully!");

        }
        if (response.action == "playerCardsSaved")
        {
            Debug.Log("Server responded: PlayerCards Saved successfully!");

        }
        else if (response.action == "error")
        {
            Debug.LogError("Error from server: " + response.message);
        }
    }


    private async void OnApplicationQuit()
    {
        if (webSocket != null)
        {
            await webSocket.Close();
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
