using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PyramidManager : MonoBehaviour
{
    private Card firstSelectedCard = null;
    private Card secondSelectedCard = null;

    public void SelectCard(Card clickedCard)
    {
        if (!clickedCard.faceUp) return;

        if (clickedCard.rank == 13)
        {
            RemoveCard(clickedCard);
            ResetSelection();
            return;
        }

        if (firstSelectedCard == null)
        {
            firstSelectedCard = clickedCard;
            return;
        }

        if (secondSelectedCard == null && clickedCard != firstSelectedCard)
        {
            secondSelectedCard = clickedCard;

            if (firstSelectedCard.rank + secondSelectedCard.rank == 13)
            {
                RemoveCard(firstSelectedCard);
                RemoveCard(secondSelectedCard);
            }

            ResetSelection();
        }
    }

    private void RemoveCard(Card card)
    {
        card.faceUp = false;
        Destroy(card.gameObject);
    }

    private void ResetSelection()
    {
        firstSelectedCard = null;
        secondSelectedCard = null;
    }
}
