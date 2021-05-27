using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BuffSpell : Spell
{
    public int bonus_pa;
    public int bonus_pm;
    public float damage_resistance;
    public int turn_duration;


    public override void ApplyOnCell(GridManager gridManager, GameCell cell)
    {
        //If the cell contains a character
        if (cell.IsOccupied())
        {
            Character character = cell.occupying; //Character on the cell

            //Create buff
            Buff buff = new Buff();
            buff.type = BuffType.BUFF;
            buff.bonus_pa = bonus_pa;
            buff.bonus_pm = bonus_pm;
            buff.damage_resistance = damage_resistance;
            buff.turn_duration = turn_duration;

            //Add buff to character
            character.AddBuff(buff);

            GameLog.Log("[BATTLE] " + caster.gameObject.name + " buffed " + character.gameObject.name + " with " + gameObject.name + " for " + turn_duration + " turns");
        }

        //Spell effect on the cell
        SpeedDisplay.Instance().DoDisplay(
            () => gridManager.StartCoroutine(gridManager.ColorCellFX(cell, Color.blue))
        );
    }
}