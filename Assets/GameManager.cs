using System.Collections;
using System.Collections.Generic;
using Mirror.Examples.MultipleMatch;
using UnityEditor.Tilemaps;
using UnityEngine;
using System.Linq;

//manages the game itself 
// has all the game resources
// decks, player hands, and table
public class GameManager : MonoBehaviour
{
    public GameObject cardPrefab; // a card
    public Transform playerCards; // hands of player zone 
    public Transform drawPile; // deck zone

    private List<Card> deck = new(); // deck that will be placed on draw pile
    private Dictionary<string, Sprite> cardSprites; // images of the front cards
    public Sprite cardBackSprite; // images of the back of cards

    //Used for Game logic
    public Card topCard; // the top card of the table
    public Transform discardPile; // table zone to place cards
    public GameObject colorPickerUI; // to chose a color when a wild card is placed (+4 card or multi color card)

    public Transform opponentCards; // the card of the opponents


    public List<KeyValuePair<string, GameObject>> UNODeckList = new(); // list to store object references of all cards in the game


    // when the actual game starts where the players have joined
    void Start()
    {
        StartCoroutine(InitializeGame()); // makes sure all the connections are setup such as the websockets
    }

   
    IEnumerator InitializeGame()
    {
        // wait for connection
        while (WebSocketManager.Instance == null || !WebSocketManager.Instance.connected || string.IsNullOrEmpty(WebSocketManager.Instance.roomId))
        {
            yield return null; // wait till connection is made
        }
        LoadCardSprites(); // loads all the card images
        InitializeDeck(); 

        //room creator only  does this
        if (WebSocketManager.Instance.host)
        {
            
            ShuffleDeck(); // shuffles the deck
            DealCards(); //edited that for top card only
            SendDeck();
        }


        while (WebSocketManager.Instance.deck.Count <= 0 || WebSocketManager.Instance.playerCards.Count <= 0 || string.IsNullOrEmpty(WebSocketManager.Instance.topCard))
        {
            yield return null;
        }

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
                //Debug.Log(cardScript.number);
                cardScript.transform.SetParent(discardPile);

                cardScript.transform.localPosition = Vector3.zero;
                if (cardScript.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
                {
                    spriteRenderer.sortingOrder = discardPile.childCount;
                }
                topCard = cardScript;
                cardScript.gameObject.SetActive(true);
                // Debug.Log(UNODeckList[i].Value.GetComponent<Card>().number);
                UNODeckList.RemoveAt(i);
                WebSocketManager.Instance.updateTopCard = false;
            }



        }
    
        
        ConvertStringToPlayerCards(WebSocketManager.Instance.playerCards);
        
        ConvertStringToDeckCards(WebSocketManager.Instance.deck); 
        
    }

 

    // converts objects of card to string names to send to the backend
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

    /// <summary>
    /// function to send the deck to the backend 
    /// </summary>
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


    /// <summary>
    /// to update opposing player hands
    /// </summary>
    /// <param name="counts">number fo card a player has</param>
    public void UpdateOpponentHandUI(Dictionary<string, int> counts){
        if(counts==null) return;

        string myPlayerName = WebSocketManager.Instance.playerName;
        int opponentCount = 0;
        foreach (var pair in counts){
            if(pair.Key!=myPlayerName){
                opponentCount += pair.Value;
            }
        }

        if(opponentCount!=opponentCards.childCount){
            foreach (Transform child in opponentCards)
                Destroy(child.gameObject);

            for (int i = 0; i < opponentCount; i++){
                Instantiate(cardPrefab, opponentCards);
            }
        }
        int opponentCardCount = opponentCards.childCount;
            if (opponentCardCount!=0){
                float spacing = Mathf.Min(0.3f,2.2f/(opponentCardCount));
                float startX = -((opponentCardCount-1)*spacing)/2;
                for(int i =0;i<opponentCardCount;i++){
                    Transform cardTransform = opponentCards.GetChild(i);
                    cardTransform.localPosition = new Vector3(startX+(i*spacing),0,0);
                }
            }
    }





    /// <summary>
    /// loading front or back card images for each card object
    /// </summary>
    void LoadCardSprites()
    {
        cardSprites = new Dictionary<string, Sprite>();

        Sprite[] loadedSprites = Resources.LoadAll<Sprite>("Sprites"); // Load all sprites in the "Resources/Sprites" folder

        foreach (Sprite sprite in loadedSprites)
        {
            cardSprites[sprite.name] = sprite; // Store in dictionary using name as the key
        }
    }


    /// <summary>
    /// deck is initialized and card is added to deck
    /// all cards are added to deck intially
    /// only the host does this
    /// the deck is then spliced so that each player gets 7 cards
    /// then the rest is sent to all the players which would be the deck
    /// </summary>
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


    /// <summary>
    /// add a card object to deck object
    /// </summary>
    /// <param name="color">color of card</param>
    /// <param name="type">type of card</param>
    /// <param name="number">number of card</param>
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

    /// <summary>
    /// shuffle the deck in a randomly fashion
    /// </summary>
    void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int randomIndex = Random.Range(i, deck.Count);
            (deck[i], deck[randomIndex]) = (deck[randomIndex], deck[i]);
        }
    }


    /// <summary>
    /// only the host will send the deck to backend to make all card are dealt appropriatley 
    /// </summary>
    void DealCards()
    {

        int index = 0;
        while (index < deck.Count && (deck[index].type != Card.CardType.Number))
        {
            index++; // Skip special cards
        }

      
        if (index < deck.Count)
        {
            topCard = deck[index];
            WebSocketManager.Instance.topCard = GetSpriteName(topCard.color, topCard.type, topCard.number); // update the top card of websocket
            var data = new Dictionary<string, object>
            {
                { "roomId", WebSocketManager.Instance.roomId },
                { "topCard",  GetSpriteName(topCard.color, topCard.type, topCard.number) },
                {"playerName", WebSocketManager.Instance.playerName},
                { "firstTime", "firstTime"}
            };
            WebSocketManager.Instance.SendData("sendTopCard", data); //send top card to server
            deck.RemoveAt(index);

            // the same instance has to be the card
            int j = 0;
            foreach (var pair in UNODeckList)
            {
                if (pair.Key == GetSpriteName(topCard.color, topCard.type, topCard.number) && pair.Value.Equals(topCard.gameObject))
                {
                    break;
                }
                j++;
            }
            UNODeckList.RemoveAt(j);
            Debug.Log(UNODeckList[j].Value.GetComponent<Card>().number);


        }
        else
        {
            Debug.LogError("No number cards found in the deck!");
            return; // Prevents further execution if there's an issue
        }

        
        topCard.transform.SetParent(discardPile);
        topCard.transform.localPosition = Vector3.zero;
        topCard.gameObject.SetActive(true);

    }


    /// <summary>
    /// get the string name of a card based on its attributes
    /// useful for sending to backend
    /// </summary>
    /// <param name="color"></param>
    /// <param name="type"></param>
    /// <param name="number"></param>
    /// <returns>string of card name</returns>
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

    /// <summary>
    /// realigns cards based when player draw a card so that if it fits the handzone
    /// </summary>
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

    /// <summary>
    /// function to initiate a draw from the deck
    /// </summary>

    public void DrawCard()
    {
        if (drawPile.childCount > 0 && WebSocketManager.Instance.allowedTurn) // Ensure there are cards left to draw
        {
            WebSocketManager.Instance.allowedTurn=false;
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
                    deck.RemoveAt(0);//remove deck at first index
                    var data = new Dictionary<string, object>
                    {
                        { "roomId", WebSocketManager.Instance.roomId },
                        { "drawnCard",  spriteName },
                        {"playerName" , WebSocketManager.Instance.playerName}
                    };

                    var turn_data = new Dictionary<string, object>
                    {
                        { "roomId", WebSocketManager.Instance.roomId },
                        {"playerName" , WebSocketManager.Instance.playerName}
                    };

                    WebSocketManager.Instance.SendData("drawCard", data); //sending  drawn card to server
                    WebSocketManager.Instance.SendData("updateTurnAccess", turn_data );
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


    /// <summary>
    /// will check if we can place a card on the discard pile 
    /// </summary>
    /// <param name="card">card object</param>
    /// <returns>bool</returns>
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


    /// <summary>
    /// setter function to update the top card
    /// </summary>
    /// <param name="newTopCard"></param>
    public void UpdateTopCard(Card newTopCard)
    {
        topCard = newTopCard;
    }


    /// <summary>
    /// will update the wild card color when a multi color card is selected and player made his color choice
    /// </summary>
    public void ChooseRed() { SetWildColor(Card.CardColor.Red); }
    public void ChooseBlue() { SetWildColor(Card.CardColor.Blue); }
    public void ChooseGreen() { SetWildColor(Card.CardColor.Green); }
    public void ChooseYellow() { SetWildColor(Card.CardColor.Yellow); }
    private void SetWildColor(Card.CardColor chosenColor)
    {
        Debug.Log("Wild card color chosen: " + chosenColor);

        // Set the chosen color for the wild card
        topCard.color = chosenColor;

        var data = new Dictionary<string, object>
                    {
                        { "roomId", WebSocketManager.Instance.roomId },
                        { "chosenColor", (int) chosenColor }
                    };

        //can do something here for wildcards
        WebSocketManager.Instance.SendData("wildcard", data);



        // This is the key fix: Change the type to allow normal cards
        topCard.type = Card.CardType.Number;

        colorPickerUI.SetActive(false); // Hide color picker UI
    }




    /// <summary>
    /// will check if backend has made any changes every frame
    /// </summary>
    void Update()
    {
        // opponent hand counts
        if (WebSocketManager.Instance.CardCounts != null)
            UpdateOpponentHandUI(WebSocketManager.Instance.CardCounts);

        //if top card is updated from other player
        if (WebSocketManager.Instance.updateTopCard && !string.IsNullOrEmpty(WebSocketManager.Instance.topCard) && topCard != null)
        {

            string cardName = WebSocketManager.Instance.topCard;
          
            
        
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
            if (cardObject == null)
            {

                Debug.Log("null in update top card");
            }
            Card cardScript = cardObject.GetComponent<Card>();
            Debug.Log(cardScript.number);
            Debug.Log(cardScript.type);
            cardScript.transform.SetParent(discardPile);

            cardScript.transform.localPosition = Vector3.zero;
            cardScript.gameObject.SetActive(true);
            if (cardScript.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            {
                spriteRenderer.sortingOrder = discardPile.childCount;
            }
            this.topCard = cardScript;

            UNODeckList.RemoveAt(i);
            WebSocketManager.Instance.updateTopCard = false;
         

        }


        //update the deck when drawing 
        // player can either draw for turn or put card for turn
        // Can do that later when making turn based logic
        // this is to be differentiated since the deck need to update when drawing
        // deck should be updated for everyone except sender(the one who draws)
        // Sender:
        // when a card is drawn, card is added to playerCards 
        // player cards for sender in backend needs to be updated 
        // deck needs to pop at the beginning
        // Other players:
        // when a card is drawn, each of their decks need to pop at the beginning 
        // remove from draw pile
        // you can also show the front card before setting it to inactive 
        // set active to false 
        // append the top card to UNODeckList
        
        if (WebSocketManager.Instance.updateDeck && deck != null)
        {

            Transform drawnCard = drawPile.GetChild(0);
            drawnCard.SetParent(null);

            Card cardScript = drawnCard.GetComponent<Card>();
            if (cardScript != null)
            {
                string spriteName = GetSpriteName(cardScript.color, cardScript.type, cardScript.number);
                if (cardSprites.ContainsKey(spriteName))
                {
                    deck.RemoveAt(0);//remove deck at first index
                    SpriteRenderer spriteRenderer = drawnCard.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.sprite = cardSprites[spriteName]; // Show the correct front sprite
                    }
                    cardScript.gameObject.SetActive(false);
                    UNODeckList.Add(new KeyValuePair<string, GameObject>(spriteName, cardScript.gameObject));
                    WebSocketManager.Instance.updateDeck = false;
                }
                else
                {
                    Debug.LogError($"Sprite not found: {spriteName}");
                }
            }



        }


        // if a wildcard is placed by other players and what color they chose
        if (WebSocketManager.Instance.wildcardPlaced && topCard != null)
        {
            // Debug.Log((Card.CardColor)WebSocketManager.Instance.wildcardColor);
            topCard.color = (Card.CardColor)WebSocketManager.Instance.wildcardColor; //nice explicit casting works, well hopefully
            WebSocketManager.Instance.wildcardPlaced = false;


        }


    }


    /// <summary>
    /// from list of strings from the backend to list of card objects for player cards
    /// </summary>
    /// <param name="cardNames"></param>
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
                    if (pair.Key == cardName && pair.Value != null)
                    {
                        cardObject = pair.Value;

                        break;
                    }
                    j += 1;
                }
                if (cardObject == null)
                {
                    Debug.Log(cardName);
                }

                Card cardScript = cardObject.GetComponent<Card>();
                cardObject.transform.SetParent(playerCards);
                cardObject.transform.localPosition = new Vector3(startX + (i * spacing), 0, 0);
                SpriteRenderer spriteRenderer = cardObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sortingOrder = i;
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




    }

    /// <summary>
    /// from list of strings from the backend to list of card objects for deck
    /// </summary>
    /// <param name="cardNames"></param>
    public void ConvertStringToDeckCards(List<string> cardNames)
    {
        // List<KeyValuePair<string, GameObject>> shallowDeckListCopy = UNODeckList.ToList();
        List<Card> newDeck = new();
        int i = 0;
        foreach (string cardName in cardNames)
        {
            int j = 0;
            if (cardSprites.ContainsKey(cardName))
            {

                GameObject cardObject = null;
                foreach (var pair in UNODeckList)
                {
                    if (pair.Key == cardName && pair.Value != null)
                    {
                        cardObject = pair.Value;

                        break;
                    }
                    j += 1;
                }
                if (cardObject == null)
                {

                    Debug.Log(cardName);
                }
                Card cardScript = cardObject.GetComponent<Card>();
                cardScript.transform.SetParent(drawPile);
                cardScript.transform.localPosition = Vector3.zero;
                SpriteRenderer spriteRenderer = cardScript.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = cardBackSprite;
                    spriteRenderer.sortingOrder = i;
                }
                cardScript.gameObject.SetActive(true);
                UNODeckList.RemoveAt(j);
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



    
}