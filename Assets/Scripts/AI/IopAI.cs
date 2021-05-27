using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/* Arbre de décision
 EDIT : https://lucid.app/lucidchart/invitations/accept/ae29801c-90eb-4b40-bcf3-125828010cec 
 VIEW : https://lucid.app/lucidchart/c45f4e44-354b-4570-9382-73114709e239/view
 */

public class IopAI : AI
{
    //////////////////////////////////////////////// Variables /////////////////////////////////////////////////////
    private List<BuffSpell> availableBuffSpells; //Available buff spells of the character (enough pa, no cooldown)
    private List<DamageSpell> availableDamageSpells; //Available damage spells of the character (enough pa, no cooldown)
    private List<Target> enemiesKillable; //Enemies killable by the character from a list of spell
    private List<Target> enemiesKillableInScope; //Enemies killable that are in scope of the character
    private GameCell cellToGo; //Gamecell to go on for movement actions
    private List<Target> enemiesInScope; //Enemies that are in scope of the character
    private DamageSpell bestDamageSpell; //Best damage spell of the character
    private List<Target> enemiesHittableBestSpell; //Enemies hittable with the best spell of the character
    private List<Target> enemiesHittableBestSpellInScope; //Enemies hittable with the best spell of the character and that are in scope
    private List<Target> enemiesHittable; //Enemies hittable by the character from a list of spell
    private List<Target> enemiesHittableInScope; //Enemies hittable that are in scope of the character


    /////////////////////////////////////////////// Specific methods ///////////////////////////////////////////////

    //Chosing target regarding spell pa 
    private (Target, Spell) ChooseKillableTarget(List<Target> targets)
    {
        Target chosenTarget = targets[0];
        Spell chosenSpell = chosenTarget.spells.OrderBy(x => x.pa).ToList()[0];
        for (int i = 1; i < targets.Count; ++i)
        {
            Spell s = targets[i].spells.OrderBy(x => x.pa).ToList()[0];
            if (s.pa < chosenSpell.pa)
            {
                chosenTarget = targets[i];
                chosenSpell = s;
            }
        }
        return (chosenTarget, chosenSpell);
    }

    //Chosing target regarding spell damage 
    private (Target, Spell) ChooseHittableTarget(List<Target> targets)
    {
        Target chosenTarget = targets[0];
        Spell chosenSpell = chosenTarget.spells.OrderByDescending(x => (x as DamageSpell).damage).ToList()[0];
        for (int i = 1; i < targets.Count; ++i)
        {
            Spell s = targets[i].spells.OrderByDescending(x => (x as DamageSpell).damage).ToList()[0];
            if ((s as DamageSpell).damage > (chosenSpell as DamageSpell).damage)
            {
                chosenTarget = targets[i];
                chosenSpell = s;
            }
        }
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
        enemiesKillable = null;
        enemiesKillableInScope = null;
        cellToGo = null;
        enemiesInScope = null;
        bestDamageSpell = null;
        enemiesHittableBestSpell = null;
        enemiesHittableBestSpellInScope = null;
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

    //B0 : Suis-je buff ?
    void B0()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " B0");
        if (!character.IsBuffed())
        {
            B2(); //Je ne suis pas buff
        }
        else
        {
            B1(); //Je suis buff
        }
    }


    //B1 : Ai-je des sorts d'attaque disponibles ?
    void B1()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " B1");
        availableDamageSpells = character.GetAvailableSpells<DamageSpell>();
        if (availableDamageSpells.Count > 0)
        {
            B3(); //J'ai des sorts d'attaque disponibles
        }
        else
        {
            B4(); //Je n'ai pas de sorts d'attaque disponibles
        }
    }


    //B2 : Suis-je low hp ?
    void B2()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " B2");
        if (character.IsLowHP())
        {
            B5(); //Je suis low hp
        }
        else
        {
            B6(); //Je ne suis pas low hp
        }
    }

    //B3 : Suis-je à portée de cibles tuables avec un sort disponible ?
    void B3()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " B3");
        enemiesKillable = GetEnemiesKillable(enemies, availableDamageSpells);
        enemiesKillableInScope = GetTargetsInScope(enemiesKillable, charCell);
        if (enemiesKillableInScope.Count > 0)
        {
            P1(); //Je suis a portée de cible tuable avec un sort disponible
        }
        else
        {
            B7(); //Je ne suis pas à portée de cible tuable avec un sort disponible
        }
    }

    //B4 : Suis-je a portée de cibles ?
    void B4()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " B4");
        enemiesInScope = GetTargetsInScope(GetEnemiesHittable(enemies, character.Spells<DamageSpell>()), charCell);
        if (enemiesInScope.Count > 0)
        {
            P2(); //Je suis a portée de cibles avec un sort
        }
        else
        {
            P3(); //Je ne suis pas a portée de cibles avec un sort
        }
    }

    //B5 : Un ennemi peut-il me tuer ?
    void B5()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " B5");
        if (CanEnemyKillMe(enemies))
        {
            B1(); //Un ennemi peut me tuer
        }
        else
        {
            B6(); //Un ennemi ne peut pas me tuer
        }
    }

    //B6 : Puis-je me buff?
    void B6()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " B6");
        availableBuffSpells = character.GetAvailableSpells<BuffSpell>();
        if (availableBuffSpells.Count > 0)
        {
            P4(); //Je peux me buff
        }
        else
        {
            B1(); //Je ne peux pas me buff
        }
    }

    //B7 : puis-je me mettre à portée de cibles tuables avec un sort disponible ?
    void B7()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " B7");
        cellToGo = ChooseCellToMoveInScopeOfTarget(enemiesKillable);
        if (cellToGo != null)
        {
            P5(); //Je peux me mettre à portée de cibles tuables avec un sort disponible
        }
        else
        {
            B8(); //Je ne peux pas me mettre à portée de cibles tuables avec un sort disponible
        }
    }

    //B8 : Puis-je attaquer des cibles avec mon meilleur sort disponible (damage) ?
    void B8()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " B8");
        bestDamageSpell = availableDamageSpells.OrderByDescending(x => x.damage).ToList()[0];
        enemiesHittableBestSpell = GetEnemiesHittable(enemies, new List<DamageSpell> { bestDamageSpell });
        enemiesHittableBestSpellInScope = GetTargetsInScope(enemiesHittableBestSpell, charCell);
        if (enemiesHittableBestSpellInScope.Count > 0)
        {
            P6(); //Je peux attaquer des cibles avec mon meilleur sort disponible
        }
        else
        {
            B9(); //Je ne peux pas attaquer des cibles avec mon meilleur sort disponible
        }
    }

    //B9 : Puis-je me mettre à portée pour attaquer des cibles avec mon meilleur sort disponible (damage) ?
    void B9()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " B9");
        cellToGo = ChooseCellToMoveInScopeOfTarget(enemiesHittableBestSpell);
        if (cellToGo != null)
        {
            P7(); //Je peux me mettre à portée pour attaquer des cibles avec mon meilleur sort disponible
        }
        else
        {
            B10(); //Je ne peux pas me mettre à portée pour attaquer des cibles avec mon meilleur sort disponible
        }
    }

    //B10 : Puis-je attaquer des cibles avec un sort disponible ?
    void B10()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " B10");
        enemiesHittable = GetEnemiesHittable(enemies, availableDamageSpells);
        enemiesHittableInScope = GetTargetsInScope(enemiesHittable, charCell);
        if (enemiesHittableInScope.Count > 0)
        {
            P8(); //Je peux attaquer des cibles avec un sort disponible
        }
        else
        {
            P9(); //Je ne peux pas attaquer des cibles avec un sort disponible
        }
    }


    // ~~~~~~~~~~~~~~~~ Process : fonctions PX qui correspondent à une action dans le flowchart ~~~~~~~~~~~~~~~~

    //P1 : Je lance un sort sur  une case touchant une cible tuable en fonction du sort qui utilise  
    //le moins de PA et qui a le meilleur ratio ennemis alliés touchés 
    void P1()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " P1");
        (Target chosenTarget, Spell chosenSpell) = ChooseKillableTarget(enemiesKillableInScope);
        GameCell targetCell = gridManager.GetCellOf(chosenTarget.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, targetCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<DamageSpell>(inScopeCells, chosenSpell));
    }

    //P2 : Fin de mon tour
    void P2()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " P2");
        //End of turn
        canPlay = false;
    }

    //P3 : Je me déplace de 1 vers la cible la plus proche si il existe un chemin et si j'ai assez de PM
    void P3()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " P3");
        if (character.pm.IsEnough(1))
        {
            //Get path to the nearest enemy
            List<GameCell> pathToTarget = ShortestPathTo(enemiesCells[0]);
            if (pathToTarget != null)
            {
                MoveTo(pathToTarget[0]); //Move to next cell in path to target
            }
            else
            {
                canPlay = false;
            }
        }
        else
        {
            canPlay = false;
        }
    }

    //P4 : Je me buff avec le premier sort disponible (tiré aléatoirement) en essayant de toucher le plus d'alliés et le moins d'ennemis
    void P4()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " P4");
        Spell buffSpell = availableBuffSpells[Utils.RandomIndex(availableBuffSpells.Count)]; //Rand
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(buffSpell, charCell, charCell);
        GameCell chosenCell = ChooseCellToThrowSpell<BuffSpell>(inScopeCells, buffSpell);
        battleManager.ThrowSpell(gameObject, buffSpell, chosenCell);
    }

    //P5 : Je me déplace de 1 vers la case la plus proche à portée de la première cible tuable
    void P5()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " P5");
        List<GameCell> path = CanGoTo(cellToGo); //Get path to the cell
        if (path != null) MoveTo(path[0]); //Move to next cell in path to target
        else canPlay = false;
    }

    //P6 : Je lance le sort sur une case touchant une cible en fonction du ratio ennemis alliés touchés 
    void P6()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " P6");
        //We get the first target with the first spell as all target are checked with one same spell
        Target chosenTarget = enemiesHittableBestSpellInScope[0];
        Spell chosenSpell = enemiesHittableBestSpellInScope[0].spells[0];
        GameCell targetCell = gridManager.GetCellOf(chosenTarget.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, targetCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<DamageSpell>(inScopeCells, chosenSpell));
    }

    //P7 : Je me déplace de 1 vers la case la plus proche à portée de la première cible
    void P7()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " P7");
        List<GameCell> path = CanGoTo(cellToGo); //Get path to the cell
        if (path != null) MoveTo(path[0]); //Move to next cell in path to target
        else canPlay = false;    
    }

    // P8 : Je lance un sort sur une case touchant une cible en fonction du sort qui a le plus de damage et du ratio ennemis alliés touchés 
    void P8()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " P8");
        //We get the best target to hit
        (Target chosenTarget, Spell chosenSpell) = ChooseHittableTarget(enemiesHittableInScope);
        GameCell targetCell = gridManager.GetCellOf(chosenTarget.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, targetCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<DamageSpell>(inScopeCells, chosenSpell));
    }

    //P9 : Je me déplace de 1 vers la cible la plus proche si il existe un chemin et si j'ai assez de PM
    void P9()
    {
        GameLog.Log("[IOP_AI] " + gameObject.name + " P9");
        if (character.pm.IsEnough(1))
        {
            //Get path to the nearest enemy
            List<GameCell> pathToTarget = ShortestPathTo(enemiesCells[0]);
            if (pathToTarget != null)
            {
                MoveTo(pathToTarget[0]); //Move to next cell in path to target
            }
            else
            {
                canPlay = false;
            }
        }
        else
        {
            canPlay = false;
        }
    }
}