using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/* Arbre de décision
 EDIT : https://lucid.app/lucidchart/4a521cb7-c8dc-4242-998f-a4e4279ca2aa/edit?shared=true&page=0_0
 VIEW : https://lucid.app/lucidchart/4a521cb7-c8dc-4242-998f-a4e4279ca2aa/view
 */

public class EniAI : AI
{
    //////////////////////////////////////////////// Variables /////////////////////////////////////////////////////
    private List<BuffSpell> availableBuffSpells; //Available buff spells of the character (enough pa, no cooldown)
    private List<DamageSpell> availableDamageSpells; //Available damage spells of the character (enough pa, no cooldown)
    private List<HealSpell> availableHealSpells; //Available heal spells of the character (enough pa, no cooldown)
    private List<GameObject> alliesToHeal; //List of allies that need heal
    private Target allieToHeal; //Target to heal
    private List<Target> alliesBuffable; //Allies buffable by the character from a list of heal spell
    private List<Target> alliesBuffableInScope; //Allies buffable that are in scope of the character
    private GameCell cellToGo; //Gamecell to go on for movement actions
    private List<Target> enemiesHittable; //Enemies hittable by the character from a list of spell
    private List<Target> enemiesHittableInScope; //Enemies hittable that are in scope of the character


    /////////////////////////////////////////////// Specific methods ///////////////////////////////////////////////

    //Chosing the best heal spell to use
    private Spell ChooseHealSpell(List<Spell> spells, GameObject target)
    {
        //Life to heal to the target
        int lifeToHeal = target.GetComponent<Character>().life.max - target.GetComponent<Character>().life.current;
        //We keep spells that can be thrown
        spells = spells.Where(x => battleManager.GetInScopeOfTargetCells(x, charCell, gridManager.GetCellOf(target)).Count > 0).ToList();
        //We order spells by heal points
        spells = spells.OrderBy(x => (x as HealSpell).heal).ToList();
        //We iterate over heal spells and get the first spell healing more than lifeToHeal
        //If it does not exist, we take the last spell of the list (with more healing points)
        Spell spellToUse = null;
        foreach (Spell spell in spells)
        {
            if ((spell as HealSpell).heal > lifeToHeal)
            {
                spellToUse = spell;
                break;
            }
        }
        //No spell found
        if (spellToUse == null) spellToUse = spells[spells.Count - 1];
        return spellToUse;
    }

    //Chosing target regarding target with lwoer hp and spell damage 
    private (Target, Spell) ChooseHittableTarget(List<Target> targets)
    {
        Target chosenTarget = targets.OrderBy(x => x.targetObj.GetComponent<Character>().life.current).ToList()[0];
        Spell chosenSpell = chosenTarget.spells.OrderByDescending(x => (x as DamageSpell).damage).ToList()[0];
        return (chosenTarget, chosenSpell);
    }

    //Reset all variables
    //-> To detect if a variable is used without init
    override public void ResetVars()
    {
        charCell = null;
        enemies = null;
        allies = null;
        enemiesCells = null;
        alliesCells = null;
        availableBuffSpells = null;
        availableDamageSpells = null;
        availableHealSpells = null;
        alliesToHeal = null;
        allieToHeal = null;
        alliesBuffable = null;
        alliesBuffableInScope = null;
        cellToGo = null;
        enemiesHittable = null;
        enemiesHittableInScope = null;
    }



    /////////////////////////////////////////////// start of the flowchart ///////////////////////////////////////////////

    override public void Execute()
    {
        //First node of the flowchart
        B0();
    }


    /////////////////////////////////////////////// Flowchart cells (one cell = one function) ///////////////////////////////////////////////


    // ~~~~~~~~~~~~~~~~ Bulles : fonctions BX qui correspondent à une question dans le flowchart ~~~~~~~~~~~~~~~~

    //B0 : Ai-je des alliés en vie ?
    void B0()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " B0");
        if (allies.Length > 0)
        {
            B1(); //J'ai des alliés en vie
        }
        else
        {
            B2(); //Je n'ai pas d'alliés en vie
        }
    }


    //B1 : Ai-je des sorts de soin disponibles ?
    void B1()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " B1");
        availableHealSpells = character.GetAvailableSpells<HealSpell>();
        if (availableHealSpells.Count > 0)
        {
            B3(); //J'ai des sorts de soin disponibles
        }
        else
        {
            B4(); //Je n'ai pas de sorts de soin disponibles
        }
    }


    //B2 : Ai-je besoin de me soigner ?
    //90% de ma vie si j'ai des alliés et 20% si je n'ai pas d'alliés
    void B2()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " B2");
        float coeffNeedHeal = allies.Length > 0 ? 0.9f : 0.2f;
        if (character.life.current <= character.life.max * coeffNeedHeal) //Need heal under 90%/20% of his life
        {
            B5(); //J'ai besoin de me soigner
        }
        else
        {
            B6(); //Je n'ai pas besoin de me soigner
        }
    }

    //B3 : Des alliés ont-ils besoin de soins ? (90% de leurs vies)
    void B3()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " B3");
        //Ge allies that need heal
        alliesToHeal = new List<GameObject>();
        foreach (GameObject allie in allies)
        {
            //Need heal under 90% of his life
            Character allieChar = allie.GetComponent<Character>();
            if (allie != gameObject && allieChar.life.current <= allieChar.life.max * 0.9) alliesToHeal.Add(allie);
        }
        if (alliesToHeal.Count > 0)
        {
            B7(); //Des alliés ont besoin de soins
        }
        else
        {
            B4(); //Les alliés n'ont pas besoin de soins
        }
    }

    //B4 : Ai-je des sorts de buff disponibles ?
    void B4()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " B4");
        availableBuffSpells = character.GetAvailableSpells<BuffSpell>();
        if (availableBuffSpells.Count > 0)
        {
            B8(); //J'ai des sorts de buff disponibles
        }
        else
        {
            B9(); //Je n'ai pas de sorts de buff disponibles
        }
    }

    //B5 : Ai-je des sorts de soin disponibles ?
    void B5()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " B5");
        availableHealSpells = character.GetAvailableSpells<HealSpell>();
        if (availableHealSpells.Count > 0)
        {
            P1(); //J'ai des sorts de soin disponibles
        }
        else
        {
            B6(); //Je n'ai pas de sorts de soin disponibles
        }
    }

    //B6 : Ai-je des sorts de buff disponibles ?
    void B6()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " B6");
        availableBuffSpells = character.GetAvailableSpells<BuffSpell>();
        if (availableBuffSpells.Count > 0)
        {
            P2(); //J'ai des sorts de buff disponibles
        }
        else
        {
            B9(); //Je n'ai pas de sorts de buff disponibles
        }
    }

    //B7 : Suis-je à portée de l'allié nécessitant le plus de soin dans le tableau ?
    void B7()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " B7");
        //We get the allie GameObject with lowest HP
        var allieObj = alliesToHeal.OrderBy(x => x.GetComponent<Character>().life.current).ToList()[0];
        //We use AI function to convert GameObject to Target (weird use of the function)
        //It returns a list of one target as we check it for one GameObject only
        //A bit tricky but it is done to reuse existing functions
        var alliesTargets = GetEnemiesHittable(new GameObject[] { allieObj }, availableHealSpells);
        allieToHeal = alliesTargets[0]; //The only one target of the list
        if (GetTargetsInScope(alliesTargets, charCell).Count > 0) //In scope
        {
            P3(); //Je suis à portée de l'allié nécessitant le plus de soin dans le tableau
        }
        else
        {
            B10(); //Je ne suis pas à portée de l'allié nécessitant le plus de soin dans le tableau
        }
    }

    //B8 : Suis-je à portée pour buff un allié ?
    void B8()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " B8");
        alliesBuffable = GetEnemiesHittable(battleManager.turn_grp_chars, availableBuffSpells); //Usually use for enemies but work the same
        alliesBuffableInScope = GetTargetsInScope(alliesBuffable, charCell);
        if (alliesBuffableInScope.Count > 0)
        {
            P4(); //Je suis à portée pour buff un allié
        }
        else
        {
            B11(); //Je ne suis pas à portée pour buff un allié
        }
    }

    //B9 : Suis-je à portée de cibles avec des sorts d'attaque disponibles ?
    void B9()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " B9");
        availableDamageSpells = character.GetAvailableSpells<DamageSpell>();
        enemiesHittable = GetEnemiesHittable(enemies, availableDamageSpells);
        enemiesHittableInScope = GetTargetsInScope(enemiesHittable, charCell);
        if (enemiesHittableInScope.Count > 0)
        {
            P5(); //Je suis à portée de cibles avec des sorts d'attaque disponibles
        }
        else
        {
            B13(); //Je ne suis pas à portée de cibles avec des sorts d'attaque disponibles
        }
    }

    //B10 : Puis-je me mettre à portée de l'allié nécessitant le plus de soin dans le tableau ?
    void B10()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " B10");
        cellToGo = ChooseCellToMoveInScopeOfTarget(allieToHeal);
        if (cellToGo != null)
        {
            P7(); //Je peux me mettre à portée de l'allié nécessitant le plus de soin dans le tableau
        }
        else
        {
            //As the character can not move until him, we update the list of allies
            alliesToHeal.Remove(allieToHeal.targetObj);
            B12(); //Je ne peux pas me mettre à portée de l'allié nécessitant le plus de soin dans le tableau
        }
    }

    //B11 : Puis-je me mettre à portée pour buff un allié ?
    void B11()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " B11");
        cellToGo = ChooseCellToMoveInScopeOfTarget(alliesBuffable);
        if (cellToGo != null)
        {
            P8(); //Je peux me mettre à portée pour buff un allié
        }
        else
        {
            P2(); //Je ne peux pas me mettre à portée pour buff un allié
        }
    }

    //B12 : Ai-je encore des alliés à traiter dans le tableau d'alliés nécessitant des soins ?
    void B12()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " B12");
        if (alliesToHeal.Count > 0)
        {
            B7(); //J'ai encore des alliés à traiter dans le tableau d'alliés nécessitant des soins
        }
        else
        {
            B2(); //Je n'ai plus d'alliés à traiter dans le tableau d'alliés nécessitant des soins
        }
    }

    //B13 : Suis-je à portée d'un allié ?
    void B13()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " B13");
        //Check avec sort ayant la plus petite portée
        int scope = character.spells.Where(x => x.maxScope > 0).Min(x => x.maxScope);
        bool inScope = alliesCells.Where(x => gridManager.ManhattanDistance(charCell, x) <= scope).Count() > 0;
        if (inScope)
        {
            P9(); //Je suis à portée d'un allié
        }
        else
        {
            P6(); //Je ne suis pas à portée d'un allié
        }
    }


    // ~~~~~~~~~~~~~~~~ Process : fonctions PX qui correspondent à une action dans le flowchart ~~~~~~~~~~~~~~~~

    //P1 : Je me soigne avec le sort le plus efficace, en essayant de toucher le moins d'ennemis possible
    void P1()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " P1");
        Spell chosenSpell = ChooseHealSpell(availableHealSpells.Cast<Spell>().ToList(), gameObject);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, charCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<HealSpell>(inScopeCells, chosenSpell));
    }

    //P2 : Je me buff avec le premier sort disponible (tiré aléatoirement) 
    void P2()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " P2");
        BuffSpell spellToUse = availableBuffSpells[Utils.RandomIndex(availableBuffSpells.Count)];
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(spellToUse, charCell, charCell);
        battleManager.ThrowSpell(gameObject, spellToUse, ChooseCellToThrowSpell<HealSpell>(inScopeCells, spellToUse));
    }

    //P3 : Je lance le sort le plus adapté sur une case touchant l'allié nécessitant le plus de soin et en fonction du ratio ennemis alliés touchés
    void P3()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " P3");
        Spell chosenSpell = ChooseHealSpell(allieToHeal.spells, allieToHeal.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, gridManager.GetCellOf(allieToHeal.targetObj));
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<HealSpell>(inScopeCells, chosenSpell));
    }

    //P4 : Je lance le premier sort de buff sur une case touchant l'allié ayant le moins de buffs actifs et en fonction du ratio ennemis alliés touchés
    void P4()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " P4");
        Target allieToBuff = alliesBuffableInScope.OrderBy(x => x.targetObj.GetComponent<Character>().buffs.Count).ToList()[0];
        Spell spellToUse = allieToBuff.spells[Utils.RandomIndex(allieToBuff.spells.Count)];
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(spellToUse, charCell, gridManager.GetCellOf(allieToBuff.targetObj));
        battleManager.ThrowSpell(gameObject, spellToUse, ChooseCellToThrowSpell<HealSpell>(inScopeCells, spellToUse));
    }

    //P5 : Je lance le sort ayant le plus de damage sur une case touchant la cible avec le moins de vie et en fonction du nombre d'ennemis touchés
    void P5()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " P5");
        (Target chosenTarget, Spell chosenSpell) = ChooseHittableTarget(enemiesHittableInScope);
        GameCell targetCell = gridManager.GetCellOf(chosenTarget.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, targetCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<DamageSpell>(inScopeCells, chosenSpell));
    }

    //P6 : Je me déplace de 1 vers l'allié le plus proche si possible
    void P6()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " P6");
        if (character.pm.IsEnough(1))
        {
            //We get the first available path from allie's cells ordered by nearest
            List<GameCell> path = null;
            foreach (GameCell allieCell in alliesCells)
            {
                path = ShortestPathTo(allieCell);
                if (path != null) break;
            }
            if (path != null && path.Count > 0) //If a path to an allie has been found and allie is not next to him
            {
                MoveTo(path[0]);
            }
            else canPlay = false;
        }
        else canPlay = false;
    }

    //P7 : Je me déplace de 1 vers l'allié nécessitant le plus de soin
    void P7()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " P7");
        List<GameCell> path = CanGoTo(cellToGo); //Get path to the cell
        if (path != null) MoveTo(path[0]); //Move to next cell in path to target
        else canPlay = false;
    }

    // P8 : Je me déplace de 1 vers l'allié le plus proche 
    void P8()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " P8");
        List<GameCell> path = CanGoTo(cellToGo); //Get path to the cell
        if (path != null) MoveTo(path[0]); //Move to next cell in path to target
        else canPlay = false;
    }

    // P9 : Fin de mon tour
    void P9()
    {
        GameLog.Log("[ENI_AI] " + gameObject.name + " P9");
        canPlay = false;
    }
}