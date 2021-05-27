using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameLog
{
    //Type (coded as an int) associated to a bool
    //True to display the log messages of the type given
    public static Dictionary<string, bool> logs = new Dictionary<string, bool>
    {
        { LogType.Char, false },
        { LogType.Battle, false },
        { LogType.Grid, false },
        { LogType.Leader, false },
        { LogType.Qlearn, false },
        { LogType.AI, false },
        { LogType.AI_SIMPLE, false },
        { LogType.CRA_AI, false },
        { LogType.ENI_AI, false },
        { LogType.IOP_AI, false },
        { LogType.SACRI_AI, false },
        { LogType.DIJKSTRA, false }
    };
    public static bool LogOff = false; //Turn to true to remove all logs

    public static void Log(string msg)
    {
        if (LogOff) return;

        string key = msg.Split(' ')[0];
        if (logs.ContainsKey(key))
        {
            if (logs[key]) Debug.Log(msg);
        }
        else Debug.Log(msg);
    }
}

public static class LogType
{
    public static readonly string Char = "[CHAR]";
    public static readonly string Battle = "[BATTLE]";
    public static readonly string Grid = "[GRID]";
    public static readonly string Leader = "[LEADER]";
    public static readonly string Qlearn = "[QLEARN]";
    public static readonly string AI = "[AI]";
    public static readonly string AI_SIMPLE = "[AI_SIMPLE]";
    public static readonly string CRA_AI = "[CRA_AI]";
    public static readonly string ENI_AI = "[ENI_AI]";
    public static readonly string IOP_AI = "[IOP_AI]";
    public static readonly string SACRI_AI = "[SACRI_AI]";
    public static readonly string DIJKSTRA = "[DIJKSTRA]";
}
