using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class QlearnTools
{
    //Do the chosen action
    //Hardcoded for the moment as we defined all possible states
    public static void ApplyAction(QlearnManager manager, Player p, int action)
    {
        Character character = p.agent.GetComponent<Character>();
        GameCell charCell = manager.gridManager.GetCellOf(p.agent);
        GameCell enemyCell = manager.gridManager.GetCellOf(p.enemy);
        int distanceToEnemy = manager.gridManager.ManhattanDistance(charCell, enemyCell);

        //All possible cells to go
        List<(GameCell,int)> cellsWhereCanGoTo = manager.battleManager.GetAllCellsWhereCharCanMoveTo(character);
        //Compute distance to enemy from possible cells where char can go
        //Linked to the size path
        List<(int,int)> newDistancesToEnemy = new List<(int,int)>();
        foreach ((GameCell cell, int pathCount) in cellsWhereCanGoTo)
        {
            int distance = manager.gridManager.ManhattanDistance(cell, enemyCell);
            newDistancesToEnemy.Add((distance, pathCount));
        }
        
        //First we compute the next state
        p.currentState = ComputeNextState(manager, p, p.currentState, action);

        if (action == 1) //Réduire la distance de 1
        {
            GameLog.Log("[QLEARN] Je réduis la distance de 1");
            int ind = newDistancesToEnemy.IndexOf((distanceToEnemy - 1, 1));
            GameCell cellToGo = cellsWhereCanGoTo[ind].Item1;
            List<GameCell> path = manager.battleManager.CanCharGoTo(character, cellToGo);
            manager.battleManager.Move(p.agent, path);
        }

        if (action == 2) //Réduire la distance de 2
        {
            GameLog.Log("[QLEARN] Je réduis la distance de 2");
            int ind = newDistancesToEnemy.IndexOf((distanceToEnemy - 2, 2));
            GameCell cellToGo = cellsWhereCanGoTo[ind].Item1;
            List<GameCell> path = manager.battleManager.CanCharGoTo(character, cellToGo);
            manager.battleManager.Move(p.agent, path);
        }

        if (action == 3) //Réduire la distance de 3
        {
            GameLog.Log("[QLEARN] Je réduis la distance de 3");
            int ind = newDistancesToEnemy.IndexOf((distanceToEnemy - 3, 3));
            GameCell cellToGo = cellsWhereCanGoTo[ind].Item1;
            List<GameCell> path = manager.battleManager.CanCharGoTo(character, cellToGo);
            manager.battleManager.Move(p.agent, path);
        }

        if (action == 4) //Augmente la distance de 1
        {
            GameLog.Log("[QLEARN] J augmente la distance de 1");
            int ind = newDistancesToEnemy.IndexOf((distanceToEnemy + 1, 1));
            GameCell cellToGo = cellsWhereCanGoTo[ind].Item1;
            List<GameCell> path = manager.battleManager.CanCharGoTo(character, cellToGo);
            manager.battleManager.Move(p.agent, path);
        }

        if (action == 5) //Augmente la distance de 2
        {
            GameLog.Log("[QLEARN] J augmente la distance de 2");
            int ind = newDistancesToEnemy.IndexOf((distanceToEnemy + 2, 2));
            GameCell cellToGo = cellsWhereCanGoTo[ind].Item1;
            List<GameCell> path = manager.battleManager.CanCharGoTo(character, cellToGo);
            manager.battleManager.Move(p.agent, path);
        }

        if (action == 6) //Augmente la distance de 3
        {
            GameLog.Log("[QLEARN] J augmente la distance de 3");
            int ind = newDistancesToEnemy.IndexOf((distanceToEnemy + 3, 3));
            GameCell cellToGo = cellsWhereCanGoTo[ind].Item1;
            List<GameCell> path = manager.battleManager.CanCharGoTo(character, cellToGo);
            manager.battleManager.Move(p.agent, path);
        }

        if (action == 7) //Attaquer avec le sort longue distance
        {
            GameLog.Log("[QLEARN] J'attaque de loin");
            manager.battleManager.ThrowSpell(p.agent, character.spells[Cte.LONG_RANGE_SPELL_IND], enemyCell);
        }

        if (action == 8) //Attaquer avec le sort corps à corps
        {
            GameLog.Log("[QLEARN] J'attaque de près");
            manager.battleManager.ThrowSpell(p.agent, character.spells[Cte.SHORT_RANGE_SPELL_IND], enemyCell);
        }

        if (action == 9) //Se soigner
        {
            GameLog.Log("[QLEARN] Je me heal");
            manager.battleManager.ThrowSpell(p.agent, character.spells[Cte.HEAL_SPELL_IND], charCell);
        }

        if (action == 0) //Ne rien faire
        {
            GameLog.Log("[QLEARN] Je ne fais rien");
            p.agent.GetComponent<AI>().SetCanPlay(false);
        }
    }


    //We test all actions and add to the output if it is possible to make it
    //Hardcoded for the moment as we defined all possible states
    public static List<int> ComputePossiblesActions(QlearnManager manager, Player p)
    {
        List<int> output = new List<int>();

        Character character = p.agent.GetComponent<Character>();
        GameCell charCell = manager.gridManager.GetCellOf(p.agent);
        GameCell enemyCell = manager.gridManager.GetCellOf(p.enemy);
        int distanceToEnemy = manager.gridManager.ManhattanDistance(charCell, enemyCell);

        //Long range spell
        List<GameCell> inScopeCellsLR = manager.battleManager.GetInScopeOfTargetCells(character.spells[Cte.LONG_RANGE_SPELL_IND], charCell, enemyCell);
        //Short range spell
        List<GameCell> inScopeCellsSR = manager.battleManager.GetInScopeOfTargetCells(character.spells[Cte.SHORT_RANGE_SPELL_IND], charCell, enemyCell);
        //Heal spell
        List<GameCell> inScopeCellsH = manager.battleManager.GetInScopeOfTargetCells(character.spells[Cte.HEAL_SPELL_IND], charCell, charCell);

        //All possible cells to go
        List<(GameCell, int)> cellsWhereCanGoTo = manager.battleManager.GetAllCellsWhereCharCanMoveTo(character);
        //Compute distance to enemy from possible cells where char can go
        //Linked to the size path
        List<(int, int)> newDistancesToEnemy = new List<(int, int)>();
        foreach ((GameCell cell, int pathCount) in cellsWhereCanGoTo)
        {
            int distance = manager.gridManager.ManhattanDistance(cell, enemyCell);
            newDistancesToEnemy.Add((distance, pathCount));
        }

        //3 : Réduire la distance de 3
        if (newDistancesToEnemy.Contains((distanceToEnemy - 3, 3)))
        {
            output.Add(3);
        }

        //2 : Réduire la distance de 2
        if (newDistancesToEnemy.Contains((distanceToEnemy - 2, 2)))
        {
            output.Add(2);
        }

        //1 : Réduire la distance de 1
        if (newDistancesToEnemy.Contains((distanceToEnemy - 1, 1)))
        {
            output.Add(1);
        }

        //6 : Augmenter la distance de 3
        if (newDistancesToEnemy.Contains((distanceToEnemy + 3, 3)))
        {
            output.Add(6);
        }

        //5 : Augmenter la distance de 2
        if (newDistancesToEnemy.Contains((distanceToEnemy + 2, 2)))
        {
            output.Add(5);
        }

        //4 : Augmenter la distance de 1
        if (newDistancesToEnemy.Contains((distanceToEnemy + 1, 1)))
        {
            output.Add(4);
        }

        //7 : Attaquer avec le sort longue distance
        if (character.pa.IsEnough(Cte.SPELL_PA) && inScopeCellsLR.Count > 0)
        {
            output.Add(7);
        }

        //8 : Attaquer avec le sort corps à corps
        if (character.pa.IsEnough(Cte.SPELL_PA) && inScopeCellsSR.Count > 0)
        {
            output.Add(8);
        }

        //9 : Se soigner
        if (character.life.current < character.life.max && character.pa.IsEnough(Cte.SPELL_PA) && inScopeCellsH.Count > 0 && !character.spells[Cte.HEAL_SPELL_IND].HasActiveCooldown())
        {
            output.Add(9);
        }

        //0 : Ne rien faire
        output.Add(0);

        return output;
    }


    //Compute the state according to the action
    //Hardcoded for the moment as we defined all possible states
    //[WARNING] Code can not be reduced in computing each field of the state,
    //          because it is used to both simulate and apply a state
    public static State ComputeNextState(QlearnManager manager, Player p, State currentState, int action)
    {
        /*
         // DO NOT DO THIS
        int distance = manager.gridManager.ManhattanDistance(
            manager.gridManager.GetCellOf(p.agent),
            manager.gridManager.GetCellOf(p.enemy)
        );
        int agentLife = p.agent.GetComponent<Character>().life.current;
        agentLife = agentLife <= 0 ? -1 : agentLife/10;
        int enemyLife = p.agent.GetComponent<Character>().life.current;
        enemyLife = enemyLife <= 0 ? -1 : enemyLife/10;
        return new State(distance, agentLife, enemyLife);
        */

        State nextState = currentState.Copy();
        
        switch (action)
        {
            case 1: //Réduire la distance de 1
            {
                nextState.distance -= 1;
                break;
            }

            case 2: //Réduire la distance de 2
            {
                nextState.distance -= 2;
                break;
            }

            case 3: //Réduire la distance de 3
            {
                nextState.distance -= 3;
                break;
            }

            case 4: //Augmenter la distance de 1
            {
                nextState.distance += 1;
                break;
            }

            case 5: //Augmenter la distance de 2
            {
                nextState.distance += 2;
                break;
            }

            case 6: //Augmenter la distance de 3
            {
                nextState.distance += 3;
                break;
            }

            case 7: //Attaquer avec le sort longue distance
            {
                Points life = p.enemy.GetComponent<Character>().life;
                int newLife = life.current - Cte.LONG_RANGE_SPELL_DAMAGE;
                newLife = newLife <= 0 ? -1 : newLife;
                nextState.enemyLife = newLife / 10;
                break;
            }

            case 8: //Attaquer avec le sort corps à corps
            {
                Points life = p.enemy.GetComponent<Character>().life;
                int newLife = life.current - Cte.SHORT_RANGE_SPELL_DAMAGE;
                newLife = newLife <= 0 ? -1 : newLife;
                nextState.enemyLife = newLife / 10;
                break;
            }

            case 9: //Se soigner
            {
                Points life = p.agent.GetComponent<Character>().life;
                int newLife = life.current + Cte.HEAL_SPEAL_LIFE;
                newLife = newLife > life.max ? life.max : newLife; 
                nextState.agentLife = newLife / 10;
                break;
            }

            case 0: break; //Ne rien faire
        }

        return nextState;
    }

    /*
    Liste de tous les états possibles, représentés avec des int, et les modifications du state correspondantes ->

    0 : Ne rien faire / rien ne change

    - Réduire la distance 
       1 : de 1 / distance-1
       2 : de 2 / distance-2
       3 : de 3 / distance-3

    - Augmenter la distance
       4 : de 1 / distance+1
       5 : de 2 / distance+2
       6 : de 3 / distance+3

    - Attaquer
        7 : avec le sort longue distance / enemyLife-x
        8 : avec le sort de corps à corps / enemyLife-yx

    9 : Se soigner / agentLife+

    -1 : action inconnue
    */
}