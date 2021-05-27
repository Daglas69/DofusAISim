using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DijkstraObject
{
    private GameCellGraphNode start;
    private readonly GridManager gridManager;
    Dictionary<int, int> distDict;
    Dictionary<int, int> fatherIndexDict;
    GameCellGraph graph;
    readonly GameObject perso;


    public DijkstraObject(GameCellGraphNode start, GridManager gridManager)
    {
        this.start = start;
        this.gridManager = gridManager;
        graph = gridManager.Graph();
        distDict = new Dictionary<int, int>();
        fatherIndexDict = new Dictionary<int, int>();
        AlgoDjikstra();
    }


    public DijkstraObject(GameCell startCell, GridManager gridManager, GameObject perso)
    {
        this.perso = perso;
        this.gridManager = gridManager;
        graph = gridManager.Graph();
        this.start = graph.GetNodes().Find(x => (x.Node().ind_x == startCell.ind_x) && ((x.Node().ind_y == startCell.ind_y)) );
        distDict = new Dictionary<int, int>();
        fatherIndexDict = new Dictionary<int, int>();
        AlgoDjikstra();
    }


    private Dictionary<int, GameCellGraphNode> computeNoeudATraiterDict(Dictionary<int, GameCellGraphNode> noeudsATraiterDict)
    {
        List<GameCellGraphNode> nodes = graph.GetNodes();
        foreach (GameCellGraphNode node in nodes)
        {
            if (node.index != start.index && (!node.Node().IsOccupied())) // On ajoute dans le tableau des noeuds à traiter seulement les noeuds non occupés
            {
                noeudsATraiterDict.Add(node.index, node);
            }
        }
        return noeudsATraiterDict;
    }


    private void Etape2(Dictionary<int, GameCellGraphNode> noeudsATraiterDict)
    {
        distDict.Add(start.index, 0);
        fatherIndexDict.Add(start.index, -1);
        foreach (GameCellGraphNode neighbor in start.GetNeighbor())
        {
            if (noeudsATraiterDict.ContainsKey(neighbor.index)) 
            {
                fatherIndexDict.Add(neighbor.index, start.index);
                distDict.Add(neighbor.index, 1);
            }
        }
    }


    private void AlgoDjikstra()
    {
        int maxInt = Int32.MaxValue;

        List<GameCellGraphNode> nodes = graph.GetNodes();
        Dictionary<int, GameCellGraphNode> noeudsATraiterDict = new Dictionary<int, GameCellGraphNode>();

        // Etape 1 et 1 bis initialisation de Dijkstra, on donne une distance infinie à chaque noeud, et on crée le tableau de noeuds à traiter
        noeudsATraiterDict = computeNoeudATraiterDict(noeudsATraiterDict);

        // Etape 2 et 2bis On examine les voisins de start, on ajoute leur distance au tableau (et on en profite pour initialiser les données de start       
        // on va garder fatherindex à -1 pour le noeud start
        Etape2(noeudsATraiterDict);
        // Etape 3 On examine le noeud n avec la plus petite distance, on renseigne la distance de ses voisins au tableau (en changeant ce qu'il y a dans le tableau uniquement si c'est plus petit que ce qui s'y trouve déja)
        // 3bis On enlève n du tableau de noeud à traiter

        //TEST POUR LA BRANCHE
        while (noeudsATraiterDict.Count != 0)
        {
            int min = maxInt;
            int ind_min = -1;
            bool didWeFindANodeToUse = false; //On cherche a arriver à un noeudsATraiterDict.Count == 0, mais si un noeud n'est pas atteignable (graphe disjoint) on y arrivera jamais, donc on doit vérifier que dans la boucle suivante on trouve bien un noeud présent dans distDict
            // ( qui contient les noeuds qu'on a déjà atteint depuis notre départ) et qui est dans les noeuds à traiter, si on a déjà traité tout les noeuds dans distDict et qu'il reste des noeuds à traiter, cela signifie que les noeuds restants dans noeudsATraiterDict ne sont pas atteignables,
            // il faut donc vérifier si target a été traité (existe dans distDict et fatherIndexDict), dans quel cas on renvoit le chemin trouvé, sinon on renvoit un tableau vide pour signifier que le noeud n'est pas atteignable
            foreach (KeyValuePair<int, int> kvp in distDict) // ON REGARDE SEULEMENT LES NOEUDS QUI ONT UNE DISTANCE, PAS LA PEINE D'EXAMINER LES AUTRES
            {
                if (noeudsATraiterDict.ContainsKey(kvp.Key)) // on vérifie que le noeud n'a pas déja été traité
                {
                    didWeFindANodeToUse = true;
                    if (kvp.Value < min)
                    {
                        min = kvp.Value;
                        ind_min = kvp.Key;
                    }
                }
            }

            if(didWeFindANodeToUse == false)  return;

            // On choisit le noeud qu'on traite et on l'enleve du dictionnaire
            GameCellGraphNode noeudChoisi = noeudsATraiterDict[ind_min];
            noeudsATraiterDict.Remove(ind_min);

            foreach (GameCellGraphNode neighbor in noeudChoisi.GetNeighbor())
            {
                if (!neighbor.Node().IsOccupied() ) // si le voisin est occupé on ne le considère même pas, à part si c'est la cible 
                {
                    if (distDict.ContainsKey(neighbor.index))
                    {
                        if (distDict[ind_min] + 1 < distDict[neighbor.index])
                        {
                            distDict[neighbor.index] = distDict[ind_min] + 1;
                            fatherIndexDict[neighbor.index] = ind_min;
                        }
                    }
                    else
                    {
                        distDict.Add(neighbor.index, distDict[ind_min] + 1);
                        fatherIndexDict.Add(neighbor.index, ind_min);
                    }
                }
            }
        }
    }


    // Convertit la cible en cible valide, puis déroule et renvoie le chemin vers la cible.
    // Par cible valide on désigne une case sur laquelle il est possible de s'arrêter, donc pas de case déjà occupée.
    private List<GameCellGraphNode> UnrollPathTo(GameCellGraphNode target)
    {
        List<GameCellGraphNode> output = new List<GameCellGraphNode>();
        GameCellGraphNode validTarget = GiveValidTarget(target);
        if(validTarget == null) return null;

        GameCellGraphNode currentNode = validTarget;

        if (!fatherIndexDict.ContainsKey(currentNode.index)) return null;
        
        while(currentNode.index != start.index)
        {
            output.Add(currentNode);
            int indexFatherOfCurrentNode = fatherIndexDict[currentNode.index];
            currentNode = graph.GetNodes().Find(x => x.index == indexFatherOfCurrentNode);
        }
        
        output.Reverse();
        return output;
    }


    // Renvoit une cible valide :
    // A savoir la target mise en paramètre ci celle-ci est libre, sinon son voisin le plus proche du start si la target en parametre est occupée.
    // Ne modifie pas le paramètre qu'on lui donne.
    private GameCellGraphNode GiveValidTarget(GameCellGraphNode target)
    {
        if (!target.Node().IsOccupied()) // Dans le cas où la case target donnée n'est pas occupée, on peut la renvoyer telle quelle
        {
            return target;
        }
        // Si toutefois elle est occupée on doit renvoyer le voisin le plus facilement atteignable
        int distanceMin = int.MaxValue;
        GameCellGraphNode validTarget = null;
        //on itère dans les voisins de la target pour voir lequel est le plus proche du start
        foreach(GameCellGraphNode neighbor in target.GetNeighbor())
        {
            int currentNeighborIndex = neighbor.index;
            if(distDict.ContainsKey(currentNeighborIndex) && distDict[currentNeighborIndex] < distanceMin)
            {
                distanceMin = distDict[currentNeighborIndex];
                validTarget = neighbor;
            }
        }
        return validTarget;
    }


    // Vérifie que la position du joueur est égale à une des cell du path donné en paramètres
    private bool IsPlayerOnPath(List<GameCellGraphNode> path)
    {
        int playerCellIndex = gridManager.Graph().GetNodeFromCell(gridManager.GetCellOf(perso)).index;
        if (playerCellIndex == start.index) return true; //le cas où le character est sur la case de départ
        foreach(GameCellGraphNode node in path)
        {
            if(node.index == playerCellIndex)
            {
                return true;
            }
        }
        return false;
    }


    // Liste toutes les cases situées avant la position courante du personnage dans le path puis les enlève du path
    private List<GameCellGraphNode> TrimPath(List<GameCellGraphNode> path)
    {
        int playerCellIndex = gridManager.Graph().GetNodeFromCell(gridManager.GetCellOf(perso)).index;
        List<GameCellGraphNode> nodesToRemove = new List<GameCellGraphNode>();
        if (playerCellIndex == start.index) return path;
        foreach (GameCellGraphNode node in path)
        {
            if(node.index != playerCellIndex)
            {
                nodesToRemove.Add(node);
            }
            else
            {
                nodesToRemove.Add(node);
                break;
            }
        }
        foreach(GameCellGraphNode node in nodesToRemove)
        {
            path.Remove(node);
        }
        return path;
    }


    public List<GameCellGraphNode> GivePathToInNodes(GameCellGraphNode target)
    {
        //PREMIERE ETAPE : on calcule un chemin avec le start de base, si le personnage est sur ce chemin et que le chemin est toujours valide
        // on renvoit ce chemin privé des cases déjà parcourue
        List<GameCellGraphNode> tmpPath = UnrollPathTo(target);
        if (tmpPath != null && IsPathStillValid(tmpPath))
        {
            if(IsPlayerOnPath(tmpPath))
            {
                return TrimPath(tmpPath);
            }
        }

        //DEUXIEME ETAPE : la première étape n'a pas marchée, donc on change le start par la position actuelle du personnage et on recalcule Djikstra
        graph = gridManager.Graph();
        start = gridManager.Graph().GetNodeFromCell(gridManager.GetCellOf(perso));// mettre le start sur la pos courante, faire tourner djikstra, dérouler le path, hop path niquel, petit couteau à la plonge, maitre d'hotel on envoit !
        distDict = new Dictionary<int, int>();
        fatherIndexDict = new Dictionary<int, int>();
        AlgoDjikstra();
        return UnrollPathTo(target);
    }


    // Donne le plus court chemin jusqu'à la case donnée en paramètre
    public List<GameCell> GivePathTo(GameCell target)
    {
        GameCellGraphNode targetNode = gridManager.Graph().GetNodeFromCell(target);
        return GameCellGraph.ConvertToGameCellList(GivePathToInNodes(targetNode));
    }


    // Vérifie que tout les noeuds du chemin sont toujours libres
    private bool IsPathStillValid(List<GameCellGraphNode> path)
    {
        foreach (GameCellGraphNode node in path)
        {
            if (node.Node().IsOccupied())
            {
                return false;
            }
        }
        return true;
    }
}
