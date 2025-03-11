using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;

public class GameManager : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform playerCards;
    public Transform drawPile;

    private List<Card> deck = new List<Card>();
    private Dictionary<string, Sprite> cardSprites;
    public Sprite cardBackSprite;

    // WebSocket
    private WebSocket webSocket;
    private string serverUrl = "ws://localhost:3000"; // Change to your server's WebSocket URL

    [System.Serializable] // This allows JsonUtility to parse it
    public class ServerResponse
    {
        public string action; // Matches "action" field from JSON
        public string message; // Matches "message" field from JSON (if applicable)
    }

    void Start()
    {
        // Initialize WebSocket
        InitializeWebSocket(); // Remember this is an async function man 
        LoadCardSprites();
        InitializeDeck();
        ShuffleDeck();
        DealCards();

        // Adding deck to server
        SendDeck();


    }

    [System.Serializable]
    public class DeckData
    {
        public string action = "sendDeck"; // Define the action type
        public List<string> cards; // COnvert Sprite Name to string
    }
    void SendDeck()
    {
        DeckData deckData = new DeckData();
        deckData.cards = new List<string>();

        foreach (Card card in deck)
        {
            string spriteName = GetSpriteName(card.color, card.type, card.number);
            deckData.cards.Add(spriteName);
        }

        string jsonData = JsonUtility.ToJson(deckData);
        Debug.Log("Sending deck to server: " + jsonData);

        webSocket.SendText(jsonData);
    }

    

    void LoadCardSprites()
    {
        cardSprites = new Dictionary<string, Sprite>();

        Sprite[] loadedSprites = Resources.LoadAll<Sprite>("Sprites"); // Load all sprites in the "Resources/Sprites" folder

        foreach (Sprite sprite in loadedSprites)
        {
            cardSprites[sprite.name] = sprite; // Store in dictionary using name as the key
        }
    }

    void InitializeDeck()
    {
        Card.CardColor[] colors = { Card.CardColor.Red, Card.CardColor.Blue, Card.CardColor.Green, Card.CardColor.Yellow };
        Card.CardType[] specialCards = { Card.CardType.Skip, Card.CardType.Reverse, Card.CardType.Draw };
        foreach (Card.CardColor color in colors)
        {
            for (int number = 0; number <= 9; number++)
            {
                AddCardToDeck(color, Card.CardType.Number, number);
                if (number != 0)
                {
                    AddCardToDeck(color, Card.CardType.Number, number);
                }
            }
            foreach (Card.CardType type in specialCards)
            {
                AddCardToDeck(color, type, -1);
                AddCardToDeck(color, type, -1);
            }
        }

        for (int i = 0; i < 4; i++)
        {
            AddCardToDeck(Card.CardColor.Wild, Card.CardType.Wild, -1);
            AddCardToDeck(Card.CardColor.Wild, Card.CardType.WildDraw, -1);
        }
    }

    void AddCardToDeck(Card.CardColor color, Card.CardType type, int number)
    {
        GameObject cardObject = Instantiate(cardPrefab);
        Card card = cardObject.GetComponent<Card>();
        string spriteName = GetSpriteName(color, type, number);
        Sprite cardSprite = cardSprites.ContainsKey(spriteName) ? cardSprites[spriteName] : null;
        card.SetCardData(color, type, number, cardSprite);
        cardObject.name = spriteName;
        deck.Add(card);
        cardObject.SetActive(false);
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int randomIndex = Random.Range(i, deck.Count);
            (deck[i], deck[randomIndex]) = (deck[randomIndex], deck[i]);
        }
    }

    void DealCards()
    {
        float spacing = 0.3f;
        float startX = -((7 - 1) * spacing) / 2;
        for (int i = 0; i < 7; i++)
        {
            Card card = deck[0];
            deck.RemoveAt(0);
            card.transform.SetParent(playerCards);
            card.transform.localPosition = new Vector3(startX + (i * spacing), 0, 0);
            SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = i; // Leftmost = lowest order, Rightmost = highest
            }
            card.gameObject.SetActive(true);
        }

        for (int i = 0; i < deck.Count; i++)
        {
            Card card = deck[i];
            card.transform.SetParent(drawPile);
            card.transform.localPosition = Vector3.zero;
            SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = cardBackSprite;
                spriteRenderer.sortingOrder = i;
            }
            card.gameObject.SetActive(true);
        }
    }

    string GetSpriteName(Card.CardColor color, Card.CardType type, int number)
    {
        if (color == Card.CardColor.Wild)
        {
            return type == Card.CardType.WildDraw ? "Wild_Draw" : "Wild";
        }

        if (type == Card.CardType.Number)
        {
            return $"{color}_{number}"; // Example: "Blue_5", "Red_3"
        }
        else
        {
            return $"{color}_{type}"; // Example: "Green_Reverse", "Red_DrawTwo"
        }
    }

    public void RealignPlayerCards()
    {
        int cardCount = playerCards.childCount;
        if (cardCount == 0) return;

        float spacing = Mathf.Min(0.3f, 2.2f / (cardCount));
        float startX = -((cardCount - 1) * spacing) / 2; // Center cards

        for (int i = 0; i < cardCount; i++)
        {
            Transform cardTransform = playerCards.GetChild(i);
            cardTransform.localPosition = new Vector3(startX + (i * spacing), 0, 0);

            // Ensure correct order of cards visually (leftmost is lowest)
            SpriteRenderer spriteRenderer = cardTransform.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = i;
            }
        }
    }

    public void DrawCard()
    {
        if (drawPile.childCount > 0) // Ensure there are cards left to draw
        {
            Transform drawnCard = drawPile.GetChild(0); // Get the top card
            drawnCard.SetParent(playerCards); // Move it to the player's hand

            // Update card position and visibility
            RealignPlayerCards();

            // Reveal the correct front sprite
            Card cardScript = drawnCard.GetComponent<Card>();
            if (cardScript != null)
            {
                string spriteName = GetSpriteName(cardScript.color, cardScript.type, cardScript.number);
                if (cardSprites.ContainsKey(spriteName))
                {
                    SpriteRenderer spriteRenderer = drawnCard.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.sprite = cardSprites[spriteName]; // Show the correct front sprite
                    }
                }
                else
                {
                    Debug.LogError($"Sprite not found: {spriteName}");
                }
            }

            // Send draw card action to the server
            SendDrawCardMessage();
        }
        else
        {
            Debug.Log("No more cards left in the draw pile!");
        }
    }

    // Initialize WebSocket
    async void InitializeWebSocket()
    {
        webSocket = new WebSocket(serverUrl);

        // On Open
        webSocket.OnOpen += () =>
        {
            Debug.Log("Connected to WebSocket server!");
            SendConnectionMessage(); // Send a message when connected
        };

        // On Message received
        webSocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Message received from server: " + message);

            // You can handle different responses here
            // For example, if the server sends a draw card response
            HandleServerResponse(message);
        };

        // On Close
        webSocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
        };

        // On Error
        webSocket.OnError += (error) =>
        {
            Debug.LogError("WebSocket error: " + error);
        };

        // Start the WebSocket connection
        await webSocket.Connect();
    }

    // Send a message when a player connects to the server
    void SendConnectionMessage()
    {
        var connectionData = new
        {
            action = "connect",
            playerName = "Player1"
        };
        Debug.Log("sending connection: " + connectionData);
        string jsonData = JsonUtility.ToJson(connectionData);
        webSocket.SendText(jsonData);
    }



    [System.Serializable]
    public class DrawCardMessage
    {
        public string action;
        public string playerName;
    };
    // Send a message when the player draws a card
    void SendDrawCardMessage()
    {

        DrawCardMessage drawCardData = new DrawCardMessage
        {
            action = "drawCard",
            playerName = "Player1"
        };
        Debug.Log("sending draw: " + drawCardData);
        string jsonData = JsonUtility.ToJson(drawCardData);
        webSocket.SendText(jsonData);
    }

    // Handle server response
    void HandleServerResponse(string message)
    {

        var response = JsonUtility.FromJson<ServerResponse>(message);

        if (response.action == "cardDrawn")
        {
            Debug.Log("Server responded: Card drawn successfully!");

        }
        else if (response.action == "error")
        {
            Debug.LogError("Error from server: " + response.message);
        }
    }

    // Coroutine to keep the WebSocket connection alive
    IEnumerator WebSocketConnect()
    {

        while (webSocket.State != WebSocketState.Open)
        {
            yield return null;
        }

        Debug.Log("WebSocket is open and connected.");
    }


    void Update()
    {
        if (webSocket != null)
        {
            webSocket.DispatchMessageQueue();
        }
    }

    // Close WebSocket when the application quits
    private async void OnApplicationQuit()
    {
        await webSocket.Close();
    }

}