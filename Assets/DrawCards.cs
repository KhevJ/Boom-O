using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class DrawCard : MonoBehaviour
{
    public List<GameObject> cardPrefabs; // List of all card prefabs
    //public Transform playerHand;  // Where drawn cards will appear

    void OnMouseDown() // Detect clicks on the draw pile
    {
        DrawNewCard();
    }

    void DrawNewCard()
    {
        if (cardPrefabs.Count > 0) // Check if deck is not empty
        {
            int randomIndex = Random.Range(0, cardPrefabs.Count); // Pick a random card
            GameObject selectedCard = cardPrefabs[randomIndex]; // Get the prefab

            GameObject newCard = Instantiate(selectedCard);
            newCard.transform.position = new Vector3(-6, 3, 0);

            cardPrefabs.RemoveAt(randomIndex); // Remove drawn card from deck
        }
        else
        {
            Debug.Log("No more cards in the deck!");
        }
    }
}
