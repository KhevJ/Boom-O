using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// class for a card object
public class Card : MonoBehaviour
{
    //enums to make sure card attributes are valid
    public enum CardColor { Red, Blue, Green, Yellow, Wild }
    public enum CardType { Number, Skip, Reverse, Draw, Wild, WildDraw }


    // card attributes
    public CardColor color;
    public CardType type;
    public int number; // Only relevant for number cards
    public Sprite cardSprite; // The visual representation of the card

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }


    // setter
    public void SetCardData(CardColor newColor, CardType newType, int newNumber, Sprite newSprite)
    {
        color = newColor;
        type = newType;
        number = newNumber;
        cardSprite = newSprite;
        spriteRenderer.sprite = newSprite; // Set sprite to visually represent the card
    }
}
