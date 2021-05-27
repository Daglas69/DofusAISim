using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSpell : Spell
{
    public int damage; //Damage points
    public int push; // Number of cells the target is pushed to (must be linear)
    public int attract; // Number of cells the target is attracted to (must be linear)
    public bool approach; // True to approach the caster next to the target (must be linear)


    public override void ApplyOnCell(GridManager gridManager, GameCell cell)
    {
        //If the cell contains a character
        if (cell.IsOccupied())
        {
            Character character = cell.occupying; //Character on the cell
            GameObject characterObj = character.gameObject; //GameObject of the character

            //Damage of the spell
            character.RemoveLife(damage); //We remove character life

            //Direction of the caster throwing the spell
            Vector2Int dir = gridManager.GetDirTo(gridManager.GetCellOf(caster.gameObject), cell);

            //For the spell with push action
            PullshAction(gridManager, push, characterObj, dir);

            //For the spell with attract action
            PullshAction(gridManager, - attract, characterObj, dir); //Atract is negative as the function is used for pushing too

            //For the spell with approach action
            //Caster goes to the cell next to target
            if (approach)
            {
                Vector2Int newCasterPos = new Vector2Int(cell.ind_x, cell.ind_y) - dir;
                if (gridManager.InWalkableBounds(newCasterPos.x, newCasterPos.y)
                    && gridManager.IsWalkable(gridManager.grid[newCasterPos.x, newCasterPos.y]))
                {
                    gridManager.RemoveObjectOf(gridManager.GetCellOf(caster.gameObject));
                    gridManager.PlaceObjectOn(gridManager.grid[newCasterPos.x, newCasterPos.y], caster.gameObject);
                    caster.gameObject.transform.position = gridManager.grid[newCasterPos.x, newCasterPos.y].world_pos;
                }
            }

            if (character.IsMuerto()) FindObjectOfType<BattleManager>().RemoveCharacter(characterObj); //Not Spell class responsability... but to instant rm
            GameLog.Log("[BATTLE] " + gameObject.name + " hit " + characterObj.name + " (current life : " + character.life.current + ")");
        }

        //Spell effect on the cell
        SpeedDisplay.Instance().DoDisplay(
            () => gridManager.StartCoroutine(gridManager.ColorCellFX(cell, Color.red))
        );
    }


    //Push or pull action on a character
    //Positive value for pushing
    //Negative value for pulling
    //characterObj is the GameObject that get the action
    private void PullshAction(GridManager gridManager, int pullsh, GameObject characterObj, Vector2Int dir)
    {
        for (int i = 0; i < Math.Abs(pullsh); i++) //absolute value as it can be negative
        {
            GameCell currCharCell = gridManager.GetCellOf(characterObj);
            Vector2Int nextCharPos = new Vector2Int(currCharCell.ind_x, currCharCell.ind_y)
                + ((pullsh >= 0) ? new Vector2Int(dir.x, dir.y) : new Vector2Int(-dir.x, -dir.y));

            //Move to cell if possible
            if (gridManager.InWalkableBounds(nextCharPos.x, nextCharPos.y)
                && gridManager.IsWalkable(gridManager.grid[nextCharPos.x, nextCharPos.y]))
            {
                //TODO: anim
                gridManager.RemoveObjectOf(currCharCell);
                gridManager.PlaceObjectOn(gridManager.grid[nextCharPos.x, nextCharPos.y], characterObj);
                characterObj.transform.position = gridManager.grid[nextCharPos.x, nextCharPos.y].world_pos;
            }
            //Inflict damage else
            else
            {
                int pullshDamage = (int)(0.5f * damage * ((float)(pullsh - i) / (float)pullsh));
                characterObj.GetComponent<Character>().RemoveLife(pullshDamage);
                //If there is another character on the blocking tile
                if (gridManager.InWalkableBounds(nextCharPos.x, nextCharPos.y))
                {
                    Character obstacleChar = gridManager.grid[nextCharPos.x, nextCharPos.y].occupying;
                    if (obstacleChar != null) obstacleChar.RemoveLife(pullshDamage / 2);
                }
            }
        }
    }
}
