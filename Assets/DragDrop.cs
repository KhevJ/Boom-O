using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragDrop : MonoBehaviour
{
    
    private Vector3 startposition;
    public Transform discardPile;
    public GameManager gameManager;
    
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
        float distance = Vector3.Distance(transform.position, discardPile.position);
        if(distance<0.6f){
            transform.SetParent(discardPile);
            transform.localPosition = Vector3.zero;
            gameManager.RealignPlayerCards();
        }
        else{
            transform.position = startposition;
        }

    }

    public Vector3 GetMousePositionInWorldSpace(){
        Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        position.z = 0f;
        return position;
    }

    
}
