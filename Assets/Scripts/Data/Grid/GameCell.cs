using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//We store (x,y) index of the cell in our grid array because it is easy to get
//the cell from the world position but harder to get the index from the world
//Hence we save compute time
public class GameCell
{
    public int ind_x; //X index of the cell in our grid array
    public int ind_y; //Y index of the cell in our grid array
    public Vector3 world_pos; //Position of the cell in the world (centered)
    public Vector3Int tilemap_pos; //Position of the cell in the tilemap (integer)
    public Character occupying; //If a character is on the cell (null if not)
    public bool walkable; //If an entity can walk on it
    public bool blocking; //If the tile block the scope

    public GameCell(int x, int y, Vector3 wp, Vector3Int tp, bool is_walkable, bool is_blocking)
    {
        ind_x = x;
        ind_y = y;
        world_pos = wp;
        tilemap_pos = tp;
        occupying = null;
        walkable = is_walkable;
        blocking = is_blocking;
    }

    public bool IsOccupied()
    {
        return occupying != null;
    }
}

