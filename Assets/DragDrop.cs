using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//property that each card will have
// basically to allow them to be moved around 
// for e.g drawing or placing cards on the table
public class DragDrop : MonoBehaviour
{

    private Vector3 startposition; // startposition of the card , to know where the card is initially at
    public Transform discardPile; // where the cards are placed on the table
    public GameManager gameManager; // the one that handles the games 


    // unity function that will run when the DragDrop property is instantiated in each card
    void Start()
    {
        discardPile = GameObject.Find("DiscardPile").transform;
        gameManager = FindObjectOfType<GameManager>();
        startposition = transform.position;
    }
        

    // for clicking on cards for example in deck and in player hand
    // basically when you press on the mouse button
    private void OnMouseDown()
    {
        if (transform.parent.name == "PlayerCards")
        {
            startposition = transform.position;
            transform.position = GetMousePositionInWorldSpace();
        }
        if (transform.parent.name == "DrawPile")
        {
            gameManager.DrawCard();
        }

    }

    // when you drag a card from player cards to discard pile
    private void OnMouseDrag()
    {
        if (transform.parent.name == "PlayerCards")
        {
            transform.position = GetMousePositionInWorldSpace();
        }
    }

    // to releases cards from player hands to the discard pile
    private void OnMouseUp()
    {
        if (transform.parent.name == "PlayerCards")
        {
            float distance = Vector3.Distance(transform.position, discardPile.position);
            if (distance < 0.6f)
            {
                Card currentCard = GetComponent<Card>();
                if (gameManager.CanPlaceCard(currentCard) && WebSocketManager.Instance.allowedTurn)
                {
                    WebSocketManager.Instance.allowedTurn=false;
                    transform.SetParent(discardPile);
                    WebSocketManager.Instance.topCard = gameManager.GetSpriteName(currentCard.color, currentCard.type, currentCard.number); // update the top card of websocket
                    var data = new Dictionary<string, object>
                    {
                        { "roomId", WebSocketManager.Instance.roomId },
                        { "topCard", gameManager.GetSpriteName(currentCard.color, currentCard.type, currentCard.number)},
                        {"playerName" , WebSocketManager.Instance.playerName}
                    };
                    var turn_data = new Dictionary<string, object>
                    {
                        { "roomId", WebSocketManager.Instance.roomId },
                        {"playerName" , WebSocketManager.Instance.playerName}
                    };
                    WebSocketManager.Instance.SendData("sendTopCard", data); //send top card to server meaning to everyone except sender
                    WebSocketManager.Instance.SendData("updateTurnAccess", turn_data);
                    transform.localPosition = Vector3.zero;

                    SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.sortingOrder = discardPile.childCount;
                    }

                    gameManager.UpdateTopCard(currentCard);
                    gameManager.RealignPlayerCards();

                    if (currentCard.type == Card.CardType.Wild || currentCard.type == Card.CardType.WildDraw)
                    {
                        gameManager.colorPickerUI.SetActive(true); // Show color selection UI
                    }
                }
                else
                {
                    Debug.Log("Invalid move! Card must match color or number.");
                    transform.position = startposition;
                }
            }
            else
            {
                transform.position = startposition;
            }
        }


    }

    // to calculate the mouse position used for updating top card position when dragging the card
    // from player hands
    public Vector3 GetMousePositionInWorldSpace()
    {
        Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        position.z = 0f;
        return position;
    }


}
