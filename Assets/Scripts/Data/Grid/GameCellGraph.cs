using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class GameCellGraph
{
    private readonly List<GameCellGraphNode> nodes;
    private readonly int size;

    public GameCellGraph(GridManager gridManager)
    {
        nodes = new List<GameCellGraphNode>();
        int size_x = gridManager.GetSize_x();
        int size_y = gridManager.GetSize_y();
        GameCell[,] grid = gridManager.grid;
        //ajout des nodes
        int cpt = 0;
        for (int i = 0; i < size_x; i++)
        {
            for (int j = 0; j < size_y; j++)
            {
                if (grid[i, j].walkable)
                {
                    GameCellGraphNode tmpNode = new GameCellGraphNode(grid[i, j])
                    {
                        index = cpt
                    };
                    nodes.Add(tmpNode);
                    cpt++;
                }
            }
        }
        size = cpt + 1;
        //ajout des voisins
        ComputeNeighborhood(size_x, size_y);
    }


    public int GetSize() => size;


    public List<GameCellGraphNode> GetNodes() => nodes;


    public GameCellGraphNode GetNodeFromCell(GameCell c) => nodes.Find(n => n.Node() == c);


    //Retourne l'index de la cellule dans le graphe, -1 si la cellule n'est pas dans le graphe.
    private int IndexOfCellInGraph(int x, int y)
    {
        for (int i = 0; i < nodes.Count(); i++)
        {
            GameCellGraphNode n = nodes.ElementAt(i);
            if (n.Node().ind_x == x && n.Node().ind_y == y)
            {
                return i;
            }
        }
        return -1;
    }


    private bool IsCorrectIndex(GameCellGraphNode node, int x, int y)
    {
        if (node.Node().ind_x == x && node.Node().ind_y == y)
        {
            return true;
        }
        return false;
    }


    //Pour chaque noeud du graphe, regarde si les cases autours sont aussi dans le graphe, si c'est le cas les ajoute aux voisins
    public void ComputeNeighborhood(int sizeX, int sizeY)
    {
        foreach (GameCellGraphNode node in nodes)
        {
            GameCell cell = node.Node();

            //cellule a gauchent
            if (cell.ind_x > 0)
            {
                int index = IndexOfCellInGraph(cell.ind_x - 1, cell.ind_y);
                if (index != -1)
                {
                    node.AddNeighbor(nodes.ElementAt(index));
                }
            }

            //cellule a droitent
            if (cell.ind_x < sizeX - 1)
            {
                int index = IndexOfCellInGraph(cell.ind_x + 1, cell.ind_y);
                if (index != -1)
                {
                    node.AddNeighbor(nodes.ElementAt(index));
                }
            }

            //cellule en baent
            if (cell.ind_y > 0)
            {
                int index = IndexOfCellInGraph(cell.ind_x, cell.ind_y - 1);
                if (index != -1)
                {
                    node.AddNeighbor(nodes.ElementAt(index));
                }
            }

            //cellule en hauent
            if (cell.ind_y < sizeY - 1)
            {
                int index = IndexOfCellInGraph(cell.ind_x, cell.ind_y + 1);
                if (index != -1)
                {
                    node.AddNeighbor(nodes.ElementAt(index));
                }
            }
        }
    }


    public static void printNodeList( List<GameCellGraphNode> l)
    {
        foreach(GameCellGraphNode n in l)
        {
            int x = n.Node().ind_x;
            int y = n.Node().ind_y;
            Debug.Log("[NODE LIST] cell(" + x + ", " + y + ") ");
        }
    }


    public static List<GameCell> ConvertToGameCellList(List<GameCellGraphNode> l)
    {
        if (l == null) return null;
        List<GameCell> output = new List<GameCell>();
        foreach(GameCellGraphNode n in l)
        {
            output.Add(n.Node());
        }
        return output;
    }
}


public class GameCellGraphNode
{
    public int index;
    private GameCell node_GameCell;
    private List<GameCellGraphNode> neighbors;


    public GameCellGraphNode( GameCell c)
    {
        node_GameCell = c;
        index = -1;
        neighbors = new List<GameCellGraphNode>();
    }

    public GameCell Node()
    {
        return node_GameCell;
    }

    public GameCellGraphNode GetNeighbor(int i)
    {
        return neighbors[i];
    }

    public List<GameCellGraphNode> GetNeighbor()
    {
        return neighbors;
    }

    public void SetNode(GameCell n)
    {
        node_GameCell = n;
    }

    public void AddNeighbor(GameCellGraphNode n)
    {
        neighbors.Add(n);
    }

    public void RemoveNeighbor(GameCellGraphNode n)
    {
        if (neighbors.Contains(n))
        {
            neighbors.Remove(n);
        }
    }
}