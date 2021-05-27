using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InitVariables
{
    //It is set to true when menu init variables
    public static bool is_init_from_menu = false;

    //Variables to init from the menu
    public static bool full_speed = false;
    public static int loop_parties = 1;
    public static bool group_play_together = false;
    public static float play_time = 30f;
    public static bool grp1_leader = false;
    public static GameObject[] grp1_chars = null;
    public static bool grp2_leader = false;
    public static GameObject[] grp2_chars = null;
    public static bool is_dungeon_mode = false;
    public static int numberOfDungeons = 3;
    public static int maxGroupSize = 6;

    public static void Reset()
    {
        is_init_from_menu = false;
        full_speed = false;
        loop_parties = 1;
        group_play_together = false;
        play_time = 30f;
        grp1_leader = false;
        grp1_chars = null;
        grp2_leader = false;
        grp2_chars = null;
        is_dungeon_mode = false;
        numberOfDungeons = 3;
        maxGroupSize = 6;
    }
}
