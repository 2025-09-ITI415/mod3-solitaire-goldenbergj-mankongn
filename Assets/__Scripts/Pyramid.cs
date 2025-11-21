using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;   // We’ll need this line later in the chapter

[RequireComponent(typeof(Deck))]                                              // a
[RequireComponent(typeof(JsonParseLayout))]
public class Pyramid : MonoBehaviour
{
    private static Pyramid S; // A private Singleton for Pyramid

    [Header("Dynamic")]
    public List<CardPyramid> drawPile;
    public List<CardPyramid> discardPile;
    public List<CardPyramid> mine;
    public List<CardPyramid> targetPile;
    public CardPyramid target;
    private CardPyramid selectedCard = null;

    private Transform layoutAnchor;

    private Deck deck;
    private JsonLayout jsonLayout;

    // A Dictionary to pair mine layout IDs and actual Cards
    private Dictionary<int, CardPyramid> mineIdToCardDict;                 // a


    void Start()
    {
        // Set the private Singleton. We’ll use this later.
        if (S != null) Debug.LogError("Attempted to set S more than once!");  // b
        S = this;

        jsonLayout = GetComponent<JsonParseLayout>().layout;

        deck = GetComponent<Deck>();
        // These two lines replace the Start() call we commented out in Deck
        deck.InitDeck();
        Deck.Shuffle(ref deck.cards);

        drawPile = ConvertCardsToCardPyramids(deck.cards);

        LayoutMine();

        MoveToTarget(Draw());
        UpdateDrawPile();
    }

    /// <summary>
    /// Converts each Card in a List(Card) into a List(CardPyramid) so that it
    ///  can be used in the Pyramid game.
    /// </summary>
    /// <param name="listCard">A List(Card) to be converted</param>
    /// <returns>A List(CardPyramid) of the converted cards</returns>
    List<CardPyramid> ConvertCardsToCardPyramids(List<Card> listCard)
    {
        List<CardPyramid> listCP = new List<CardPyramid>();
        CardPyramid cp;
        foreach (Card card in listCard)
        {
            cp = card as CardPyramid;                                      // c
            listCP.Add(cp);
        }
        return (listCP);
    }

    /// <summary>
    /// Pulls a single card from the beginning of the drawPile and returns it
    /// Note: There is no protection against trying to draw from an empty pile!
    /// </summary>
    /// <returns>The top card of drawPile</returns>
    CardPyramid Draw()
    {
        CardPyramid cp = drawPile[0]; // Pull the 0th CardPyramid
        drawPile.RemoveAt(0);            // Then remove it from drawPile
        return (cp);                      // And return it
    }

    /// <summary>
    /// Positions the initial tableau of cards, a.k.a. the "mine"
    /// </summary>
    void LayoutMine()
    {
        // Create an empty GameObject to serve as an anchor for the tableau   // a
        if (layoutAnchor == null)
        {
            // Create an empty GameObject named _LayoutAnchor in the Hierarchy
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;             // Grab its Transform
        }

        CardPyramid cp;

        // Generate the Dictionary to match mine layout ID to CardPyramid
        mineIdToCardDict = new Dictionary<int, CardPyramid>();             // b


        // Iterate through the JsonLayoutSlots pulled from the JSON_Layout
        foreach (JsonLayoutSlot slot in jsonLayout.slots)
        {
            cp = Draw(); // Pull a card from the top (beginning) of the draw Pile
            cp.faceUp = slot.faceUp;    // Set its faceUp to the value in SlotDef
                                        // Make the CardPyramid a child of layoutAnchor
            cp.transform.SetParent(layoutAnchor);

            // Convert the last char of the layer string to an int (e.g. "Row 0")
            int z = int.Parse(slot.layer[slot.layer.Length - 1].ToString());  // c

            // Set the localPosition of the card based on the slot information
            cp.SetLocalPos(new Vector3(
            jsonLayout.multiplier.x * slot.x,
            jsonLayout.multiplier.y * slot.y,
            -z));                                                       // d

            cp.layoutID = slot.id;
            cp.layoutSlot = slot;
            // CardPyramids in the mine have the state CardState.mine
            cp.state = eCardState.mine;

            // Set the sorting layer of all SpriteRenderers on the Card
            cp.SetSpriteSortingLayer(slot.layer);

            mine.Add(cp); // Add this CardPyramid to the List<mine>

            // Add this CardPyramid to the mineIDtoCardDict Dictionary
            mineIdToCardDict.Add(slot.id, cp);                                // c

        }
    }

    /// <summary>
    /// Moves the current target card to the discardPile
    /// </summary>
    /// <param name="cp">The CardPyramid to be moved</param>
    void MoveToDiscard(CardPyramid cp)
    {
        // Set the state of the card to discard
        cp.state = eCardState.discard;
        discardPile.Add(cp);  // Add it to the discardPile List<>
        cp.transform.SetParent(layoutAnchor); // Update its transform parent

        // Position it on the discardPile
        cp.SetLocalPos(new Vector3(
        jsonLayout.multiplier.x * jsonLayout.discardPile.x,
        jsonLayout.multiplier.y * jsonLayout.discardPile.y,
        0));

        cp.faceUp = true;

        // Place it on top of the pile for depth sorting
        cp.SetSpriteSortingLayer(jsonLayout.discardPile.layer);               // a
        cp.SetSortingOrder(-200 + (discardPile.Count * 3));                  // b
    }

    void RemoveCard(CardPyramid cp)
    {
        switch (cp.state)
        {
            case eCardState.mine:
                mine.Remove(cp);
                MoveToDiscard(cp);
                break;

            case eCardState.target:
                int index = targetPile.IndexOf(cp);
                if(index >= 0)
                {
                    targetPile.RemoveAt(index);
                }
                MoveToDiscard(cp);

                if (targetPile.Count > 0)
                {
                    CardPyramid newTarget = targetPile[targetPile.Count - 1];
                    target = newTarget;
                    newTarget.state = eCardState.target;

                    int i = targetPile.Count - 1;
                    newTarget.SetSpriteSortingLayer("Target");
                    newTarget.SetSortingOrder(-200 + i * 3);
                }
                else
                {
                    target = null;
                }
                break;
        }
    }

    void ManageCardSelection(CardPyramid cp)
    {
        if (!cp.faceUp) return;

        if (cp.rank == 13) // King is selected
        {
            RemoveCard(cp);
            ScoreManager.TALLY(eScoreEvent.mine);
            SetMineFaceUps();
            selectedCard = null;
            return;
        }

        if (selectedCard == null)
        {
            selectedCard = cp;
            return;
        }

        if (selectedCard.rank + cp.rank == 13)
        {
            RemoveCard(selectedCard);
            RemoveCard(cp);
            ScoreManager.TALLY(eScoreEvent.mine);
            selectedCard = null;
            SetMineFaceUps();
            CheckForGameOver();
        }

        else
        {
            selectedCard = cp;
        }
    }

    /// <summary>
    /// Make cp the new target card
    /// </summary>
    /// <param name="cp">The CardPyramid to be moved</param>
    void MoveToTarget(CardPyramid cp)
    {
        targetPile.Add(cp);
        // Then set a few additional things to make cp the new target
        target = cp; // cp is the new target

        cp.state = eCardState.target;
        cp.transform.SetParent(layoutAnchor);
        cp.faceUp = true;

        float tx = (jsonLayout.discardPile.x + 3);
        float ty = jsonLayout.drawPile.y;
        int i = targetPile.Count - 1;
        Vector3 pos = new Vector3(jsonLayout.multiplier.x * tx, jsonLayout.multiplier.y * ty, -0.1f * i);
        cp.SetLocalPos(pos);
        // Set the depth sorting so that cp is on top of the discardPile
        cp.SetSpriteSortingLayer("Target");                                 // c
        cp.SetSortingOrder(-200 + i * 3);
    }

    /// <summary>
    /// Arranges all the cards of the drawPile to show how many are left
    /// </summary>
    void UpdateDrawPile()
    {
        CardPyramid cp;
        // Go through all the cards of the drawPile
        for (int i = 0; i < drawPile.Count; i++)
        {
            cp = drawPile[i];
            cp.transform.SetParent(layoutAnchor);

            // Position it correctly with the layout.drawPile.stagger
            Vector3 cpPos = new Vector3();
            cpPos.x = jsonLayout.multiplier.x * jsonLayout.drawPile.x;
            // Add the staggering for the drawPile
            cpPos.x += jsonLayout.drawPile.xStagger * i;
            cpPos.y = jsonLayout.multiplier.y * jsonLayout.drawPile.y;
            cpPos.z = 0.1f * i;
            cp.SetLocalPos(cpPos);

            cp.faceUp = false; // DrawPile Cards are all face-down
            cp.state = eCardState.drawpile;
            // Set depth sorting
            cp.SetSpriteSortingLayer(jsonLayout.drawPile.layer);
            cp.SetSortingOrder(-10 * i);
        }
    }

    /// <summary>
    /// This turns cards in the Mine face-up and face-down
    /// </summary>
    public void SetMineFaceUps()
    {                                            // d
        CardPyramid coverCP;
        foreach (CardPyramid cp in mine)
        {
            bool faceUp = true; // Assume the card will be face-up

            // Iterate through the covering cards by mine layout ID
            foreach (int coverID in cp.layoutSlot.hiddenBy)
            {
                coverCP = mineIdToCardDict[coverID];
                // If the covering card is null or still in the mine...
                if (coverCP == null || coverCP.state == eCardState.mine)
                {
                    faceUp = false; // then this card is face-down
                }
            }
            cp.faceUp = faceUp; // Set the value on the card
        }
    }

    /// <summary>
    /// Test whether the game is over
    /// </summary>
    void CheckForGameOver()
    {                                                 // a
                                                      // If the mine is empty, the game is over
        if (mine.Count == 0)
        {
            GameOver(true);  // Call GameOver() with a win
            return;
        }

        // If there are still cards in the mine & draw pile, the game’s not over
        if (drawPile.Count > 0) return;

        // Check for remaining valid plays
        if (!HasPlayablePair())                                      // b
        {
            GameOver(false);
        }
    }

    bool HasPlayablePair()
    {
        List<CardPyramid> availableCards = new List<CardPyramid>();

        foreach (CardPyramid cp in mine)
        {
            if (cp.faceUp) availableCards.Add(cp);
        }

        if (target != null && target.state == eCardState.target)
        {
            availableCards.Add(target);
        }

        foreach (CardPyramid cp in availableCards)
        {
            if (cp.rank == 13) return true;
        }

        for (int i = 0; i < availableCards.Count - 1; i++)
        {
            for (int j = i + 1; j < availableCards.Count; j++)
            {
                if (availableCards[i].rank + availableCards[j].rank == 13) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Called when the game is over. Simple for now, but expandable
    /// </summary>
    /// <param name="won">true if the player won</param>
    void GameOver(bool won)
    {
        if (won)
        {
            //Debug.Log("Game Over. You won! :)");
            ScoreManager.TALLY(eScoreEvent.gameWin);
        }
        else
        {
            //Debug.Log("Game Over. You Lost. :(");
            ScoreManager.TALLY(eScoreEvent.gameLoss);
        }

        // Reset the CardSpritesSO singleton to null
        CardSpritesSO.RESET();                                                // b
                                                                              // Reload the scene, resetting the game
                                                                              // Note that there are TWO underscores at the beginning of "__Pyramid…
        SceneManager.LoadScene("__Pyramid_Scene_0");
    }

    /// <summary>
    /// Handler for any time a card in the game is clicked
    /// </summary>
    /// <param name="cp">The CardPyramid that was clicked</param>
    static public void CARD_CLICKED(CardPyramid cp)
    {
        // The reaction is determined by the state of the clicked card
        switch (cp.state)
        {
            case eCardState.target:
                S.ManageCardSelection(cp);
                break;
            case eCardState.drawpile:
                // Clicking *any* card in the drawPile will draw the next card
                // Call two methods on the Pyramid Singleton S
                S.MoveToTarget(S.Draw());  // Draw a new target card
                S.UpdateDrawPile();          // Restack the drawPile
                ScoreManager.TALLY(eScoreEvent.draw);
                S.selectedCard = null; // Clear selection if drawing
                break;
            case eCardState.mine:
                S.ManageCardSelection(cp);
                break;
            case eCardState.discard:
                break;
        }
        S.CheckForGameOver();
    }

}