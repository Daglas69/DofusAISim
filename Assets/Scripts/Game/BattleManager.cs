using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;

public class BattleManager : MonoBehaviour
{
    public bool full_speed = false; //True to run the simulation VERY FAST
    public int loop_parties = 1; //Start a new party when the current one is over until it reachs 0
    public bool group_play_together = false; //false => characters playing one by one | true => every characters of the group play together
    public float play_time = 30.0f; //By round or character depending of the setting chosen
    public int grp_turn = 1; //Current turn : 1 or 2
    [HideInInspector] public int grp_char_id_turn = 0; //Current character playing in the group 
    public bool grp1_leader = false;
    public GameObject[] grp1_chars;
    public bool grp2_leader = false;
    public GameObject[] grp2_chars;
    public Transform[] grp1_starts;
    public Transform[] grp2_starts;
    private GridManager gridManager;
    private Timer timer;
    [HideInInspector] public bool is_paused = false;
    [HideInInspector] public bool is_in_game = false;
    [HideInInspector] public int nb_parties_ended = 0;
    [HideInInspector] public int nb_wins_grp1 = 0;
    [HideInInspector] public int nb_wins_grp2 = 0;
    //Save of some data
    //To start a new game
    private int save_grp_turn;
    [HideInInspector] public GameObject[] save_grp1_chars;
    [HideInInspector] public GameObject[] save_grp2_chars;
    private bool is_dungeon_mode = false; //When true, change to next scene when ended
    public GameObject[] simpleAI_prefabs; //Prefabs of all the simple AIs


    void Awake()
    {
        //Load variables from menu init instead of default values
        //If they have been init
        LoadVariables();

        //For dungeon mode, group 2 is adapted from group 1
        if (is_dungeon_mode) grp2_chars = ChooseEnemiesOf(grp1_chars);

        //We save start info of the game, if we want to start a new one later
        save_grp_turn = grp_turn;
        save_grp1_chars = new List<GameObject>(grp1_chars).ToArray();
        save_grp2_chars = new List<GameObject>(grp2_chars).ToArray();

        gridManager = FindObjectOfType<GridManager>();
        timer = new Timer(play_time);

        //Speed of the simulation (slow or fast)
        SpeedDisplay.Instance().SetSpeed(full_speed);

        //Init communications between chars
        Comm.Instance().SetBattleManager(this);

        NewGame();
    }


    //Start a new game
    //Useful for games with Qlearn training
    public void NewGame()
    {
        //Not the first game
        if (nb_parties_ended > 0)
        {
            RemoveCharacters(grp1_chars);
            RemoveCharacters(grp2_chars);
            grp_turn = save_grp_turn;
            grp1_chars = new List<GameObject>(save_grp1_chars).ToArray();
            grp2_chars = new List<GameObject>(save_grp2_chars).ToArray();
        }

        grp_char_id_turn = 0;
        int uuid = 0;

        //Place players of the group 1 to their start positions (random)
        Utils.ShuffleArray(grp1_starts); //Randomize value order
        for (int a = 0; a < grp1_chars.Length; a++)
        {
            string name = grp1_chars[a].name;
            grp1_chars[a] = Instantiate(grp1_chars[a]); //Initially contains a prefab that we must instantiate
            GameCell startCell = gridManager.WorldToCell(grp1_starts[a].position);
            gridManager.PlaceObjectOn(startCell, grp1_chars[a]);
            grp1_chars[a].transform.position = startCell.world_pos;
            grp1_chars[a].GetComponent<Character>().grp = 1;
            grp1_chars[a].GetComponent<Character>().grp_id = a;
            grp1_chars[a].GetComponent<Character>().uuid = uuid; uuid++;
            grp1_chars[a].name = name + " (grp1 c" + a + ")";
        }
        if (grp1_leader) GroupLeader.LeaderElection(grp1_chars);

        //Place players of the group 2 to their start positions (random)
        Utils.ShuffleArray(grp2_starts); //Randomize value order
        for (int b = 0; b < grp2_chars.Length; b++)
        {
            string name = grp2_chars[b].name;
            grp2_chars[b] = Instantiate(grp2_chars[b]); //Initially contains a prefab that we must instantiate
            GameCell startCell = gridManager.WorldToCell(grp2_starts[b].position);
            gridManager.PlaceObjectOn(startCell, grp2_chars[b]);
            grp2_chars[b].transform.position = startCell.world_pos;
            grp2_chars[b].GetComponent<Character>().grp = 2;
            grp2_chars[b].GetComponent<Character>().grp_id = b;
            grp2_chars[b].GetComponent<Character>().uuid = uuid; uuid++;
            grp2_chars[b].name = name + " (grp2 c" + b + ")";
        }
        if (grp2_leader) GroupLeader.LeaderElection(grp2_chars);

        //Not the first game
        if (nb_parties_ended > 0)
        {
            //To reset stats if Qlearn training is operating
            QlearnManager qlearnManager = FindObjectOfType<QlearnManager>();
            if (qlearnManager != null)
            {
                qlearnManager.NewGame(grp1_chars[0], grp2_chars[0]); //Reset
                qlearnManager.Train(nb_parties_ended); //Train the agents
            }

            //To reset UI 
            PanelManager uiManager = FindObjectOfType<PanelManager>();
            if (uiManager != null) uiManager.Init();
        }

        if (is_paused) Resume();

        GameLog.Log("[BATTLE] group " + grp_turn + " turn began");
        if (!group_play_together) GameLog.Log("[BATTLE] " + turn_char.name + " turn began");

        is_in_game = true;
        Time.timeScale = 1;
    }


    //Update not based on time
    //To handle user controls
    void Update()
    {
        //Pause & Resume battle
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Action_Pause();
        }

        //Change turn
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Action_ChangeTurn();
        }

        //[DEBUG] For player actions
        PlayerControls.PlayerActions(this);
    }


    //Update based on time
    void FixedUpdate()
    {
        //Only if game is running
        if (!is_in_game) return;

        //Check battle end
        CheckGameEnd();

        //If timer is over 
        //Or if all characters do not have any points left
        if (timer.IsReady || IsCurrentTurnOver())
        {
            NextTurn();
            timer.Reset();
        }
    }


    //Move the character following the path given
    public void Move(GameObject charObj, List<GameCell> path)
    {
        foreach (GameCell cell in path)
        {
            GameCell charCell = gridManager.GetCellOf(charObj);
            Vector3Int nextDir = new Vector3Int(cell.ind_x - charCell.ind_x, cell.ind_y - charCell.ind_y, 0);
            Move(charObj, nextDir);
        }
    }


    //Move the character to the direction passed as argument
    public void Move(GameObject charObj, Vector3Int dir)
    {
        Character character = charObj.GetComponent<Character>();
        if (character.pm.IsEnough(1))
        {
            GameCell charCell = gridManager.GetCellOf(charObj);
            if (charCell == null) return;
            int target_x = charCell.ind_x + dir.x;
            int target_y = charCell.ind_y + dir.y;
            if (!gridManager.InBounds(target_x, target_y)) return;
            GameCell targetCell = gridManager.grid[target_x, target_y];
            if (targetCell == null) return;
            if (gridManager.IsWalkable(targetCell))
            {
                gridManager.RemoveObjectOf(charCell);
                gridManager.PlaceObjectOn(targetCell, charObj);
                SpeedDisplay.Instance().DoDisplay(
                    () => StartCoroutine(character.WalkTo(targetCell.world_pos, dir)),
                    () => charObj.transform.position = targetCell.world_pos
                );
                character.pm.Remove(1);
                GameLog.Log("[BATTLE] " + charObj.name + " is moving to (" + targetCell.ind_x + "," + targetCell.ind_y + ")");
            }
        }
    }


    //Caster character throwing spell to game cell passed as argument
    public void ThrowSpell(GameObject casterObj, Spell spell, GameCell targetCell)
    {
        GameCell casterCell = gridManager.GetCellOf(casterObj);
        Character caster = casterObj.GetComponent<Character>();

        //If the spell has cooldown time and can not be thrown
        if (spell.HasActiveCooldown()) return;

        //Not in scope
        if (!GetInScopeCells(spell, casterCell).Contains(targetCell)) return;

        //Not enough PA
        if (!caster.pa.IsEnough(spell.pa)) return;

        //Remove PA
        caster.pa.Remove(spell.pa);

        //If we reach this, spell can be thrown
        GameLog.Log("[BATTLE] " + casterObj.name + " threw the spell " + spell.spellName);
        //Animation
        SpeedDisplay.Instance().DoDisplay(
            () => StartCoroutine(caster.ThrowSpellAnim(targetCell.tilemap_pos - casterCell.tilemap_pos))
        );

        //Spell effect
        //We check all cells around the spellCell (with an offset of scope_zone)
        List<GameCell> affectedCells = GetZoneEffectCells(targetCell, spell.scope_zone);
        foreach (GameCell affectedCell in affectedCells)
        {
            spell.ApplyOnCell(gridManager, affectedCell);
        }

        //Set cooldown
        spell.PutCooldown();
    }


    public List<GameCell> GetZoneEffectCells(GameCell cell, int scope_zone)
    {
        List<GameCell> cells = new List<GameCell>();
        for (int x = cell.ind_x - scope_zone; x <= cell.ind_x + scope_zone; x++)
        {
            for (int y = cell.ind_y - scope_zone; y <= cell.ind_y + scope_zone; y++)
            {
                //If the spell cell hits the cell 
                if (gridManager.InWalkableBounds(x, y)
                    && gridManager.ManhattanDistance(cell, gridManager.grid[x, y]) <= scope_zone
                    && !gridManager.IsViewBlocked(cell, gridManager.grid[x, y]))
                {
                    cells.Add(gridManager.grid[x, y]);
                }
            }
        }
        return cells;
    }


    //Return a list of cells where the spell can be thrown from source to hit destination
    public List<GameCell> GetInScopeOfTargetCells(Spell spell, GameCell source, GameCell destination)
    {
        var inScopeCells = new List<GameCell>();
        var cells = GetInScopeCells(spell, source); //All cells that the spell can be thrown from source

        //We check for each cell if it can hit the destination
        foreach (GameCell c in cells)
        {
            for (int x = c.ind_x - spell.scope_zone; x <= c.ind_x + spell.scope_zone; x++)
            {
                for (int y = c.ind_y - spell.scope_zone; y <= c.ind_y + spell.scope_zone; y++)
                {
                    if (x == destination.ind_x && y == destination.ind_y
                        && gridManager.ManhattanDistance(c, gridManager.grid[x, y]) <= spell.scope_zone
                        && !gridManager.IsViewBlocked(c, gridManager.grid[x, y]))
                        inScopeCells.Add(c);
                }
            }
        }
        return inScopeCells;
    }


    //Return a list of cells where the spell can be thrown from the source
    public List<GameCell> GetInScopeCells(Spell spell, GameCell source)
    {
        var cells = new List<GameCell>();

        //We check all cells around the source cell (with an offset of maxScope)
        for (int x = source.ind_x - spell.maxScope; x <= source.ind_x + spell.maxScope; x++)
        {
            for (int y = source.ind_y - spell.maxScope; y <= source.ind_y + spell.maxScope; y++)
            {
                if (gridManager.InWalkableBounds(x, y))
                {
                    if (spell.is_line_scope)
                    {
                        int distance_x = Math.Abs(source.ind_x - x);
                        int distance_y = Math.Abs(source.ind_y - y);

                        bool is_line_x = (source.ind_x == x && distance_y >= spell.minScope && distance_y <= spell.maxScope);
                        bool is_line_y = (source.ind_y == y && distance_x >= spell.minScope && distance_x <= spell.maxScope);
                        bool is_view_blocked = gridManager.IsViewBlocked(source, gridManager.grid[x, y]);

                        if ((is_line_x || is_line_y) && !is_view_blocked) cells.Add(gridManager.grid[x, y]);
                    }
                    else
                    {
                        int distance = gridManager.ManhattanDistance(source, gridManager.grid[x, y]);
                        if (distance >= spell.minScope && distance <= spell.maxScope
                            && !gridManager.IsViewBlocked(source, gridManager.grid[x, y]))
                            cells.Add(gridManager.grid[x, y]);
                    }
                }
            }
        }
        return cells;
    }


    //Return true if the spell can be thrown from source to hit destination
    //Code repetition but much more efficient than get all cells with GetInScopeOfTargetCells to check if list.Count > 0
    public bool IsInScopeOfTarget(Spell spell, GameCell source, GameCell destination)
    {
        //For the spell with scope zone <=> The spell can be thrown on cells near the destination
        //For the spell without scope zone <=> The spell can be thrown only on the destination cell

        //Cells around destination with offset of scope_zone (if 0, => check on destination cell only)
        for (int x = destination.ind_x - spell.scope_zone; x <= destination.ind_x + spell.scope_zone; x++)
        {
            for (int y = destination.ind_y - spell.scope_zone; y <= destination.ind_y + spell.scope_zone; y++)
            {
                //Check if it can hit destination from the cell
                //Only when scope zone > 0
                if (spell.scope_zone == 0 || (gridManager.InWalkableBounds(x, y)
                        && gridManager.ManhattanDistance(destination, gridManager.grid[x, y]) <= spell.scope_zone
                        && !gridManager.IsViewBlocked(destination, gridManager.grid[x, y])))
                {
                    GameCell targetCell = gridManager.grid[x, y];

                    //Check for line scopes
                    bool is_line_x = true;
                    bool is_line_y = true;
                    if (spell.is_line_scope)
                    {
                        is_line_x = source.ind_x == targetCell.ind_x;
                        is_line_y = source.ind_y == targetCell.ind_y;
                    }
                    if (is_line_x || is_line_y) //Always true when not in line cell
                    {
                        //Check distance and visibility
                        int distance = gridManager.ManhattanDistance(source, targetCell);
                        if (distance >= spell.minScope && distance <= spell.maxScope)
                        {
                            bool is_view_blocked = gridManager.IsViewBlocked(source, targetCell);
                            if (!is_view_blocked) return true;
                        }
                    }
                }
            }
        }
        //Else
        return false;
    }


    public void RemoveCharacter(GameObject charObj)
    {
        GameCell charCell = gridManager.GetCellOf(charObj);
        gridManager.RemoveObjectOf(charCell);
        //Can be optimized
        int grp_char = charObj.GetComponent<Character>().grp;
        if (grp_char == 1)
        {
            grp1_chars = Utils.RemoveFromArray(grp1_chars, charObj);
            //Update id in the group
            for (int x = 0; x < grp1_chars.Length; x++) grp1_chars[x].GetComponent<Character>().grp_id = x;
        }
        else if (grp_char == 2)
        {
            grp2_chars = Utils.RemoveFromArray(grp2_chars, charObj);
            //Update id in the group
            for (int x = 0; x < grp2_chars.Length; x++) grp2_chars[x].GetComponent<Character>().grp_id = x;
        }
        Destroy(charObj);
    }


    //Remove all the characters from an array
    public void RemoveCharacters(GameObject[] chars)
    {
        int size = chars.Length; //We save the size as it will be updated in the loop
        for (int i = 0; i < size; ++i)
        {
            GameObject charToRemove = chars[0]; //As the array is updated in the loop, we always take the first one
            GameCell charCell = gridManager.GetCellOf(charToRemove);
            gridManager.RemoveObjectOf(charCell);
            Destroy(charToRemove);
            chars = Utils.RemoveFromArray(chars, charToRemove);
        }
    }


    //Check if the current playing group/char has finished to play
    //Can be optimized
    public bool IsCurrentTurnOver()
    {
        if (group_play_together)
        {
            foreach (GameObject charObj in turn_grp_chars)
            {
                if (!charObj.GetComponent<Character>().HasFinishedToPlay()) return false;
            }
            return true;
        }
        else
        {
            return turn_char.GetComponent<Character>().HasFinishedToPlay();
        }
    }


    //True if the character passed as argument can play
    public bool CanPlay(GameObject charObj)
    {
        int grp = charObj.GetComponent<Character>().grp;
        int grp_id = charObj.GetComponent<Character>().grp_id;
        if (group_play_together) return grp == grp_turn;
        else return grp == grp_turn && grp_id == grp_char_id_turn;
    }


    //Change the current turn
    private void NextTurn()
    {
        if (group_play_together) ChangeGroupTurn();
        else ChangeCharTurn();
    }


    //Change the group turn
    private void ChangeGroupTurn()
    {
        gridManager.ResetTilesColor(); //TEMPORARY

        //End of the turn for the current group playing
        foreach (GameObject charObj in turn_grp_chars)
        {
            charObj.GetComponent<Character>().EndTurn();
        }
        GameLog.Log("[BATTLE] group " + grp_turn + " turn ended");

        grp_turn = grp_turn == 1 ? 2 : 1;
        
        //New turn for the new group playing
        foreach (GameObject charObj in turn_grp_chars)
        {
            charObj.GetComponent<Character>().UpdateTurn();
        }
        GameLog.Log("[BATTLE] group " + grp_turn + " turn began");
    }


    //Change the character turn
    private void ChangeCharTurn()
    {
        turn_char.GetComponent<Character>().EndTurn(); //End of the turn of the current character
        GameLog.Log("[BATTLE] " + turn_char.name + " turn ended");

        if (grp_char_id_turn + 1 >= turn_grp_chars.Length)
        {
            grp_char_id_turn = 0;
            GameLog.Log("[BATTLE] group " + grp_turn + " turn ended");
            gridManager.ResetTilesColor(); //TEMPORARY
            grp_turn = grp_turn == 1 ? 2 : 1;
            GameLog.Log("[BATTLE] group " + grp_turn + " turn began");
        }
        else grp_char_id_turn++;

        turn_char.GetComponent<Character>().UpdateTurn(); //New turn for the new character playing
        GameLog.Log("[BATTLE] " + turn_char.name + " turn began");
    }


    //End the battle
    //Winner is passed as argument
    private void End(int grp_winner)
    {
        nb_parties_ended++;
        if (grp_winner == 1) nb_wins_grp1++;
        else nb_wins_grp2++;
        is_in_game = false;
        GameLog.Log("[BATTLE] battle ended. Winner is group " + grp_winner);

        //Next dungeon in dungeon mode
        if (is_dungeon_mode)
        {
            //If group 1 win
            if (grp_winner == 1)
            {
                //Index of the dungeon
                int currDungeon = Int32.Parse(SceneManager.GetActiveScene().name.Split('-')[1]);
                //Number of dungeons
                int nbOfDungeons = InitVariables.numberOfDungeons;
                //If it is not the last scene
                if (currDungeon < nbOfDungeons)
                {
                    //Change to next scene
                    SceneManager.LoadScene("Donjon-" + (currDungeon + 1));
                }
                else Pause();
            }
            else Pause();
        }
        //New game in fight mode
        else if (nb_parties_ended < loop_parties)
        {
            NewGame();
        }

        //Stop game
        else
        {
            SaveGameResultsInFile();
            Pause();
        }
    }


    //True if the game is finished
    public bool IsGameFinished()
    {
        return (grp1_chars.Length == 0) || (grp2_chars.Length == 0);
    }


    //Check if the game is finished
    public void CheckGameEnd()
    {
        if (grp1_chars.Length == 0) End(2);
        else if (grp2_chars.Length == 0) End(1);
    }


    //Pause the battle
    public void Pause()
    {
        Time.timeScale = 0;
        is_paused = true;
        GameLog.Log("[BATTLE] battle paused");
    }


    //Resume the battle
    public void Resume()
    {
        Time.timeScale = 1;
        is_paused = false;
        GameLog.Log("[BATTLE] battle resumed");
    }


    //Copy from AI
    //Return all the cells where the character can move to according to his current PM
    //Each cell is associated to an int, representing the size of the path in cells
    //[OPTIONAL] Set a limit of PM to use
    public List<(GameCell,int)> GetAllCellsWhereCharCanMoveTo(Character character, int pmLimit = int.MaxValue)
    {
        int pm = character.pm.current;
        GameCell charCell = gridManager.GetCellOf(character.gameObject);
        List<(GameCell,int)> cells = new List<(GameCell,int)>();
        for (int x = charCell.ind_x - pm; x <= charCell.ind_x + pm; x++)
        {
            for (int y = charCell.ind_y - pm; y <= charCell.ind_y + pm; y++)
            {
                if (gridManager.InWalkableBounds(x, y))
                {
                    var path = CanCharGoTo(character, gridManager.grid[x, y]);
                    if (path != null && path.Count <= pmLimit) cells.Add((gridManager.grid[x, y],path.Count));
                }
            }
        }
        cells.Sort((c1, c2) => c1.Item2.CompareTo(c2.Item2)); //Sort based on the size of the path
        return cells;
    }


    //Copy from AI
    //Check if the character can go to a cell according with his current PM
    //Null if not possible
    public List<GameCell> CanCharGoTo(Character character, GameCell cell)
    {
        //First check destination cell to optimize
        if (!gridManager.IsWalkable(cell)) return null;
        List<GameCell> pathToTarget = character.dijkstra.GivePathTo(cell);
        if (pathToTarget == null) return null; //Can not go to
        if (pathToTarget.Count > character.pm.current) return null;
        else return pathToTarget;
    }


    public GameObject[] GetGrpChars(int i)
    {
        return (i == 1) ? grp1_chars : grp2_chars;
    }


    public GameObject[] GetEnemyGrpChars(int i)
    {
        return (i == 1) ? grp2_chars : grp1_chars;
    }


    public GameObject[] turn_grp_chars
    {
        get { return grp_turn == 1 ? grp1_chars : grp2_chars; }
        set { if (grp_turn == 1) grp1_chars = value; else grp2_chars = value; }
    }


    public GameObject turn_char { get { return grp_turn == 1 ? grp1_chars[grp_char_id_turn] : grp2_chars[grp_char_id_turn]; } }


    public GameObject[] AlliesOf(GameObject charObj, int grp)
    {
        var tmp = new List<GameObject>(grp == 1 ? grp1_chars : grp2_chars);
        tmp.Remove(charObj);
        return tmp.ToArray();
    }


    public GameObject[] EnemyGroup(int grp) 
    { 
        return grp == 1 ? grp2_chars : grp1_chars; 
    }


    public List<GameCell> EnemyGroupCells(int grp)
    {
        var groupList = new List<GameCell>();
        foreach (GameObject enemyObj in EnemyGroup(grp))
        {
            groupList.Add(gridManager.GetCellOf(enemyObj));
        }
        return groupList;
    }


    public List<GameCell> AllieGroupCells(GameObject charObj)
    {
        var groupList = new List<GameCell>();
        int grp = charObj.GetComponent<Character>().grp;
        GameObject[] group_chars = grp == 1 ? grp1_chars : grp2_chars;
        foreach (GameObject allieObj in group_chars)
        {
            if (allieObj != charObj)
                groupList.Add(gridManager.GetCellOf(allieObj));
        }
        return groupList;
    }


    public List<GameCell> GetCellsOf(GameObject[] charObjs)
    {
        var cells = new List<GameCell>();
        foreach (GameObject charObj in charObjs)
        {
            var cell = gridManager.GetCellOf(charObj);
            if (cell != null) cells.Add(cell);
        }
        return cells;
    }


    //Trigger leader election for a group
    public void NeedNewLeaderFor(int grp)
    {
        if (!is_in_game) return;

        //Invalidate current order
        foreach (GameObject x in GetGrpChars(grp).Where(x => x != null).ToList())
        { 
            var ai = x.GetComponent<AI>();
            if (ai != null) ai.SetOrder(null);
        }
        if (GetGrpChars(grp).Length >= 2) GroupLeader.LeaderElection(GetGrpChars(grp));
    }


    //Load vars from menu init
    private void LoadVariables()
    {
        //Init variables from menu
        if (InitVariables.is_init_from_menu)
        {
            full_speed = InitVariables.full_speed;
            loop_parties = InitVariables.loop_parties;
            group_play_together = InitVariables.group_play_together;
            play_time = InitVariables.play_time;
            grp1_leader = InitVariables.grp1_leader;
            grp1_chars = new List<GameObject>(InitVariables.grp1_chars).ToArray();
            grp2_leader = InitVariables.grp2_leader;
            grp2_chars = new List<GameObject>(InitVariables.grp2_chars).ToArray();
            is_dungeon_mode = InitVariables.is_dungeon_mode;
        }
        //Else use default values
    }


    //For dungeon mode
    //Choose enemies of the characters from group 1 passed as argument
    private GameObject[] ChooseEnemiesOf(GameObject[] characters)
    {
        //int nbOfDungeons = SceneManager.GetAllScenes().Where(x => x.name.Contains("Donjon")).Count();
        int currDungeon = Int32.Parse(SceneManager.GetActiveScene().name.Split('-')[1]);
        int nbOfEnemies = characters.Length + (currDungeon - 1);
        nbOfEnemies = (nbOfEnemies > InitVariables.maxGroupSize) ? InitVariables.maxGroupSize : nbOfEnemies;
        GameObject[] enemies = new GameObject[nbOfEnemies];
        for (int i = 0; i < nbOfEnemies; i++)
            enemies[i] = simpleAI_prefabs[UnityEngine.Random.Range(0, simpleAI_prefabs.Length)];
        return enemies;
    }


    //Save data in a file
    private static readonly string save_folder = "/Results/";
    private void SaveGameResultsInFile()
    {
        if (nb_parties_ended < 20) return; //Save useful results
        GameResult gr = new GameResult(this);
        string folderPath = Application.persistentDataPath + save_folder;
        string filePath = folderPath + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".txt";
        try
        {
            //Create directory if does not exist
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            File.Create(filePath).Dispose();
            //Write data serialized in file
            string json = JsonConvert.SerializeObject(gr, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        catch (IOException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }


    //Handle actions made by the user
    public void Action_Pause()
    {
        if (is_paused) Resume(); else Pause();
    }
    public void Action_ChangeTurn()
    {
        NextTurn();
        timer.Reset();
    }
    public void Action_ReturnMenu()
    {
        SceneManager.LoadScene(0);
    }





    /* ------------------------------------------------------------------ */
    /* ------------------------------------------------------------------ */

    //Index of the current spell for player actions
    //Used for debug
    int currentSpellInd = 0;

    //WARNING : This class is for DEBUG purpose and is not implemented in the sim
    static class PlayerControls
    {
        //Handle keyboard actions to control the current character playing
        public static void PlayerActions(BattleManager b)
        {
            if (b.grp1_chars.Length > b.grp_char_id_turn && b.CanPlay(b.grp1_chars[b.grp_char_id_turn]))
            {
                if (Input.GetKeyDown(KeyCode.D)) //Move right
                {
                    b.Move(b.grp1_chars[b.grp_char_id_turn], b.gridManager._RIGHT());
                }
                if (Input.GetKeyDown(KeyCode.Q)) //Move left
                {
                    b.Move(b.grp1_chars[b.grp_char_id_turn], b.gridManager._LEFT());
                }
                if (Input.GetKeyDown(KeyCode.S)) //Move down
                {
                    b.Move(b.grp1_chars[b.grp_char_id_turn], b.gridManager._DOWN());
                }
                if (Input.GetKeyDown(KeyCode.Z)) //Move up
                {
                    b.Move(b.grp1_chars[b.grp_char_id_turn], b.gridManager._UP());
                }
                if (Input.GetKeyDown(KeyCode.E)) //Change spell
                {
                    ChangeSpell(b);
                }
                if (Input.GetMouseButtonDown(0)) //Attack
                {
                    DoAttack(b);
                }
                if (Input.GetMouseButtonDown(1)) //Show where can throw spell
                {
                    ColorSpellThrowableTiles(b);
                }
                if (Input.GetMouseButtonUp(1))
                {
                    b.gridManager.ResetTilesColor();
                }
            }
        }

        //Color the cells where the spell can be thrown 
        private static void ColorSpellThrowableTiles(BattleManager b)
        {
            b.gridManager.ResetTilesColor();
            var charCell = b.gridManager.GetCellOf(b.grp1_chars[b.grp_char_id_turn]);
            var spell = b.grp1_chars[b.grp_char_id_turn].GetComponent<Character>().spells[b.currentSpellInd];
            foreach (GameCell cell in b.GetInScopeCells(spell, charCell))
            {
                b.gridManager.SetTileColor(Color.blue, cell.tilemap_pos);
            }
        }

        //Change the current chosen spell
        private static void ChangeSpell(BattleManager b)
        {
            var charSpells = b.grp1_chars[b.grp_char_id_turn].GetComponent<Character>().spells;
            if (b.currentSpellInd + 1 >= charSpells.Count) b.currentSpellInd = 0;
            else b.currentSpellInd++;
            GameLog.Log("[CHAR] Current spell of " + b.grp1_chars[b.grp_char_id_turn].name + " is set to " + charSpells[b.currentSpellInd].spellName);
        }

        //Throw a spell
        private static void DoAttack(BattleManager b)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            GameCell mouseCell = b.gridManager.WorldToCell(mouseWorldPos);
            b.ThrowSpell(b.grp1_chars[b.grp_char_id_turn], b.grp1_chars[b.grp_char_id_turn].GetComponent<Character>().spells[b.currentSpellInd], mouseCell);
        }
    }
}


//To store result of the game in a file
[Serializable]
class GameResult
{
    public List<string> grp1;
    public List<string> grp2;
    public int nb_wins_grp1;
    public int nb_wins_grp2;
    public int nb_parties;
    public bool leader_grp1;
    public bool leader_grp2;
    public string dungeon;

    public GameResult(BattleManager bm)
    {
        grp1 = new List<GameObject>(bm.save_grp1_chars).Select(x => x.name).ToList();
        grp2 = new List<GameObject>(bm.save_grp2_chars).Select(x => x.name).ToList();
        nb_wins_grp1 = bm.nb_wins_grp1;
        nb_wins_grp2 = bm.nb_wins_grp2;
        nb_parties = bm.nb_parties_ended;
        leader_grp1 = bm.grp1_leader;
        leader_grp2 = bm.grp2_leader;
        dungeon = SceneManager.GetActiveScene().name;
    }
}