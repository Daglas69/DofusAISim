using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class SimpleAI : AI
{
    override public void Execute()
    {
        if (enemiesCells.Count == 0)
        {
            canPlay = false;
            return;
        }
        GameCell targetCell = enemiesCells[0];

        //Get path to the nearest enemy
        List<GameCell> pathToTarget = ShortestPathTo(targetCell);

        //Get the best spell to use for this action
        List<GameCell> inScopeCells = new List<GameCell>(); //Empty list
        Spell currentSpell = ChooseSpell(character.spells, charCell, targetCell);
        if (currentSpell != null) //If there is an available spell
        {
            inScopeCells = battleManager.GetInScopeOfTargetCells(currentSpell, charCell, targetCell);
        }
        GameLog.Log("[AI_SIMPLE] " + gameObject.name + " -> spell chosen : " + currentSpell);

        //If the character can move and he can not throw spell (if he can throw but does not have enough PA, he must stay there)
        if (character.pm.current > 0 && inScopeCells.Count == 0 && pathToTarget != null && pathToTarget.Count > 0)
        {
            GameCell nextCell = pathToTarget[0];
            Vector3Int nextDir = new Vector3Int(nextCell.ind_x - charCell.ind_x, nextCell.ind_y - charCell.ind_y, 0);
            GameLog.Log("[AI_SIMPLE] " + gameObject.name + " -> Trying to move to : " + nextDir);
            battleManager.Move(gameObject, nextDir);

            //To indicate that it is still playing
            //Return to prevent multiples actions in one Execute
            canPlay = true;
            return;
        }

        //If he can throw spell (to the first cell of the in scope list)
        else if (currentSpell != null && character.pa.current - currentSpell.pa >= 0 && inScopeCells.Count > 0)
        {
            //Debug.Log("[AISimple] " + gameObject.name + " -> Trying to throw spell to : " + inScopeCells[0].ind_x + "," + inScopeCells[0].ind_y);
            battleManager.ThrowSpell(gameObject, currentSpell, inScopeCells[0]);
            
            //To indicate that it is still playing
            //Return to prevent multiples actions in one Execute
            canPlay = true;
            return;
        }

        //It the AI reachs this point = can no more play
        canPlay = false;
    }


    //Only uses damage spells
    private Spell ChooseSpell(List<Spell> spells, GameCell charCell, GameCell targetCell)
    {
        List<Spell> spellList = new List<Spell>(spells);

        //We keep damage spells
        spellList = spellList.Where(x => (x as DamageSpell) != null).ToList();
        if (spellList.Count == 0) return null; //If there is no damage spell

        //We remove spells with active cooldown 
        spellList = spellList.Where(x => !x.HasActiveCooldown()).ToList();
        if (spellList.Count == 0) return null; //If there is no more spell

        //We remove spell that can not hit the target
        spellList = spellList.Where(x => battleManager.GetInScopeOfTargetCells(x, charCell, targetCell).Count > 0).ToList();
        if (spellList.Count == 0) return null; //If there is no spell that can hit the target

        //We sort spells by damage (decreasing order)
        spellList.Sort((x,y) => y.GetComponent<DamageSpell>().damage.CompareTo(x.GetComponent<DamageSpell>().damage));

        //We return the best spell classified by damage
        return spellList[0];
    } 
}