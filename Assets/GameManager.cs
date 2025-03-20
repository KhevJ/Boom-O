using System.Collections;
using System.Collections.Generic;
using Mirror.Examples.MultipleMatch;
using UnityEditor.Tilemaps;
using UnityEngine;
using System.Linq;


public class GameManager : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform playerCards;
    public Transform drawPile;

    private List<Card> deck = new();
    private Dictionary<string, Sprite> cardSprites;
    public Sprite cardBackSprite;

    //Used for Game logic
    public Card topCard;
    public Transform discardPile;
    public GameObject colorPickerUI;

    public List<KeyValuePair<string, GameObject>> UNODeckList = new();


    void Start()
    {
        StartCoroutine(InitializeGame());
    }

    IEnumerator InitializeGame()
    {
        // yield return StartCoroutine(InitializeWebSocketCoroutine()); // wait for connection
        while (WebSocketManager.Instance == null || !WebSocketManager.Instance.connected || string.IsNullOrEmpty(WebSocketManager.Instance.roomId))
        {
            yield return null; // wait till connection is made
        }
        LoadCardSprites();
        InitializeDeck();

        //room creator does this
        if (WebSocketManager.Instance.host)
        {
            //InitializeDeck(); // should be when firt client joins
            ShuffleDeck(); // should be when firt client joins
            DealCards(); //edited that for top card only
            SendDeck();
        }


        while (WebSocketManager.Instance.deck.Count <= 0 || WebSocketManager.Instance.playerCards.Count <= 0 || string.IsNullOrEmpty(WebSocketManager.Instance.topCard))
        {
            yield return null;
        }
        //Debug.Log(WebSocketManager.Instance.topCard);

        //convert string of cards to deck
        // convert string of cards to playcards

        // ConvertStringToPlayerCards(WebSocketManager.Instance.playerCards);



        // convert top card to topCard
        if (!WebSocketManager.Instance.host)
        {
            string cardName = WebSocketManager.Instance.topCard;
            

            if (cardSprites.ContainsKey(cardName))
            {
                GameObject cardObject = null;
                int i = 0;
                foreach (var pair in UNODeckList)
                {
                    if (pair.Key == cardName)
                    {
                        
                        cardObject = pair.Value;

                        break;
                    }
                    i++;
                }
                Card cardScript = cardObject.GetComponent<Card>();
                Debug.Log(cardScript.number);
                cardScript.transform.SetParent(discardPile);

                cardScript.transform.localPosition = Vector3.zero;
                topCard = cardScript;
                cardScript.gameObject.SetActive(true);
                UNODeckList.RemoveAt(i);
            }
            
            
            
        }
        ConvertStringToPlayerCards(WebSocketManager.Instance.playerCards);
        ConvertStringToDeckCards(WebSocketManager.Instance.deck);









        // UpdateDrawPile();
        // UpdatePlayerCard();

        // DealCards();
        // host sends top card and shuffled deck

        // Deal Cards() include sending top card to server

        // SendDeck(); // should be when first client joins
        // find a way to set deck when other clients join
        // SendPlayerCards();
    }

    public void TestButton()
    {
        Debug.Log("Button Clicked!");
    }

    public List<string> ConvertCardstoString(List<Card> cards)
    {
        List<string> string_cards = new();
        foreach (Card card in cards)
        {
            string spriteName = GetSpriteName(card.color, card.type, card.number);
            string_cards.Add(spriteName);
        }
        return string_cards;

    }
    void SendDeck()
    {

        List<string> string_deck = ConvertCardstoString(deck); //names of cards as string Note: can use new() to instanstiate
        var data = new Dictionary<string, object>
        {
            { "roomId", WebSocketManager.Instance.roomId },
            { "deck", string_deck }
        };
        WebSocketManager.Instance.SendData("sendDeck", data);
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


        WebSocketManager.Instance.SendData("sendPlayerCards", cards);
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
        UNODeckList.Add(new KeyValuePair<string, GameObject>(spriteName, cardObject));

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

        //Note: change this Khevin so that only host does this.
        // If we found a valid number card, set it as topCard
        if (index < deck.Count)
        {
            topCard = deck[index];
            WebSocketManager.Instance.topCard = GetSpriteName(topCard.color, topCard.type, topCard.number); // update the top card of websocket
            var data = new Dictionary<string, object>
            {
                { "roomId", WebSocketManager.Instance.roomId },
                { "topCard",  GetSpriteName(topCard.color, topCard.type, topCard.number) }
            };
            WebSocketManager.Instance.SendData("sendTopCard", data); //send top card to server
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
        // for (int i = 0; i < 7; i++)
        // {
        //     Card card = deck[0];
        //     deck.RemoveAt(0);
        //     card.transform.SetParent(playerCards);
        //     card.transform.localPosition = new Vector3(startX + (i * spacing), 0, 0);
        //     SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
        //     if (spriteRenderer != null)
        //     {
        //         spriteRenderer.sortingOrder = i; // Leftmost = lowest order, Rightmost = highest
        //     }
        //     card.gameObject.SetActive(true);
        // }

        // for (int i = 0; i < deck.Count; i++)
        // {
        //     Card card = deck[i];
        //     card.transform.SetParent(drawPile);
        //     card.transform.localPosition = Vector3.zero;
        //     SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
        //     if (spriteRenderer != null)
        //     {
        //         spriteRenderer.sprite = cardBackSprite;
        //         spriteRenderer.sortingOrder = i;
        //     }
        //     card.gameObject.SetActive(true);
        // }

    }

    public string GetSpriteName(Card.CardColor color, Card.CardType type, int number)
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



    // void Update()
    // {

    //     if (WebSocketManager.Instance.topCard != GetSpriteName(this.topCard.color, this.topCard.type, this.topCard.number))
    //     {
    //         //this.topCard shoudl become what the WebSocketManager is
    //     }

    //     //implement same deck across all users
    //     //this one is gonna be hard man
    //     //why am I doing this, man
    //     // Host sends deck to backend 
    //     // backend assigns player cards to all players
    //     // top card on UI should be fine Double check that 
    //     // each one will receive their player cards
    //     // deck is stored and sent to all players
    //     // deck of string cards need to get converted to cards
    //     // playerCards of string cards need to get converted to cards



    //     //implement turn based feature
    //     // do that after synchronizing deck


    // }


    public void ConvertStringToPlayerCards(List<string> cardNames)
    {
        float spacing = 0.3f;
        float startX = -((7 - 1) * spacing) / 2;
        int i = 0;

        foreach (string cardName in cardNames)
        {

            if (cardSprites.ContainsKey(cardName))
            {
                
        
                GameObject cardObject = null;
                int j = 0;
                foreach (var pair in UNODeckList)
                {
                    if (pair.Key == cardName  && pair.Value != null)
                    {
                        cardObject = pair.Value;

                        break;
                    }
                    j+=1;
                }
                if(cardObject == null ){
                    
                    Debug.Log(cardName);
                }

                Card cardScript = cardObject.GetComponent<Card>();

                // Extract color and type from the sprite name
                // string[] parts = cardName.Split('_');
                // Card.CardColor color = (Card.CardColor)System.Enum.Parse(typeof(Card.CardColor), parts[0]);

                // Card.CardType type;
                // int number = -1;

                // if (parts.Length == 2 && int.TryParse(parts[1], out number))
                // {
                //     type = Card.CardType.Number;
                // }
                // else
                // {
                //     type = (Card.CardType)System.Enum.Parse(typeof(Card.CardType), parts[1]);
                // }

                // cardScript.SetCardData(color, type, number, cardSprites[cardName]);

                cardObject.transform.SetParent(playerCards);
                cardObject.transform.localPosition = new Vector3(startX + (i * spacing), 0, 0);
                SpriteRenderer spriteRenderer = cardObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sortingOrder = i; // Leftmost = lowest order, Rightmost = highest
                }
                cardScript.gameObject.SetActive(true);
                UNODeckList.RemoveAt(j);
            }
            else
            {
                Debug.LogError($"Card sprite not found for: {cardName}");
            }
            i += 1;
        }

        //UNODeckList.RemoveAt(i-1);


    }

    public void ConvertStringToDeckCards(List<string> cardNames)
    {
        List<KeyValuePair<string, GameObject>> shallowDeckListCopy = UNODeckList.ToList();
        List<Card> newDeck = new();
        int i = 0;
        foreach (string cardName in cardNames)
        {
            int j =0;
            if (cardSprites.ContainsKey(cardName))
            {

                GameObject cardObject = null;
                foreach (var pair in shallowDeckListCopy)
                {
                    if (pair.Key == cardName && pair.Value != null)
                    {
                        cardObject = pair.Value;
                        
                        break;
                    }
                    j+=1;
                }
                if(cardObject == null ){
                    
                    Debug.Log(cardName);
                }
                Card cardScript = cardObject.GetComponent<Card>();

                // Extract color and type from the sprite name
                // string[] parts = cardName.Split('_');
                // Card.CardColor color = (Card.CardColor)System.Enum.Parse(typeof(Card.CardColor), parts[0]);

                // Card.CardType type;
                // int number = -1;

                // if (parts.Length == 2 && int.TryParse(parts[1], out number))
                // {
                //     type = Card.CardType.Number;
                // }
                // else
                // {
                //     type = (Card.CardType)System.Enum.Parse(typeof(Card.CardType), parts[1]); //here
                // }

                // cardScript.SetCardData(color, type, number, cardSprites[cardName]);

                cardScript.transform.SetParent(drawPile);
                cardScript.transform.localPosition = Vector3.zero;
                SpriteRenderer spriteRenderer = cardScript.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = cardBackSprite;
                    spriteRenderer.sortingOrder = i;
                }
                cardScript.gameObject.SetActive(true);
                shallowDeckListCopy.RemoveAt(j);
                newDeck.Add(cardScript);
            }
            else
            {
                Debug.LogError($"Card sprite not found for: {cardName}");
            }
            i += 1;

        }
        deck = newDeck;
        


    }

    // public void UpdatePlayerCard()
    // {
    //     float spacing = 0.3f;
    //     float startX = -((7 - 1) * spacing) / 2;
    //     for (int i = 0; i < 7; i++)
    //     {
    //         Card card = deck[0];
    //         deck.RemoveAt(0);
    //         card.transform.SetParent(playerCards);
    //         card.transform.localPosition = new Vector3(startX + (i * spacing), 0, 0);
    //         SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
    //         if (spriteRenderer != null)
    //         {
    //             spriteRenderer.sortingOrder = i; // Leftmost = lowest order, Rightmost = highest
    //         }
    //         card.gameObject.SetActive(true);

    //     }
    // }

    // public void UpdateDeck()
    // {
    //     for (int i = 0; i < deck.Count; i++)
    //     {
    //         Card card = deck[i];
    //         card.transform.SetParent(drawPile);
    //         card.transform.localPosition = Vector3.zero;
    //         SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
    //         if (spriteRenderer != null)
    //         {
    //             spriteRenderer.sprite = cardBackSprite;
    //             spriteRenderer.sortingOrder = i;
    //         }
    //         card.gameObject.SetActive(true);
    //     }
    // }



    private void OnApplicationQuit()
    {
        // will prolly be recovering Logic
    }

}