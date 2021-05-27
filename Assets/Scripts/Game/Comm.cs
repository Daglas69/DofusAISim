using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

//Singleton class to handle communications between characters
public sealed class Comm
{
    private static readonly Lazy<Comm> lazy = new Lazy<Comm>(() => new Comm());
    private BattleManager battleManager;

    public static Comm Instance()
    {
        return lazy.Value;
    }

    private Comm() { }

    public void SetBattleManager(BattleManager b)
    {
        battleManager = b;
    }



    //Main function of the class
    //Used to handle communications between characters
    public void SendMessageTo<T>(Msg<T> msg, int uuid)
    {
        //We get all the characters in the game
        var chars = battleManager.grp1_chars.Concat(battleManager.grp2_chars).Where(x => x != null).Select(x => x.GetComponent<Character>()).ToArray();

        //We search the character with the given UUID
        foreach (Character character in chars)
        {
            if (character.UUID() == uuid)
            {
                character.GetComponent<CharacterComm>().ReceiveMessage(msg);
                return;
            }
        }
    }
}