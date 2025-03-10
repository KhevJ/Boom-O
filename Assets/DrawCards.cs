using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawCard : MonoBehaviour
{
    public List<GameObject> cardPrefabs; // List of all card prefabs
    public Transform playerHand; // Assign PlayerCards object in Inspector

    void OnMouseDown() // Detect clicks on the draw pile
    {
        DrawNewCard();
    }

    void DrawNewCard()
    {
        if (cardPrefabs.Count > 0) // Check if deck is not empty
        {
            int randomIndex = Random.Range(0, cardPrefabs.Count); // Pick a random card
            GameObject selectedCard = Instantiate(cardPrefabs[randomIndex], playerHand); // Parent to PlayerCards

            // Get RectTransform since we are using UI
            RectTransform cardRect = selectedCard.GetComponent<RectTransform>();

            if (cardRect != null)
            {
                int cardCount = playerHand.childCount - 1; // Count existing cards
                float overlapAmount = -60f; //Negative for overlap (adjust value if needed)

                // Position cards with slight overlap
                cardRect.anchoredPosition = new Vector2(cardCount * overlapAmount, 0);
                cardRect.localScale = Vector3.one; // Ensure correct size
            }
            else
            {
                Debug.LogError("New card does not have a RectTransform!");
            }

            // Remove the drawn card from the deck
            cardPrefabs.RemoveAt(randomIndex);
        }
        else
        {
            Debug.Log("No more cards in the deck!");
        }
    }
}
