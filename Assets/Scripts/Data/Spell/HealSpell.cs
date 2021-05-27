using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealSpell : Spell
{
    public int heal; //heal points

    public override void ApplyOnCell(GridManager gridManager, GameCell cell)
    {
        //If the cell contains a character
        if (cell.IsOccupied())
        {
            Character character = cell.occupying; //Character on the cell
            character.life.Add(heal); //We remove character life
            GameLog.Log("[BATTLE] " + gameObject.name + " healed " + character.gameObject.name + " (current life : " + character.life.current + ")");
        }

        //Spell effect on the cell
        SpeedDisplay.Instance().DoDisplay(
            () => gridManager.StartCoroutine(gridManager.ColorCellFX(cell, Color.green))
        );
    }
}
