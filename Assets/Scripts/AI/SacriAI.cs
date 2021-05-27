using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/* Arbre de décision
 EDIT : https://lucid.app/lucidchart/invitations/accept/inv_bbb15076-a945-4fc6-b1d4-c07559c8b259?viewport_loc=7577%2C-874%2C2710%2C2105%2C0_0
 VIEW : https://lucid.app/lucidchart/86f55ce8-c79f-404c-8649-72fecc07ccde/view
*/

public class SacriAI : AI
{
    //////////////////////////////////////////////// Variables /////////////////////////////////////////////////////
    private List<BuffSpell> availableBuffSpells; //Available buff spells of the character (enough pa, no cooldown)
    private List<DebuffSpell> availableDebuffSpells; //Available debuff spells of the character (enough pa, no cooldown)
    private List<DamageSpell> availableDamageSpells; //Available damage spells of the character (enough pa, no cooldown)
    private List<Target> enemiesKillable; //Enemies killable by the character from a list of spell
    private List<Target> enemiesKillableInScope; //Enemies killable that are in scope of the character
    private GameCell cellToGo; //Gamecell to go on for movement actions
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
        availableDebuffSpells = null;
        availableDamageSpells = null;
        enemiesKillable = null;
        enemiesKillableInScope = null;
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

    //B0 : Ai-je des sorts d'attaque disponibles ?
    void B0()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B0");
        availableDamageSpells = character.GetAvailableSpells<DamageSpell>();
        if (availableDamageSpells.Count > 0)
        {
            B1(); //J'ai des sorts d'attaque disponibles
        }
        else
        {
            B3(); //Je n'ai pas de sorts d'attaque disponibles
        }
    }


    //B1 : Suis-je à portée de cibles tuables avec un sort d'attaque disponible ?
    void B1()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B1");
        enemiesKillable = GetEnemiesKillable(enemies, availableDamageSpells);
        enemiesKillableInScope = GetTargetsInScope(enemiesKillable, charCell);
        if (enemiesKillableInScope.Count > 0)
        {
            P1(); //Je suis a portée de cibles tuables avec un sort disponible
        }
        else
        {
            B2(); //Je ne suis pas à portée de cibles tuables avec un sort disponible
        }
    }


    //B2 : Puis-je me mettre à portée de cibles tuables avec un sort disponible ?
    void B2()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B2");
        cellToGo = ChooseCellToMoveInScopeOfTarget(enemiesKillable);
        if (cellToGo != null)
        {
            P2(); //Je peux me mettre à portée de cibles tuables avec un sort disponible
        }
        else
        {
            B3(); //Je ne peux pas me mettre à portée de cibles tuables avec un sort disponible
        }
    }

    //B3 : Puis-je me buff ?
    void B3()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B3");
        availableBuffSpells = character.GetAvailableSpells<BuffSpell>();
        if (availableBuffSpells.Count > 0)
        {
            P3(); //Je peux me buff
        }
        else
        {
            B4(); //Je ne peux pas me buff
        }
    }

    //B4 : Suis-je au corps à corps d'un ennemi ?
    void B4()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B4");
        bool isCacToEnemy = enemiesCells.Any(x => gridManager.ManhattanDistance(charCell, x) == 1);
        if (isCacToEnemy)
        {
            B5(); //Je suis au corps à corps d'un ennemi
        }
        else
        {
            B6(); //Je ne suis pas au corps à corps d'un ennemi
        }
    }

    //B5 : Ai-je des sorts d'attraction disponibles ?
    void B5()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B5");
        availableDamageSpells = character.GetAvailableSpells<DamageSpell>();
        availableDamageSpells = availableDamageSpells.Where(x => x.attract > 0).ToList();
        if (availableDamageSpells.Count > 0)
        {
            B7(); //J'ai des sorts d'attraction disponibles
        }
        else
        {
            B10(); //Je n'ai pas de sorts d'attraction disponibles
        }
    }

    //B6 : Ai-je des sorts d'attraction ou de rapprochement disponibles ?
    void B6()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B6");
        availableDamageSpells = character.GetAvailableSpells<DamageSpell>();
        availableDamageSpells = availableDamageSpells.Where(x => (x.attract > 0 || x.approach)).ToList();
        if (availableDamageSpells.Count > 0)
        {
            B8(); //J'ai des sorts d'attraction ou de rapprochement disponibles
        }
        else
        {
            B14(); //Je n'ai pas de sorts d'attraction ou de rapprochement disponibles
        }
    }

    //B7 : Suis-je à portée pour utiliser un sort d'attraction sur un ennemi non au corps à corps avec moi ?
    void B7()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B7");
        enemiesHittable = GetEnemiesHittable(enemies, availableDamageSpells);
        enemiesHittable = enemiesHittable.Where(x => gridManager.ManhattanDistance(charCell, gridManager.GetCellOf(x.targetObj)) > 1).ToList();
        enemiesHittableInScope = GetTargetsInScope(enemiesHittable, charCell);
        if (enemiesHittableInScope.Count > 0)
        {
            P5(); //Je suis à portée pour utiliser un sort d'attraction sur un ennemi non au corps à corps avec moi
        }
        else
        {
            B9(); //Je ne suis pas à portée pour utiliser un sort d'attraction sur un ennemi non au corps à corps avec moi
        }
    }

    //B8 : Suis-je à portée d'une cible avec un sort d'attraction ou de rapprochement ?
    void B8()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B8");
        enemiesHittable = GetEnemiesHittable(enemies, availableDamageSpells);
        enemiesHittableInScope = GetTargetsInScope(enemiesHittable, charCell);
        if (enemiesHittableInScope.Count > 0)
        {
            P6(); //Je suis à portée pour utiliser un sort d'attraction ou de rapprochement
        }
        else
        {
            B14(); //Je ne suis pas à portée pour utiliser un sort d'attraction ou de rapprochement
        }
    }

    //B9 : Puis-je me mettre à portée pour utiliser un sort d'attraction sur un ennemi non au corps à corps avec moi,
    //en utilisant moins de la moitié de mes PM  ?
    void B9()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B9");
        cellToGo = ChooseCellToMoveInScopeOfTarget(enemiesHittable, character.pm.current/2);
        if (cellToGo != null)
        {
            P7(); //Je peux me mettre à portée pour utiliser un sort d'attraction sur un ennemi
        }
        else
        {
            B10(); //Je ne peux pas me mettre à portée pour utiliser un sort d'attraction sur un ennemi
        }
    }

    //B10 : Ai-je des sorts de debuff disponibles ?
    void B10()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B10");
        availableDebuffSpells = character.GetAvailableSpells<DebuffSpell>();
        if (availableDebuffSpells.Count > 0)
        {
            B11(); //J'ai des sorts de debuff disponibles
        }
        else
        {
            B12(); //Je n'ai pas de sorts de debuff disponibles
        }
    }

    //B11 : Suis-je à portée de cibles avec un sort de debuff disponible ?
    void B11()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B11");
        enemiesHittable = GetEnemiesHittable(enemies, availableDebuffSpells);
        enemiesHittableInScope = GetTargetsInScope(enemiesHittable, charCell);
        if (enemiesHittableInScope.Count > 0)
        {
            P8(); //Je suis à portée de cibles avec un sort de debuff disponible
        }
        else
        {
            B12(); //Je ne suis pas à portée de cibles avec un sort de debuff disponible
        }
    }

    //B12 : Ai-je des sorts d'attaque disponibles ?
    void B12()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B12");
        availableDamageSpells = character.GetAvailableSpells<DamageSpell>();
        if (availableDamageSpells.Count > 0)
        {
            B13(); //J'ai des sorts d'attaque disponibles
        }
        else
        {
            P9(); //Je n'ai pas de sorts d'attaque disponibles
        }
    }

    //B13 : Suis-je à portée de cibles avec un sort d'attaque disponible ?
    void B13()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B13");
        enemiesHittable = GetEnemiesHittable(enemies, availableDamageSpells);
        enemiesHittableInScope = GetTargetsInScope(enemiesHittable, charCell);
        if (enemiesHittableInScope.Count > 0)
        {
            P10(); //Je suis à portée de cibles avec un sort d'attaque disponible
        }
        else
        {
            P9(); //Je ne suis pas à portée de cibles avec un sort d'attaque disponible
        }
    }

    //B14 : Puis-je me rapprocher d'un ennemi ?
    void B14()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " B14");
        //Only if character has still PM
        //We check if at the beginning to optimize => function's code is bigger
        if (character.pm.current > 0)
        {
            //We search an ennemi with a possible path
            //Ordered by nearest
            List<GameCell> path = null;
            foreach (GameCell cell in enemiesCells)
            {
                path = ShortestPathTo(cell);
                if (path != null) break;
            }
            if (path != null)
            {
                cellToGo = path[0]; //Go to next cell in path (more opti as it moves 1 by 1)
                P4(); //Je peux me rapprocher d'un ennemi 
            }
            else
            {
                B10(); //Je ne peux pas me rapprocher d'un ennemi
            }
        }
        else
        {
            B10(); //Je ne peux pas me rapprocher d'un ennemi
        }
    }


    // ~~~~~~~~~~~~~~~~ Process : fonctions PX qui correspondent à une action dans le flowchart ~~~~~~~~~~~~~~~~

    //P1 : Je lance un sort sur une case touchant une cible tuable en fonction du sort
    //qui utilise le moins de PA et qui a le meilleur ratio ennemis alliés touchés 
    void P1()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " P1");
        (Target chosenTarget, Spell chosenSpell) = ChooseKillableTarget(enemiesKillableInScope);
        GameCell targetCell = gridManager.GetCellOf(chosenTarget.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, targetCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<DamageSpell>(inScopeCells, chosenSpell));
    }

    //P2 : Je me déplace de 1 vers la case la plus proche à portée de la première cible tuable
    void P2()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " P2");
        List<GameCell> path = CanGoTo(cellToGo); //Get path to the cell
        if (path != null) MoveTo(path[0]); //Move to next cell in path to target
        else canPlay = false;
    }

    //P3 : Je me buff avec le premier sort disponible (tiré aléatoirement)
    void P3()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " P3");
        Spell buffSpell = availableBuffSpells[Utils.RandomIndex(availableBuffSpells.Count)]; //Rand
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(buffSpell, charCell, charCell);
        GameCell chosenCell = ChooseCellToThrowSpell<BuffSpell>(inScopeCells, buffSpell);
        battleManager.ThrowSpell(gameObject, buffSpell, chosenCell);
    }

    //P4 : Je me rapproche de 1 vers l'ennemi le plus proche
    void P4()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " P4");
        List<GameCell> path = CanGoTo(cellToGo); //Get path to the cell
        if (path != null) MoveTo(path[0]); //Move to next cell in path to target
        else canPlay = false;
    }

    //P5 : Je lance le sort sur la cible à portée la plus loin de moi
    void P5()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " P5");
        Target chosenTarget = enemiesHittableInScope.OrderByDescending(
            x => gridManager.ManhattanDistance(charCell, gridManager.GetCellOf(x.targetObj))).First();
        Spell chosenSpell = chosenTarget.spells.OrderByDescending(x => (x as DamageSpell).attract).First();
        GameCell targetCell = gridManager.GetCellOf(chosenTarget.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, targetCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<DamageSpell>(inScopeCells, chosenSpell));
    }

    //P6 : Je lance le sort le plus adapté sur la première cible à portée
    void P6()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " P6");
        Target chosenTarget = enemiesHittableInScope.OrderBy(
            x => gridManager.ManhattanDistance(charCell, gridManager.GetCellOf(x.targetObj))).First();
        Spell chosenSpell = chosenTarget.spells
            .OrderBy(x => (x as DamageSpell).approach ? 0 : 1)
            .ThenByDescending(x => (x as DamageSpell).attract).First();
        GameCell targetCell = gridManager.GetCellOf(chosenTarget.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, targetCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<DamageSpell>(inScopeCells, chosenSpell));
    }

    //P7 : Je me déplace de 1 vers la case la plus proche à portée de la première cible
    void P7()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " P7");
        List<GameCell> path = CanGoTo(cellToGo); //Get path to the cell
        if (path != null) MoveTo(path[0]); //Move to next cell in path to target
        else canPlay = false;
    }

    //P8 : Je lance le sort de debuff le plus adpaté sur une case touchant la cible
    //avec le plus de vie et en fonction du ratio ennemis alliés touchés
    void P8()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " P8");
        Target chosenTarget = enemiesHittableInScope.OrderByDescending(x => x.targetObj.GetComponent<Character>().life.current).First();
        Spell chosenSpell = chosenTarget.spells[0]; //To choose better option
        GameCell targetCell = gridManager.GetCellOf(chosenTarget.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, targetCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<DamageSpell>(inScopeCells, chosenSpell));
    }

    //P9 : Fin de mon tour
    void P9()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " P9");
        canPlay = false;
    }

    //P10 : Je lance le sort de d'attaque avec le plus de damage sur une case touchant la cible
    //avec le moins de vie, et en fonction du ratio ennemis alliés touchés
    void P10()
    {
        GameLog.Log("[SACRI_AI] " + gameObject.name + " P10");
        Target chosenTarget = enemiesHittableInScope.OrderBy(x => x.targetObj.GetComponent<Character>().life.current).First();
        Spell chosenSpell = chosenTarget.spells.OrderBy(x => (x as DamageSpell).damage).First();
        GameCell targetCell = gridManager.GetCellOf(chosenTarget.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, targetCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<DamageSpell>(inScopeCells, chosenSpell));
    }
}