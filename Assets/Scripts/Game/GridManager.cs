using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    public Tilemap tilemap; //Tilemap
    public GameCell[,] grid; //Grid of the tilemap
    private int size_x; //Size of the grid in x
    private int size_y; //Size of the grid in y
    private GameCellGraph graph; //To perform Dijkstra algorithm


    //Getters
    public int GetSize_x() => size_x;
    public int GetSize_y() => size_y;
    public GameCellGraph Graph() => graph;


    //Call at startup of the scene
    //Compute the grid 2D array from the tilemap
    void Awake()
    {
        //Remove extra cells that do not contain tiles
        tilemap.CompressBounds();

        var bounds = tilemap.cellBounds;
        GameLog.Log("[GRID] bounds : " + bounds); 
        size_x = bounds.size.x;
        size_y = bounds.size.y;

        grid = new GameCell[size_x,size_y];   
        
        for (int x = 0; x < size_x; x++)
        {
            for (int y = 0; y < size_y; y++)
            {
                var px = bounds.xMin + x; 
                var py = bounds.yMin + y;
                Vector3Int tilemap_pos = new Vector3Int(px, py, 0);
                Vector3 world_pos = GetCellCenterWorld(tilemap_pos);
                if (tilemap.HasTile(tilemap_pos))
                {
                    var tile = tilemap.GetTile(tilemap_pos);
                    //Blocking tile
                    if (tile.name == "wall") grid[x,y] = new GameCell(x, y, world_pos, tilemap_pos, false, true);
                    //Ground tile
                    else grid[x,y] = new GameCell(x, y, world_pos, tilemap_pos, true, false);
                }
                else
                {
                    //For cells that do not contain a tile,
                    //we set a -1 Z value 
                    tilemap_pos = new Vector3Int(px, py, -1);
                    grid[x,y] = new GameCell(x, y, world_pos, tilemap_pos, false, false);
                }
            }
        }

        //Center main camera with grid
        Camera camera = Camera.main.GetComponent<Camera>(); //Main camera
        Vector3Int centerCell = Vector3Int.FloorToInt(tilemap.cellBounds.center); //Center cell tilemap position (with float wtf)
        Vector3 centerCellWorld = GetCellCenterWorld(centerCell); //Center cell world positon
        centerCellWorld.z = camera.transform.position.z; //We want to keep the same z axis
        camera.transform.position = centerCellWorld; //Change main camera position

        //Graph from the grid
        graph = new GameCellGraph(this);
    }

    
    //Movement in the grid
    public Vector3Int _UP() {return new Vector3Int(1,0,0);}
    public Vector3Int _DOWN() {return new Vector3Int(-1,0,0);}
    public Vector3Int _LEFT() {return new Vector3Int(0,1,0);}
    public Vector3Int _RIGHT() {return new Vector3Int(0,-1,0);}


    //Place the object on the cell (ONLY for the logic, the obj sprite is not placed on the cell)
    public void PlaceObjectOn(GameCell cell, GameObject obj)
    {
        cell.occupying = obj.GetComponent<Character>(); //Null if not a character
    }


    //Remove the object of the cell (ONLY for the logic, the obj sprite is not deleted of the cell)
    public void RemoveObjectOf(GameCell cell)
    {
        cell.occupying = null;
    }


    //Return true if the cell is walkable in the grid
    public bool IsWalkable(GameCell cell)
    {
        return (cell.IsOccupied() == false && cell.walkable == true);
    }


    //Get the game cell of a game object (occupying the cell => character)
    //The object should be in the grid
    //Logical function
    public GameCell GetCellOf(GameObject obj)
    {
        var occupying = obj.GetComponent<Character>().uuid;
        for (int x = 0; x < size_x; x++)
        {
            for (int y = 0; y < size_y; y++)
            {
                if (grid[x, y].occupying != null && grid[x, y].occupying.uuid == occupying) return grid[x, y];
            }
        }
        return null; //Else
    }


    //Get the game cell from world position (null if not exists)
    //We use tilemap position as intermediate to get the cell that we stored in our array
    public GameCell WorldToCell(Vector3 world_pos)
    {
        //World position contains a Z value that we dont want
        world_pos.z = 0.0f;
        Vector3Int tilemap_pos = tilemap.WorldToCell(world_pos);
        if (tilemap_pos == null) return null;
        for (int x = 0; x < size_x; x++)
        {
            for (int y = 0; y < size_y; y++)
            {
                if (grid[x,y].tilemap_pos == tilemap_pos) return grid[x,y];
            }
        }
        return null;
    }


    //Return true if the tuple (x,y) is in the grid bounds
    public bool InBounds(int x, int y)
    {
        return (x >= 0 && x < size_x && y >=0 && y < size_y);
    }


    //Return true if the tuple (x,y) is in the grid bounds
    //and the cell is not un trou 
    public bool InWalkableBounds(int x, int y)
    {
        return (x >= 0 && x < size_x && y >=0 && y < size_y && grid[x,y].walkable);
    }


    //Return the position of the cell center in the world
    private Vector3 GetCellCenterWorld(Vector3Int pos)
    {
        Vector3 newPos = tilemap.CellToWorld(pos);
        newPos.y += (tilemap.cellSize.y/2);
        return newPos;
    }


    //Return the list ordered by nearest cell of the source cell
    public List<GameCell> OrderByNearest(List<GameCell> positions, GameCell source)
    {
        return positions.OrderBy(x => ManhattanDistance(x, source)).ToList();
    }


    //Return Manhattan Distance between a and b
    public int ManhattanDistance(GameCell a, GameCell b)
    {
        return Math.Abs(a.ind_x - b.ind_x) + Math.Abs(a.ind_y - b.ind_y);
    }


    //Return true if the view is blocked between a and b
    //Work almost well : some issues with the cell chosen in WorldToCell when the point is in a corner
    public bool IsViewBlocked(GameCell a, GameCell b)
    {
        //On trace une droite entre le centre de la case a et de la case b puis on verifie que tous les points
        //de cette droite (avec une precision de 0.01) ne sont pas sur une case bloquante
        //On autorise une intervalle de confiance si le points est suffisamment proche d'un corner
        for (float x = 0; x <= 1; x+=0.01f)
        {
            Vector3 nextPoint = Vector3.Lerp(a.world_pos, b.world_pos, x);
            GameCell cell = WorldToCell(nextPoint);
            if (cell != null)
            {
                float confidenceX = tilemap.cellSize.x / 100f;
                float confidenceY = tilemap.cellSize.y / 100f;
                Vector3 cornerA = tilemap.CellToWorld(cell.tilemap_pos); //down
                Vector3 cornerB = cornerA + new Vector3(0, tilemap.cellSize.y, 0); //up
                Vector3 cornerC = cornerA + new Vector3(tilemap.cellSize.x / 2f, tilemap.cellSize.y, 0); //right
                Vector3 cornerD = cornerA + new Vector3(-tilemap.cellSize.x / 2f, tilemap.cellSize.y, 0); //left
                if (!IsInConfidence(nextPoint, cornerA, confidenceY)
                    && !IsInConfidence(nextPoint, cornerB, confidenceY)
                    && !IsInConfidence(nextPoint, cornerC, confidenceX)
                    && !IsInConfidence(nextPoint, cornerD, confidenceX))
                {
                    //if the target cell contains a character, it is not blocking the view
                    if (cell == b && cell.occupying) return false;
                    if (cell != a && cell != b && (cell.blocking || cell.occupying)) return true;
                }
            }
        }
        return false;
    }
    //True if point A == Point B with confidence interval passed as argument
    private bool IsInConfidence(Vector3 pointA, Vector3 pointB, float confidence)
    {
        return Vector3.Distance(pointA, pointB) <= confidence;
    }


    //Normalized direction from a source to a target
    public Vector2Int GetDirTo(GameCell source, GameCell target)
    {
        if (source == null || target == null) return new Vector2Int(1, 1);
        int x = target.ind_x - source.ind_x; if (x > 0) x = 1; if (x < 0) x = -1;
        int y = target.ind_y - source.ind_y; if (y > 0) y = 1; if (y < 0) y = -1;
        return new Vector2Int(x, y);
    }


    //Reset the color of all the tiles in the grid
    public void ResetTilesColor()
    {
        for (int x = 0; x < size_x; x++)
        {
            for (int y = 0; y < size_y; y++)
            {
                SetTileColor(Color.white, grid[x,y].tilemap_pos);
            }
        }
    }


    //Used with coroutine to color cell for seconds
    public IEnumerator ColorCellFX(GameCell c, Color color)
    {
        SetTileColor(color, c.tilemap_pos); //Color
        yield return new WaitForSeconds(1f); //Wait
        SetTileColor(Color.white, c.tilemap_pos); //Reset
    }


    //Change color of the tile in the grid tilemap
    public void SetTileColor(Color color, Vector3Int position)
    {
        tilemap.SetTileFlags(position, TileFlags.None); //Flag the tile to be able to change color
        tilemap.SetColor(position, color); //Set the colour
    }
}