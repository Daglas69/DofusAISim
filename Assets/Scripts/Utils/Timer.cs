using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer
{
    public float waitTime;
    private float completionTime;
 
    public Timer(float w)
    {
        waitTime = w;
        Reset();
    }

    public void Reset()
    {
        completionTime = Time.time + waitTime;
    }
 
    public bool IsReady { get { return Time.time >= completionTime; } }
}

public class Delay
{
    public float maxTime;
    private float currentTime;

    public Delay(float t)
    {
        maxTime = t;
        currentTime = 0f;
    }

    public void Reset()
    {
        currentTime = 0f;
    }

    public void Update(float t)
    {
        currentTime += t;
    }

    public bool IsReady { get { return currentTime >= maxTime; } }
}