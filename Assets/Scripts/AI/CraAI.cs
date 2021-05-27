using System;
using System.Collections.Generic;
using System.Linq;

/* Arbre de décision
 EDIT : 
  1. https://lucid.app/lucidchart/invitations/accept/61f6f7a8-d16a-4f2e-995a-94e752d4e807
  2. https://lucid.app/lucidchart/invitations/accept/9fa030a4-2c1a-497b-84f5-af94fbb4337c 
 VIEW:
  1. https://lucid.app/lucidchart/2c0a4479-440f-4091-adfb-c79f06ae7ce4/view
  2. https://lucid.app/lucidchart/50fa78d3-5ad4-4a24-9a07-d127b3a864e4/view
*/

public class CraAI : AI
{
    //////////////////////////////////////////////// Variables /////////////////////////////////////////////////////
    private List<BuffSpell> availableBuffSpells; //Available buff spells of the character (enough pa, no cooldown)
    private List<DamageSpell> availableDamageSpells; //Available damage spells of the character (enough pa, no cooldown)
    private List<DebuffSpell> availableDebuffSpells; //Available debuff spells of the character (enough pa, no cooldown)
    private List<Target> enemiesKillable; //Enemies killable by the character from a list of spell
    private List<Target> enemiesKillableInScope; //Enemies killable that are in scope of the character
    private GameCell cellToGo; //Gamecell to go on for movement actions
    private List<Target> enemiesHittable; //Enemies hittable by the character from a list of spell
    private List<Target> enemiesHittableInScope; //Enemies hittable that are in scope of the character
    private GroupViews enemiesViews; //Views of the enemies


    /////////////////////////////////////////////// Tools method ///////////////////////////////////////////////

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
        availableDamageSpells = null;
        availableDebuffSpells = null;
        enemiesKillable = null;
        enemiesKillableInScope = null;
        cellToGo = null;
        enemiesHittable = null;
        enemiesHittableInScope = null;
        enemiesViews = null;
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
        GameLog.Log("[CRA_AI] " + gameObject.name + " B0");
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
        GameLog.Log("[CRA_AI] " + gameObject.name + " B1");
        availableDamageSpells = character.GetAvailableSpells<DamageSpell>();
        availableDebuffSpells = character.GetAvailableSpells<DebuffSpell>();
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
        GameLog.Log("[CRA_AI] " + gameObject.name + " B2");
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
        GameLog.Log("[CRA_AI] " + gameObject.name + " B3");
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

    //B4 : Ai-je des sorts de debuff disponibles ?
    void B4()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " B4");
        availableDebuffSpells = character.GetAvailableSpells<DebuffSpell>();
        if (availableDebuffSpells.Count > 0)
        {
            B8(); //J'ai des sorts de debuff disponibles
        }
        else
        {
            B9(); //Je n'ai pas de sorts de debuff disponibles
        }
    }

    //B5 : Un ennemi peut-il me tuer ?
    void B5()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " B5");
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
        GameLog.Log("[CRA_AI] " + gameObject.name + " B6");
        availableBuffSpells = character.GetAvailableSpells<BuffSpell>();
        if (availableBuffSpells.Count > 0)
        {
            P2(); //Je peux me buff
        }
        else
        {
            B1(); //Je ne peux pas me buff
        }
    }

    //B7 : puis-je me mettre à portée de cibles tuables avec un sort disponible ?
    void B7()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " B7");
        cellToGo = ChooseCellToMoveInScopeOfTarget(enemiesKillable);
        if (cellToGo != null)
        {
            P3(); //Je peux me mettre à portée de cibles tuables avec un sort disponible
        }
        else
        {
            B10(); //Je ne peux pas me mettre à portée de cibles tuables avec un sort disponible
        }
    }

    //B8 : Suis-je à portée de cibles avec un sort de debuff disponible ?
    void B8()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " B8");
        enemiesHittable = GetEnemiesHittable(enemies, availableDebuffSpells);
        enemiesHittableInScope = GetTargetsInScope(enemiesHittable, charCell);
        if (enemiesHittableInScope.Count > 0)
        {
            P4(); //Je suis à portée de cibles avec un sort de debuff disponible
        }
        else
        {
            B11(); //Je ne suis pas à portée de cibles avec un sort de debuff disponible
        }
    }

    //B9 : Suis-je à portée de la cible la plus proche avec mon sort ayant la plus grande portée ?
    void B9()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " B9");
        Spell spellBiggestRange = character.spells.Where(x => (x as DamageSpell) != null).OrderByDescending(x => x.maxScope).First();
        if (battleManager.IsInScopeOfTarget(spellBiggestRange, charCell, enemiesCells[0]))
        {
            P8(); //Je suis à portée de la cible la plus proche avec mon sort ayant la plus grande portée
        }
        else
        {
            B17(); //Je ne suis pas à portée de la cible la plus proche avec mon sort ayant la plus grande portée
        }
    }

    //B10 : Suis-je à portée de cibles avec un sort de recul ?
    void B10()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " B10");
        availableDamageSpells = character.GetAvailableSpells<DamageSpell>().Where(x => x.push > 0).ToList();
        enemiesHittable = GetEnemiesHittable(enemies, availableDamageSpells);
        enemiesHittableInScope = GetTargetsInScope(enemiesHittable, charCell);
        if (enemiesHittableInScope.Count > 0)
        {
            P6(); //Je suis à portée de cibles avec un sort de recul
        }
        else
        {
            B12(); //Je ne suis pas à portée de cibles avec un sort de recul
        }
    }

    //B11 : Puis-je me mettre à portée de cibles avec un sort de debuff disponible ?
    void B11()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " B11");
        cellToGo = ChooseCellToMoveInScopeOfTarget(enemiesHittable);
        if (cellToGo != null)
        {
            P7(); //Je peux me mettre à portée de cibles avec un sort de debuff disponible
        }
        else
        {
            B9(); //Je ne peux pas me mettre à portée de cibles avec un sort de debuff disponible
        }
    }

    //B12 : Puis-je me mettre à portée d'une cible avec un sort de recul en utilisant moins de la moitié de mes PM ?
    void B12()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " B12");
        cellToGo = ChooseCellToMoveInScopeOfTarget(enemiesHittable, character.pm.current / 2);
        if (cellToGo != null)
        {
            P9(); //Je peux me mettre à portée d'une cible avec un sort de recul en utilisant moins de la moitié de mes PM
        }
        else
        {
            B13(); //Je ne peux pas me mettre à portée d'une cible avec un sort de recul en utilisant moins de la moitié de mes PM
        }
    }

    //B13 : Ai-je des sorts de debuff disponibles ?
    void B13()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " B13");
        availableDebuffSpells = character.GetAvailableSpells<DebuffSpell>();
        if (availableDebuffSpells.Count > 0)
        {
            B14(); //J'ai des sorts de debuff disponibles
        }
        else
        {
            B15(); //Je n'ai pas de sorts de debuff disponibles
        }
    }

    //B14 : Suis-je à portée de cibles ayant plus d'1/3 de vie avec un sort de debuff disponible ?
    void B14()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " B14");
        enemiesHittable = GetEnemiesHittable(enemies, availableDebuffSpells);
        enemiesHittable = enemiesHittable.Where(x => x.targetObj.GetComponent<Character>().life.current > x.targetObj.GetComponent<Character>().life.max/3).ToList();
        enemiesHittableInScope = GetTargetsInScope(enemiesHittable, charCell);
        if (enemiesHittableInScope.Count > 0)
        {
            P10(); //Je suis à portée de cibles avec un sort de debuff disponible
        }
        else
        {
            B16(); //Je ne suis pas à portée de cibles avec un sort de debuff disponible
        }
    }

    //B15 : Puis-je attaquer des cibles avec un sort disponible ?
    void B15()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " B15");
        availableDamageSpells = character.GetAvailableSpells<DamageSpell>(); //We get it again as we used the var for push spells
        enemiesHittable = GetEnemiesHittable(enemies, availableDamageSpells);
        enemiesHittableInScope = GetTargetsInScope(enemiesHittable, charCell);
        if (enemiesHittableInScope.Count > 0)
        {
            P11(); //Je suis à portée de cibles avec un sort disponible
        }
        else
        {
            B9(); //Je ne suis pas à portée de cibles avec un sort disponible
        }
    }

    //B16 : Puis-je me mettre à portée de cibles ayant plus d'1/3 de vie avec un sort de debuff disponible ?
    void B16()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " B16");
        cellToGo = ChooseCellToMoveInScopeOfTarget(enemiesHittable);
        if (cellToGo != null)
        {
            P12(); //Je peux me mettre à portée de cibles avec un sort de debuff disponible
        }
        else
        {
            B15(); //Je ne peux pas me mettre à portée de cibles avec un sort de debuff disponible
        }
    }

    //B17 : Suis-je à 1 case d'être à portée de la cible la plus proche avec mon sort ayant la plus grande portée ?
    void B17()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " B17");
        Spell spellBiggestRange = character.spells.Where(x => (x as DamageSpell) != null).OrderByDescending(x => x.maxScope).First();
        Target target = new Target();
        target.targetObj = enemiesCells[0].occupying.gameObject;
        target.spells.Add(spellBiggestRange);
        cellToGo = ChooseCellToMoveInScopeOfTarget(target);
        if (cellToGo != null && gridManager.ManhattanDistance(charCell, cellToGo) == 1)
        {
            P13(); //Je suis à 1 case d'être à portée de la cible la plus proche avec mon sort ayant la plus grande portée
        }
        else
        {
            P5(); //Je ne suis pas à 1 case d'être à portée de la cible la plus proche avec mon sort ayant la plus grande portée
        }
    }


    // ~~~~~~~~~~~~~~~~ Process : fonctions PX qui correspondent à une action dans le flowchart ~~~~~~~~~~~~~~~~

    //P1 : Je lance un sort sur  une case touchant une cible tuable en fonction du sort qui utilise  
    //le moins de PA et qui a le meilleur ratio ennemis alliés touchés 
    void P1()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " P1");
        (Target chosenTarget, Spell chosenSpell) = ChooseKillableTarget(enemiesKillableInScope);
        GameCell targetCell = gridManager.GetCellOf(chosenTarget.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, targetCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<DamageSpell>(inScopeCells, chosenSpell));
    }

    //P2 : Je me buff avec le premier sort disponible en essayant de toucher le plus d'alliés et le moins d'ennemis
    void P2()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " P2");
        Spell buffSpell = availableBuffSpells[0];
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(buffSpell, charCell, charCell);
        GameCell chosenCell = ChooseCellToThrowSpell<BuffSpell>(inScopeCells, buffSpell);
        battleManager.ThrowSpell(gameObject, buffSpell, chosenCell);
    }

    //P3 : Je me déplace de 1 vers la case la plus proche à portée de la première cible tuable
    void P3()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " P3");
        List<GameCell> path = CanGoTo(cellToGo); //Get path to the cell
        if (path != null) MoveTo(path[0]); //Move to next cell in path to target
        else canPlay = false;
    }

    //P4 : Je lance le premier sort (tiré aléatoirement) sur une case touchant la cible avec le plus de vie et en fonction du ratio ennemis alliés touchés
    void P4()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " P4");
        //We get the target with the upper life and we get the first debuff spell
        Target chosenTarget = enemiesHittableInScope.OrderByDescending(x => x.targetObj.GetComponent<Character>().life.current).ToList()[0];
        Spell chosenSpell = chosenTarget.spells[Utils.RandomIndex(chosenTarget.spells.Count)];
        GameCell targetCell = gridManager.GetCellOf(chosenTarget.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, targetCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<DamageSpell>(inScopeCells, chosenSpell));
    }

    //P5 : Je me déplace de 1 vers la première case à portée de la cible
    //si je le peux (chemin possible, PM restants)
    void P5()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " P5");
        if (character.pm.current > 0)
        {
            var path = ShortestPathTo(enemiesCells[0]); //Closest enemy
            if (path != null)
            {
                MoveTo(path[0]); //Move to next cell in path to target
            }
            else canPlay = false;
        }
        else canPlay = false;
    }

    //P6 : Je lance le sort avec le plus de degats sur la cible la plus proche
    void P6()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " P6");
        //We get the closest target to hit with the best spell
        Target chosenTarget = enemiesHittableInScope.OrderBy(x => gridManager.ManhattanDistance(charCell, gridManager.GetCellOf(x.targetObj))).ToList()[0];
        Spell chosenSpell = chosenTarget.spells.OrderByDescending(x => (x as DamageSpell).damage).ToList()[0];
        GameCell targetCell = gridManager.GetCellOf(chosenTarget.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, targetCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<DamageSpell>(inScopeCells, chosenSpell));
    }

    //P7 : Je me déplace de 1 vers la case à portée de la cible la plus proche
    void P7()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " P7");
        List<GameCell> path = CanGoTo(cellToGo); //Get path to the cell
        if (path != null) MoveTo(path[0]); //Move to next cell in path to target
        else canPlay = false;
    }

    //P8 : Je me déplace de 1 vers la case la plus loin des ennemis et à portée du moins d'ennemis possible
    //si je le peux (chemin possible, PM restants)
    void P8()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " P8");
        if (character.pm.current > 0)
        {
            enemiesViews = GetEnemiesViews(enemies);
            cellToGo = ChooseCellToMoveAway(enemiesViews);
            if (cellToGo != null)
            {
                List<GameCell> path = CanGoTo(cellToGo); //Get path to the cell
                if (path != null) MoveTo(path[0]); //Move to next cell in path to target
                else canPlay = false;
            }
            else canPlay = false;
        }
        else canPlay = false;
    }

    //P9 : Je me déplace de 1 vers la case à portée de la cible la plus proche
    void P9()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " P9");
        List<GameCell> path = CanGoTo(cellToGo); //Get path to the cell
        if (path != null) MoveTo(path[0]); //Move to next cell in path to target
        else canPlay = false;
    }


    //P10 : Je lance le premier sort (tiré aléatoirement) sur une case touchant la cible avec le plus de vie et en fonction du ratio ennemis alliés touchés
    void P10()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " P10");
        //We get the target with the upper life and we get the first debuff spell
        Target chosenTarget = enemiesHittableInScope.OrderByDescending(x => x.targetObj.GetComponent<Character>().life.current).ToList()[0];
        Spell chosenSpell = chosenTarget.spells[Utils.RandomIndex(chosenTarget.spells.Count)];
        GameCell targetCell = gridManager.GetCellOf(chosenTarget.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, targetCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<DamageSpell>(inScopeCells, chosenSpell));
    }

    //P11 : Je lance le sort avec le plus de degats sur une case touchant une cible
    //en fonction de la cible qui a le moins de pv et du ratio ennemis alliés touchés 
    void P11()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " P11");
        //We get the target with the upper life and we get the first debuff spell
        Target chosenTarget = enemiesHittableInScope.OrderBy(x => x.targetObj.GetComponent<Character>().life.current).ToList()[0];
        Spell chosenSpell = chosenTarget.spells.OrderByDescending(x => (x as DamageSpell).damage).ToList()[0];
        GameCell targetCell = gridManager.GetCellOf(chosenTarget.targetObj);
        List<GameCell> inScopeCells = battleManager.GetInScopeOfTargetCells(chosenSpell, charCell, targetCell);
        battleManager.ThrowSpell(gameObject, chosenSpell, ChooseCellToThrowSpell<DamageSpell>(inScopeCells, chosenSpell));
    }

    //P12 : Je me déplace de 1 vers la case à portée de la cible la plus proche
    void P12()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " P12");
        List<GameCell> path = CanGoTo(cellToGo); //Get path to the cell
        if (path != null) MoveTo(path[0]); //Move to next cell in path to target
        else canPlay = false;
    }

    //P13 : Fin de mon tour
    void P13()
    {
        GameLog.Log("[CRA_AI] " + gameObject.name + " P13");
        canPlay = false;
    }
}