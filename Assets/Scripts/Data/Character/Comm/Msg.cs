using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Msg<T>
{
    public string type;
    public T msg;

    public static Msg<Order> OrderMsg(Order msgContent)
    {
        Msg<Order> msg = new Msg<Order>();
        msg.msg = msgContent;
        msg.type = MsgType.OrderMsg;
        return msg;
    }
}
