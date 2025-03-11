using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawCard : MonoBehaviour
{
    public List<GameObject> cardPrefabs; // List of all card prefabs
    public RectTransform playerHand; // Assign PlayerCards (RectTransform) in Inspector
    public float maxHandWidth = 3f; // Maximum width for all cards in world space
    public float minSpacing = 0.5f; // Minimum spacing to prevent overlap

    void OnMouseDown() // Detect clicks on the draw pile
    {
        DrawNewCard();
    }

    void DrawNewCard()
    {
        if (cardPrefabs.Count > 0) // Check if deck is not empty
        {
            int randomIndex = Random.Range(0, cardPrefabs.Count); // Pick a random card
            GameObject newCard = Instantiate(cardPrefabs[randomIndex]); // Create card without parenting to UI
            newCard.transform.SetParent(playerHand, false); // Set as a child but maintain world position

            // Get the card's Transform
            Transform cardTransform = newCard.transform;
            cardTransform.localRotation = Quaternion.identity; // Reset rotation

            // Convert RectTransform's position to World Position
            cardTransform.position = playerHand.position;

            // Reposition all cards including the new one
            RepositionAllCards();

            // Remove the drawn card from the deck
            cardPrefabs.RemoveAt(randomIndex);
        }
        else
        {
            Debug.Log("No more cards in the deck!");
        }
    }

    void RepositionAllCards()
    {
        int cardCount = playerHand.childCount;
        if (cardCount == 0) return;

        // Calculate spacing based on max width and number of cards
        float spacing = Mathf.Max(minSpacing, maxHandWidth / Mathf.Max(1, cardCount - 1));

        float centerOffset = (cardCount - 1) * spacing / 2; // Keep cards centered

        for (int i = 0; i < cardCount; i++)
        {
            Transform cardTransform = playerHand.GetChild(i).transform;
            float newX = (i * spacing) - centerOffset; // Adjust position dynamically
            cardTransform.position = new Vector3(playerHand.position.x + newX, playerHand.position.y, 0); // Update world position
        }
    }
}
