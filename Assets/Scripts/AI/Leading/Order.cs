using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Leader gives order to its allies
public class Order
{
    public GameObject[] targets = null; //Targets to focus
    public GameCell cell = null; //Cell to move on
    //public GameObject[] group; //Group following the order
}

