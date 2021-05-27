using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//Display according to the speed of the simulation
//SLOW or FAST
//Singleton
public sealed class SpeedDisplay
{
    private bool full_speed = false; //True to run the simulation fastly
    private static readonly Lazy<SpeedDisplay> lazy = new Lazy<SpeedDisplay>(() => new SpeedDisplay());

    public static SpeedDisplay Instance()
    {
        return lazy.Value;
    }

    private SpeedDisplay(){}

    //Do action according to simulation speed setting
    //Action 1 for slow display
    //Action 2 for fast display
    public void DoDisplay(Action action1, Action action2)
    {
        if (full_speed) action2.Invoke();
        else action1.Invoke();
    }

    //Do action according to simulation speed setting
    //Do action only if speed is set to SLOW
    public void DoDisplay(Action action1)
    {
        if (!full_speed) action1.Invoke();
    }

    //Setter
    public void SetSpeed(bool speed)
    {
        full_speed = speed;
    }

    //Getter
    public bool IsFastDisplay()
    {
        return full_speed;
    }
}