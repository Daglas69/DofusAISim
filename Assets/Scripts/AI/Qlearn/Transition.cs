using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Transition
{
    public State state;
    public int action;
    public int reward;
    public State sp; //State à t+1, donc après l'application de action à state


    public Transition(State s, int a, int r, State sp)
    {
        state = s;
        action = a;
        reward = r;
        this.sp = sp;
    }
}