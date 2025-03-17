using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform playerCards;
    public Transform drawPile;

    private List<Card> deck = new List<Card>();
    private Dictionary<string, Sprite> cardSprites;
    public Sprite cardBackSprite;

    //Used for Game logic
    public Card topCard;
    public Transform discardPile;
    public GameObject colorPickerUI;

    // WebSocket
    private WebSocket webSocket;
    public bool connected = false;
    private string serverUrl = "ws://localhost:3000"; // Gotta change to maybe an array of links for replication

    [System.Serializable]
    public class ServerResponse
    {
        public string action;
        public string message;
    }

    void Start()
    {
        // Ensure WebSocket initializes first



        //InitializeWebSocket();
        StartCoroutine(InitializeGame());



    }

    IEnumerator InitializeGame()
    {
        yield return StartCoroutine(InitializeWebSocketCoroutine()); // Wait for connection

        LoadCardSprites();
        InitializeDeck();
        ShuffleDeck();
        DealCards();

        SendDeck();
        SendPlayerCards();
    }

    [System.Serializable]
    public class DeckData
    {
        public string action = "sendDeck";
        public List<string> cards; // COnvert Sprite Name to string
    }
    public void TestButton()
    {
        Debug.Log("Button Clicked!");
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

    [System.Serializable]
    public class PlayerCardsData
    {
        public string action = "sendPlayerCards";
        public List<string> cards;
    }

    void SendPlayerCards()
    {
        PlayerCardsData playerCardsData = new PlayerCardsData();
        playerCardsData.cards = new List<string>();

        foreach (Transform cardTransform in playerCards)
        {
            Card cardScript = cardTransform.GetComponent<Card>();
            if (cardScript != null)
            {
                string spriteName = GetSpriteName(cardScript.color, cardScript.type, cardScript.number);
                playerCardsData.cards.Add(spriteName);
            }
        }

        string jsonData = JsonUtility.ToJson(playerCardsData);
        Debug.Log("Sending player cards to server: " + jsonData);

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

        // Set the first card in discard pile
        topCard = deck[0];
        deck.RemoveAt(0);
        //if (topCard == null)
        //{
        //    Debug.LogError("Error: The first card in the deck is null.");
        //    return; // Prevents further execution if there's an issue
        //}
        topCard.transform.SetParent(discardPile);
        topCard.transform.localPosition = Vector3.zero;
        topCard.gameObject.SetActive(true);
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

    //Game Logic
    public bool CanPlaceCard(Card card)
    {
        // Wild cards are always playable
        if (card.type == Card.CardType.Wild || card.type == Card.CardType.WildDraw)
        {
            return true;
        }
        return card.color == topCard.color || card.number == topCard.number;
    }

    public void UpdateTopCard(Card newTopCard)
    {
        topCard = newTopCard;
    }

    public void ChooseRed() { SetWildColor(Card.CardColor.Red); }
    public void ChooseBlue() { SetWildColor(Card.CardColor.Blue); }
    public void ChooseGreen() { SetWildColor(Card.CardColor.Green); }
    public void ChooseYellow() { SetWildColor(Card.CardColor.Yellow); }
    public void SetWildColor(Card.CardColor chosenColor)
    {
        Debug.Log("Wild card color chosen: " + chosenColor);

        // Set the chosen color for the wild card
        topCard.color = chosenColor;

        // This is the key fix: Change the type to allow normal cards
        topCard.type = Card.CardType.Number;

        colorPickerUI.SetActive(false); // Hide color picker UI
    }

    IEnumerator InitializeWebSocketCoroutine()
    {
        webSocket = new WebSocket(serverUrl);

        connected = false;

        webSocket.OnOpen += () =>
        {
            Debug.Log("Connected to WebSocket server!");
            SendConnectionMessage();
            connected = true;

        };

        webSocket.OnError += (error) =>
        {
            Debug.LogError("WebSocket error: " + error);
        };

        webSocket.OnClose += (e) =>
        {
            Debug.Log("WebSocket closed!");
        };

        webSocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Message from server: " + message);
            HandleServerResponse(message);
        };

        webSocket.Connect(); // No await needed
        SendDeck();
        SendPlayerCards();

        while (!connected)
        {
            yield return null; // Wait until connected
        }
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