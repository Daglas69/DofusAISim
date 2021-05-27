using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basé sur https://github.com/thibo73800/aihub/blob/master/rl/sticks.py
/// https://www.youtube.com/watch?v=OKTjheBEvDY&t=1361s
/// </summary>
public class State
{
    public int distance; // Manhattan distance between the two agents
    public int agentLife; // Life of the first agent represented in [-1, 10]
    public int enemyLife; // Life of the second agent represented in [-1, 10]

    //Unique int from state fields, to store it in a dict
    public int Key { get { return (distance + "," + agentLife + "," + enemyLife).GetHashCode(); } } 


    public State(int distance, int agentLife, int ennemyLife)
    {
        this.distance = distance;
        this.agentLife = agentLife;
        this.enemyLife = ennemyLife;
    }


    public State Copy()
    {
        State output = new State(distance, agentLife, enemyLife);
        return output;
    }


    public bool AgentIsStillAlive()
    {
        return agentLife >= 0;
    }


    public State Reverse()
    {
        State output = this.Copy();
        output.agentLife = enemyLife;
        output.enemyLife = agentLife;
        return output;
    }


    //Hardcoded for the moment
    //Init a dictionnary with all possible states coded as int hashed key
    public static Dictionary<int, double> CreateAllStatesDict(QlearnManager manager)
    {
        Dictionary<int, double> va = new Dictionary<int, double>();
        for (int i = 1; i <= manager.state_distanceMax; i++) //Distance between the two agents
        {
            for (int j = -1; j <= manager.state_p1MaxLife; j++) //Life of the first agent
            {
                for (int k = -1; k <= manager.state_p2MaxLife; k++) //Life of the second agent
                {
                    State s = new State(i, j, k);
                    va.Add(s.Key, 0);
                }
            }
        }
        return va;
    }
}
