using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This enum defines the variable type eCardState with four named values.      // a
public enum eCardState { drawpile, mine, target, discard }

public class CardPyramid : Card
{ // Make CardPyramid extend Card        // b
    [Header("Dynamic: CardPyramid")]
    public eCardState state = eCardState.drawpile;                   // c
                                                                     // The hiddenBy list stores which other cards will keep this one face down
    public List<CardProspector> hiddenBy = new List<CardProspector>();
    // The layoutID matches this card to the tableau JSON if it’s a tableau card
    public int layoutID;
    // The JsonLayoutSlot class stores information pulled in from JSON_Layout
    public JsonLayoutSlot layoutSlot;

    /// <summary>
    /// Informs the Pyramid class that this card has been clicked.
    /// </summary>
    override public void OnMouseUpAsButton()
    {
        Pyramid.CARD_CLICKED(this);
        base.OnMouseUpAsButton();
    }

}
