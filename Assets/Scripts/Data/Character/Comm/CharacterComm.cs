using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Script to send and receive messages from other characters
public class CharacterComm : MonoBehaviour
{
    //Send a message to a character defined by its UUID
    public void SendMessage<T>(Msg<T> msg, int uuid)
    {
        Comm.Instance().SendMessageTo(msg, uuid);
    }


    //Receive a message
    public void ReceiveMessage<T>(Msg<T> msg)
    {
        //Character do an action regarding the message type 
        GetComponent<Character>().HandleMessage(msg);
    }
}