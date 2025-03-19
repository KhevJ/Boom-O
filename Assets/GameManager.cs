using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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

    
    void Start()
    {
        StartCoroutine(InitializeGame());
    }

    IEnumerator InitializeGame()
    {
        // yield return StartCoroutine(InitializeWebSocketCoroutine()); // Wait for connection
        while (WebSocketManager.Instance == null || !WebSocketManager.Instance.connected)
        {
            yield return null; // wait till connection is made
        }
        LoadCardSprites();
        InitializeDeck();
        ShuffleDeck();
        DealCards();

        SendDeck();
        SendPlayerCards();
    }

    public void TestButton()
    {
        Debug.Log("Button Clicked!");
    }
    void SendDeck()
    {
        
        List<string> string_deck = new(); //names of cards as string Note can use new() to instanstiate
        


        foreach (Card card in deck)
        {
            string spriteName = GetSpriteName(card.color, card.type, card.number);
            string_deck.Add(spriteName);
        }

        
        WebSocketManager.Instance.SendData("sendDeck", string_deck );
    }

    

    void SendPlayerCards()
    {
    
        List<string> cards = new List<string>();

        foreach (Transform cardTransform in playerCards)
        {
            Card cardScript = cardTransform.GetComponent<Card>();
            if (cardScript != null)
            {
                string spriteName = GetSpriteName(cardScript.color, cardScript.type, cardScript.number);
                cards.Add(spriteName);
            }
        }


        WebSocketManager.Instance.SendData("sendPlayerCards",cards);
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
        //topCard = deck[0];
        //deck.RemoveAt(0);

        int index = 0;
        while (index < deck.Count && (deck[index].type != Card.CardType.Number))
        {
            index++; // Skip special cards
        }

        // If we found a valid number card, set it as topCard
        if (index < deck.Count)
        {
            topCard = deck[index];
            deck.RemoveAt(index);
        }
        else
        {
            Debug.LogError("No number cards found in the deck!");
            return; // Prevents further execution if there's an issue
        }

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
                    //SendDrawCardMessage(spriteName); //not too sure if this is right place to do that or above
                    WebSocketManager.Instance.SendData("drawCard", spriteName); //sending  drawn card to server
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

        }
        else
        {
            Debug.Log("No more cards left in the draw pile!");
        }
    }

    //Game Logic
    public bool CanPlaceCard(Card card)
    {
        Debug.Log($"Checking card placement: {card.color} {card.type} on {topCard.color} {topCard.type}");

        // Wild cards can always be played
        if (card.type == Card.CardType.Wild || card.type == Card.CardType.WildDraw)
        {
            return true;
        }

        // Number cards must match either color or number
        if (card.type == Card.CardType.Number && topCard.type == Card.CardType.Number)
        {
            return card.color == topCard.color || card.number == topCard.number;
        }

        // Action cards (Skip, Reverse, Draw) must match BOTH color AND type
        if (card.type != Card.CardType.Number && topCard.type != Card.CardType.Number)
        {
            bool validMove = (card.type == topCard.type) || (card.color == topCard.color);
            Debug.Log($"Action card move valid: {validMove}");
            return validMove;
        }

        // General case: allow only color matches
        bool validColorMatch = card.color == topCard.color;
        Debug.Log($"Color match valid: {validColorMatch}");
        return validColorMatch;
    }

    public void UpdateTopCard(Card newTopCard)
    {
        topCard = newTopCard;
    }

    public void ChooseRed() { SetWildColor(Card.CardColor.Red); }
    public void ChooseBlue() { SetWildColor(Card.CardColor.Blue); }
    public void ChooseGreen() { SetWildColor(Card.CardColor.Green); }
    public void ChooseYellow() { SetWildColor(Card.CardColor.Yellow); }
    private void SetWildColor(Card.CardColor chosenColor)
    {
        Debug.Log("Wild card color chosen: " + chosenColor);

        // Set the chosen color for the wild card
        topCard.color = chosenColor;

        // This is the key fix: Change the type to allow normal cards
        topCard.type = Card.CardType.Number;

        colorPickerUI.SetActive(false); // Hide color picker UI
    }

    private  void OnApplicationQuit()
    {
        // will prolly be recovering Logic
    }

}