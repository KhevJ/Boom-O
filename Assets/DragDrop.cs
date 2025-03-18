using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragDrop : MonoBehaviour
{
    
    private Vector3 startposition;
    public Transform discardPile;
    public GameManager gameManager;

    int order = 0;
    
    void Start(){
        discardPile = GameObject.Find("DiscardPile").transform;
        gameManager = FindObjectOfType<GameManager>();
        startposition=transform.position;
    }

    private void OnMouseDown(){
        if(transform.parent.name=="PlayerCards"){
            startposition=transform.position;
            transform.position = GetMousePositionInWorldSpace();
        }
        if(transform.parent.name=="DrawPile"){
            gameManager.DrawCard();
        }
        
    }

    private void OnMouseDrag(){
        if(transform.parent.name=="PlayerCards"){
            transform.position = GetMousePositionInWorldSpace();
        }
    }

    private void OnMouseUp(){
        if(transform.parent.name=="PlayerCards"){
            float distance = Vector3.Distance(transform.position, discardPile.position);
            if (distance < 0.6f)
            {
                Card currentCard = GetComponent<Card>();
                if (gameManager.CanPlaceCard(currentCard))
                {
                    transform.SetParent(discardPile);
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
            else{
                transform.position = startposition;
            }
        }


    }

    public Vector3 GetMousePositionInWorldSpace(){
        Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        position.z = 0f;
        return position;
    }

    
}
