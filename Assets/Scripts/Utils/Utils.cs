using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    //Randomize value order of the array
    public static void ShuffleArray<T>(T[] t)
    {
        //Knuth shuffle algorithm
        for (int i = 0; i < t.Length; i++)
        {
            T tmp = t[i];
            int r = Random.Range(i, t.Length);
            t[i] = t[r];
            t[r] = tmp;
        }
    }


    //Remove value at index passed as arg from regular array
    public static T[] RemoveAtFromArray<T>(T[] arr, int ind)
    {
        var grr = new List<T>(arr);
        grr.RemoveAt(ind);
        return grr.ToArray();
    }


    //Remove value passed as arg from regular array
    public static T[] RemoveFromArray<T>(T[] arr, T val)
    {
        var grr = new List<T>(arr);
        grr.Remove(val);
        return grr.ToArray();
    }


    //Return a random between 0 and length (exclusive)
    public static int RandomIndex(int length)
    {
        return UnityEngine.Random.Range(0, length);
    }
}
