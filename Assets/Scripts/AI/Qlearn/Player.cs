using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Basé sur https://github.com/thibo73800/aihub/blob/master/rl/sticks.py
/// https://www.youtube.com/watch?v=OKTjheBEvDY&t=1361s
/// </summary>
[System.Serializable]
public class Player 
{
    public QlearnManager manager;
    public GameObject agent;
    public GameObject enemy;
    public State currentState;
    public State previousState;
    public List<Transition> historique;
    public Dictionary<int, double> v; //dictionnaire où on conserve l'esperance de gain pour chaque état
    public int win_nb = 0;
    public int lose_nb = 0;
    public double epsilon = QlearnVariables.EPSILON;
    public bool trainable = QlearnVariables.TRAINABLE;
    


    public Player(QlearnManager manager, GameObject agent, GameObject enemy)
    { 
        this.historique = new List<Transition>();
        this.manager = manager;
        this.agent = agent;
        this.enemy = enemy;
        //To Modify (compute real distance)
        //First state
        this.currentState = new State(manager.state_distanceMax, manager.state_p1MaxLife, manager.state_p2MaxLife);
        this.previousState = null;
    }


    public void ResetStats()
    {
        win_nb = 0;
        lose_nb = 0;
    }


    public int GreedyStep(State state, List<int> allPossiblesActions)
    {
        double vmax = double.MinValue;
        int vi = -1;
        State nextState;
        foreach(int action in allPossiblesActions)
        {
            nextState = QlearnTools.ComputeNextState(manager, this, state, action);
            if(nextState.AgentIsStillAlive() && v[nextState.Key] > vmax) //We check if still alive to prevent from suicide
            {
                vmax = v[nextState.Key];
                vi = action;
            }
        }
        return vi;
    }


    public int Play()
    {
        int action;
        List<int> allPossiblesActions = QlearnTools.ComputePossiblesActions(manager, this);
        double rnd = Random.value;
        if(rnd < epsilon)
        {
            action = allPossiblesActions[Random.Range(0, allPossiblesActions.Count)];
        }
        else
        {
            action = GreedyStep(currentState, allPossiblesActions);
        }
        return action;
    }


    public void AddTransition(Transition t)
    {
        historique.Add(t);
    }


    public void Train() //TCHOU TCHOU
    {
        if (!trainable) return;

        historique.Reverse();
        foreach(Transition transition in historique)
        {
            State s = transition.state;
            int reward = transition.reward;
            State sp = transition.sp;
            double coeff = QlearnVariables.COEFF_TRAIN_BACKTRACE;
            double updateCoeff = QlearnVariables.COEFF_TRAIN_UPDATE;
            if (reward == 0)
            {
                v[s.Key] = v[s.Key] + coeff * (updateCoeff*v[sp.Key] - v[s.Key]);
            }
            else
            {
                v[s.Key] = v[s.Key] + coeff * (updateCoeff*reward - v[s.Key]);
            }
        }

        historique = new List<Transition>();
    }
}