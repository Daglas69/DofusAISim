using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//Manage the 1vs1 simulation with Qlearning AI
public class QlearnManager : MonoBehaviour
{
    [HideInInspector] public BattleManager battleManager;
    [HideInInspector] public GridManager gridManager;
    [HideInInspector] public GameObject character1;
    [HideInInspector] public GameObject character2;
    [HideInInspector] public Player player1;
    [HideInInspector] public Player player2;
    [HideInInspector] public bool is_p1_qlearn;
    [HideInInspector] public bool is_p2_qlearn;

    //Harcoded for the moment
    //Used to init dict of all possible states
    [HideInInspector] public int state_distanceMax;
    [HideInInspector] public int state_p1MaxLife;
    [HideInInspector] public int state_p2MaxLife;

    //Name of the files to store expectations for each sate
    //Saved in persistent datapath
    private static bool new_train = false; //Set to true to erase last save, else use values from last train
    private static readonly string save_folder = "/Qlearn/";
    private static readonly string player1_file = "player1_save.json";
    private static readonly string player2_file = "player2_save.json";


    //Init
    public void Awake()
    {
        battleManager = FindObjectOfType<BattleManager>();
        gridManager = FindObjectOfType<GridManager>();
        character1 = battleManager.GetGrpChars(1)[0];
        character2 = battleManager.GetGrpChars(2)[0];

        //Harcoded for the moment
        //Get max values of the states
        state_distanceMax = gridManager.ManhattanDistance(gridManager.grid[0, 0], gridManager.grid[gridManager.GetSize_x() - 1, gridManager.GetSize_y() - 1]);
        state_p1MaxLife = character1.GetComponent<Character>().life.max / 10;
        state_p2MaxLife = character2.GetComponent<Character>().life.max / 10;

        is_p1_qlearn = character1.GetComponent<QlearnAI>() != null;
        is_p2_qlearn = character2.GetComponent<QlearnAI>() != null;

        player1 = new Player(this, character1, character2);
        if (is_p1_qlearn) player1.v = GetStateExpectationsFromFile(player1_file);
        player2 = new Player(this, character2, character1);
        if (is_p2_qlearn) player2.v = GetStateExpectationsFromFile(player2_file);

        //Change nb of parties 
        battleManager.loop_parties = QlearnVariables.NUMBER_OF_TRAINING_GAMES;

        //Add actions to execute before and after each AI move if it is no Qlearning AI
        if (!is_p1_qlearn) AdaptToQlearn(character1);
        if (!is_p2_qlearn) AdaptToQlearn(character2);
    }


    //Reset player stats at the beginning of the game
    public void NewGame(GameObject char1, GameObject char2)
    {
        //P1
        character1 = char1;
        player1.agent = char1;
        player1.enemy = char2;
        player1.currentState = new State(state_distanceMax, state_p1MaxLife, state_p2MaxLife);
        if (!is_p1_qlearn) AdaptToQlearn(character1);

        //P2
        character2 = char2;
        player2.agent = char2;
        player2.enemy = char1;
        player2.currentState = new State(state_distanceMax, state_p2MaxLife, state_p1MaxLife);
        if (!is_p2_qlearn) AdaptToQlearn(character2);
    }


    //Train the agents 
    public void Train(int nb_parties)
    {
        if (nb_parties % QlearnVariables.TRAINING_RATIO == 0)
        {
            double coeff = QlearnVariables.COEFF_EPSILON;
            double epsilonMin = QlearnVariables.EPSILON_MIN;
            if (is_p1_qlearn && player1.trainable)
            {
                player1.epsilon = Math.Max(player1.epsilon * coeff, epsilonMin);
                SaveStateExpectationsInFile(player1.v, player1_file);
            }
            if (is_p2_qlearn && player2.trainable)
            {
                player2.epsilon = Math.Max(player2.epsilon * coeff, epsilonMin);
                SaveStateExpectationsInFile(player2.v, player2_file);
            }
        }

        if (is_p1_qlearn) player1.Train();
        if (is_p2_qlearn) player2.Train();
    }


    //Save the dict containing expectations for each state in a file
    public void SaveStateExpectationsInFile(Dictionary<int, double> v, string filename)
    {
        string folderPath = Application.persistentDataPath + save_folder;
        string filePath = folderPath + filename;
        try
        {
            //Create directory if does not exist
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            //Create file if does not exist
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }
            //Write data serialized in file
            string json = JsonConvert.SerializeObject(v, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        catch (IOException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }


    //Get the dict containing expectations for each state from a file
    public Dictionary<int, double> GetStateExpectationsFromFile(string filename)
    {
        string folderPath = Application.persistentDataPath + save_folder;
        string filePath = folderPath + filename;
        try
        {
            //Create directory if does not exist
            //<=> File does not exist too
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            //Create file if does not exist
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
                return State.CreateAllStatesDict(this);
            }
            //To dont use data from last train
            if (new_train) return State.CreateAllStatesDict(this);
            //Read data from file
            string fileContent = File.ReadAllText(filePath);
            Dictionary<int, double> v = JsonConvert.DeserializeObject<Dictionary<int, double>>(fileContent);
            if (v == null) return State.CreateAllStatesDict(this);
            else return v;
        }
        catch (IOException ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }


    //Add actions to execute before and after each move of the character AI
    //[WARNING] The AI must not be Qlearn AI
    //Used to adapt the Qlearn algo to the game
    private void AdaptToQlearn(GameObject character)
    {
        //On est jamais trop prudents
        if (character.GetComponent<QlearnAI>() != null) return;
        AI ai = character.GetComponent<AI>();
        if (ai == null) return;

        //Action made before play
        Action beforePlay = () =>
        {
            var manager = GetComponent<QlearnManager>();
            var player = manager.GetPlayer(character);
            var enemy = manager.GetEnemy(character);
            player.previousState = player.currentState;
            enemy.previousState = enemy.currentState;
        };
        ai.AddBeforeExecAction(beforePlay);

        //Action made after play
        Action afterPlay = () =>
        {
            var manager = GetComponent<QlearnManager>();
            var player = manager.GetPlayer(character);
            var enemy = manager.GetEnemy(character);
            player.currentState = manager.GetState(player);
            enemy.currentState = player.currentState.Reverse();
            int reward = 0; //Reward to 0 except for win
            int action = -1; //Unknown action
            if (battleManager.IsGameFinished())
            {
                reward = Rewards.win;
                player.win_nb++;
                enemy.lose_nb++;
            }
            // on ajoute les transitions aux deux historiques
            Transition trans2 = new Transition(enemy.previousState, action, -reward, enemy.currentState);
            enemy.AddTransition(trans2);
            Transition trans = new Transition(player.previousState, action, reward, player.currentState);
            player.AddTransition(trans);
            GameLog.Log("[QLEARN] " + player.agent.name + " state " + player.currentState.distance + " " + player.currentState.agentLife + " " + player.currentState.enemyLife);
        };
        ai.AddAfterExecAction(afterPlay);
    }


    //Get current state of a player
    public State GetState(Player p)
    {
        //Agent dead
        if (p.agent.GetComponent<Character>().life.current <= 0)
        {
            int distance = p.previousState.distance;
            int enemyLife = p.enemy.GetComponent<Character>().life.current / 10;
            return new State(distance, -1, enemyLife);
        }

        //Enemy dead
        if (p.enemy.GetComponent<Character>().life.current <= 0)
        {
            int distance = p.previousState.distance;
            int agentLife = p.agent.GetComponent<Character>().life.current / 10;
            return new State(distance, agentLife, - 1);
        }

        //Else
        else
        {
            int distance = gridManager.ManhattanDistance(
                gridManager.GetCellOf(p.agent),
                gridManager.GetCellOf(p.enemy)
            );
            int agentLife = p.agent.GetComponent<Character>().life.current;
            agentLife = agentLife <= 0 ? -1 : agentLife / 10;
            int enemyLife = p.enemy.GetComponent<Character>().life.current;
            enemyLife = enemyLife <= 0 ? -1 : enemyLife / 10;
            return new State(distance, agentLife, enemyLife);
        }
    }


    public Player GetPlayer(GameObject character)
    {
        if (player1.agent == character) return player1;
        if (player2.agent == character) return player2;
        return null;
    }


    public Player GetEnemy(GameObject character)
    {
        if (player1.agent == character) return player2;
        if (player2.agent == character) return player1;
        return null;
    }
}

