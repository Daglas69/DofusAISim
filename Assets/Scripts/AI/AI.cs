using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class AI : MonoBehaviour
{
    protected BattleManager battleManager;
    protected GridManager gridManager;
    protected Character character;
    protected Order orderGiven;
    protected Delay executeDelay;
    protected bool canPlay = true;
    public float waitTime = 0.5f;
    //Useful general vars for execute function
    protected GameCell charCell; //Cell of the character
    protected GameObject[] enemies; //Enemies of the character
    protected GameObject[] allies; //Allies of the character
    protected List<GameCell> enemiesCells; //Cells of the enemies
    protected List<GameCell> alliesCells; //Cells of the allies
    private List<Action> beforeExecActions; //Action list before each execute call
    private List<Action> afterExecActions; //Action list after each execute call


    public void SetCanPlay(bool b) { canPlay = b; }


    public bool IsPlaying() { return canPlay; }


    public void Start()
    {
        battleManager = FindObjectOfType<BattleManager>();
        gridManager = FindObjectOfType<GridManager>();
        character = GetComponent<Character>();
        //Time between actions
        if (SpeedDisplay.Instance().IsFastDisplay()) executeDelay = new Delay(0.0f);
        else executeDelay = new Delay(UnityEngine.Random.Range(waitTime - 0.1f, waitTime + 0.1f));
        Init();
    }


    //For specific class init
    public virtual void Init() { }


    public void FixedUpdate()
    {
        //Only when it is the character turn and AI can still play
        if (!battleManager.CanPlay(gameObject) || !canPlay) return;

        //When character is not doing an action
        if (!character.IsAnimOn())
        {
            executeDelay.Update(Time.deltaTime);

            //Execute action every x seconds when character
            if (executeDelay.IsReady)
            {
                //action only if character still has points
                if (character.HasPoints())
                {
                    BeforeExecute(); //Action(s) before execute function
                    DoExecute();
                    AfterExecute(); //Action(s) after execute function
                }
                else canPlay = false;
                executeDelay.Reset(); //Reset action delay
            }
        }
    }


    //Actions made before the execute call
    //Can be used to init vars
    public virtual void BeforeExecute()
    {
        ResetVars();
        charCell = gridManager.GetCellOf(gameObject);
        allies = battleManager.AlliesOf(gameObject, character.grp);
        alliesCells = gridManager.OrderByNearest(battleManager.GetCellsOf(allies), charCell);
        if (CheckTargetOrderValidity() && orderGiven.cell == null) //If has move order, all enemies assigned
        {
            enemies = orderGiven.targets.ToArray(); 
        }
        else
        { 
            enemies = GetEnemies();
        }
        enemiesCells = gridManager.OrderByNearest(battleManager.GetCellsOf(enemies), charCell);

        //Actions executed before each execute call
        if (beforeExecActions == null) return;
        foreach (Action a in beforeExecActions)
        {
            a.Invoke();
        }
    }
    public void AddBeforeExecAction(Action a)
    { 
        if (beforeExecActions == null) beforeExecActions = new List<Action>(); 
        beforeExecActions.Add(a);
    }


    //Actions made after the execute call
    public virtual void AfterExecute()
    {
        //Actions executed after each execute call
        if (afterExecActions == null) return;
        foreach (Action a in afterExecActions)
        {
            a.Invoke();
        }
    }
    public void AddAfterExecAction(Action a)
    {
        if (afterExecActions == null) afterExecActions = new List<Action>();
        afterExecActions.Add(a);
    }


    //Clear variables of the AI
    public virtual void ResetVars()
    {
        charCell = null;
        enemies = null;
        allies = null;
        enemiesCells = null;
        alliesCells = null;
    }


    //Call execute function of the AI
    public void DoExecute()
    {
        //When AI has a move order
        //No need to call execute
        if (orderGiven != null && orderGiven.cell != null)
        {
            //Go to the next cell closer to the order cell
            //If not possible, execute will be called if there are enemies
            List<GameCell> path = ShortestPathTo(orderGiven.cell); //Get path to the cell
            if (path != null && character.pm.current > 0)
            {
                MoveTo(path[0]); //Move to next cell in path to target
                return;
            }
        }

        //When AI has no move order
        //Execute is called when there are enemies in the battle area
        if (enemiesCells != null && enemiesCells.Count > 0)
        {
            Execute();
        }
    }


    //Main function of the AI, running the decision tree
    //Specific to AI specialization 
    public abstract void Execute();


    //Called when starting the character turn
    //Can be used to reset/init specifics vars in child classes by overriding it
    public virtual void BeginTurn()
    {
        SetCanPlay(true);
    }


    //Add an order to the AI
    public void SetOrder(Order order)
    {
        orderGiven = order;
    }


    //Remove an order to the AI
    public void RemoveOrder()
    {
        orderGiven = null;
    }


    //Check validity of target order given
    public bool CheckTargetOrderValidity()
    {
        //No order
        if (orderGiven == null) return false;
        //No targets
        if (orderGiven.targets == null)
        {
            orderGiven = null;
            return false;
        }
        //Has not been updated
        if (orderGiven.targets[0] == null)
        {
            orderGiven = null;
            return false;
        }
        //Else OK
        return true;
    }


    //Return the shortest path (list of GameCell) between the character and a cell
    //Does not include source cell, neither destination cell if it is occupied
    //null if not possible
    protected List<GameCell> ShortestPathTo(GameCell targetCell)
    {
        return character.dijkstra.GivePathTo(targetCell);
    }


    //Check if the character can go to a cell according with his current PM
    //Null if not possible
    protected List<GameCell> CanGoTo(GameCell cell)
    {
        //First check destination cell to optimize
        if (!gridManager.IsWalkable(cell)) return null;
        List<GameCell> pathToTarget = ShortestPathTo(cell);
        if (pathToTarget == null) return null; //Can not go to
        if (pathToTarget.Count > character.pm.current) return null;
        else return pathToTarget;
    }


    //Move the character to the cell
    protected void MoveTo(GameCell cell)
    {
        GameCell charCell = gridManager.GetCellOf(gameObject); //Character cell
        if (gridManager.ManhattanDistance(charCell, cell) > 1) return; //Can not move
        Vector3Int nextDir = new Vector3Int(cell.ind_x - charCell.ind_x, cell.ind_y - charCell.ind_y, 0);
        battleManager.Move(gameObject, nextDir);
    }


    //Array of gameobjects containing enemies of the character
    protected GameObject[] GetEnemies()
    {
        return battleManager.EnemyGroup(character.grp);
    }


    //List of enemy gameobjects order by weaker from an array passed as argument
    //(only take in consideration life, not resistance)
    protected List<GameObject> OrderEnemiesByWeaker(GameObject[] enemies)
    {
        //if (enemies == null) return null;
        return new List<GameObject>(enemies).OrderBy(x => x.GetComponent<Character>().life.current).ToList(); 
    }


    //Fields of view of the enemies
    //from an array passed as argument
    protected GroupViews GetEnemiesViews(GameObject[] enemies)
    {
        //if (enemies == null) return null;
        var enemiesViews = new GroupViews();
        //For all the enemies, we check their field of view for each spell, and we add the cells where they can throw it
        //As multiples enemies may throw a spell in the same cell, we add them in a list 
        foreach (GameObject enemyObj in enemies)
        {
            foreach (Spell spell in enemyObj.GetComponent<Character>().spells)
            {
                //We check for damage and debuff spells
                if (spell as DamageSpell || spell as DebuffSpell)
                {
                    //Cells where the enemy can throw the spell
                    List<GameCell> inScopeCells = battleManager.GetInScopeCells(spell, gridManager.GetCellOf(enemyObj));
                    foreach (GameCell cell in inScopeCells)
                    {
                        //TO ADD : Scope damage ?
                        //If a view contains the cell, we check if the character has been added to the char array and we add it if not
                        View view = enemiesViews.GetViewOf(cell);
                        if (view != null)
                        {
                            if (!view.chars.Contains(enemyObj)) view.chars.Add(enemyObj);
                        }
                        else enemiesViews.views.Add(new View(cell, enemyObj));
                    }
                }
            }
        }
        return enemiesViews;
    }


    //List of enemies that are in scope and can kill the character
    //from an array passed as argument
    protected List<GameObject> GetEnemiesThatCanKillMe(GameObject[] enemies)
    {
        //if (enemies == null) return null;
        var enemyList = new List<GameObject>();
        foreach (GameObject enemyObj in enemies)
        {
            foreach (DamageSpell spell in enemyObj.GetComponent<Character>().GetAvailableSpells<DamageSpell>())
            {
                if (character.CanDie(spell.damage))
                {
                    GameCell source = gridManager.GetCellOf(enemyObj);
                    GameCell destination = gridManager.GetCellOf(gameObject);
                    if (battleManager.IsInScopeOfTarget(spell, source, destination))
                    {
                        enemyList.Add(enemyObj);
                    }
                }
            }
        }
        return enemyList;
    }

    
    //Return true if there is an enemy in scope that can kill the character
    //from an array passed as argument
    protected bool CanEnemyKillMe(GameObject[] enemies)
    {
        foreach (GameObject enemyObj in enemies)
        {
            foreach (DamageSpell spell in enemyObj.GetComponent<Character>().GetAvailableSpells<DamageSpell>())
            {
                if (character.CanDie(spell.damage))
                {
                    GameCell source = gridManager.GetCellOf(enemyObj);
                    GameCell destination = gridManager.GetCellOf(gameObject);
                    if (battleManager.IsInScopeOfTarget(spell, source, destination))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }


    //Get the targets in scope of a cell (including damage range)
    protected List<Target> GetTargetsInScope(List<Target> targets, GameCell cell)
    {
        //if (targets == null || cell == null) return null;
        var targetList = new List<Target>();
        foreach (Target target in targets)
        {
            Target t = new Target();
            t.targetObj = target.targetObj;
            //We keep spellCells in scope of cell
            foreach (Spell spell in target.spells)
            {
                GameCell targetCell = gridManager.GetCellOf(target.targetObj);
                if (battleManager.IsInScopeOfTarget(spell, cell, targetCell))
                {
                    t.spells.Add(spell);
                }
            }
            //If at least one spell can hit the target
            if (t.spells.Count > 0) targetList.Add(t);
        }
        return targetList;
    }


    //List of enemies that can be killed, associated to a list of spells
    //from an array of enemy gameobjects passed as argument and a list of damage spells
    //Watch out that the damage spells list contains only available spells
    protected List<Target> GetEnemiesKillable(GameObject[] enemies, List<DamageSpell> damageSpells)
    {
        //if (enemies == null || damageSpells == null) return null;
        var enemyList = new List<Target>();
        foreach (GameObject enemyObj in enemies)
        {
            Character enemyCharacter = enemyObj.GetComponent<Character>();
            Target targetKillable = new Target();
            targetKillable.targetObj = enemyObj;
            foreach (DamageSpell spell in damageSpells)
            {
                if (enemyCharacter.CanDie(spell.damage)) // = killable
                {
                    targetKillable.spells.Add(spell);
                }
            }
            if (targetKillable.spells.Count > 0) enemyList.Add(targetKillable);
        }
        return enemyList;
    }


    //List of enemies that can be hit, associated to a list of spells
    //from an array of enemy gameobjects passed as argument and a list of spells
    //In this case, the main goal is to associate ennemis to a list of spells, for easier data manipulation
    protected List<Target> GetEnemiesHittable<T>(GameObject[] enemies, List<T> spells) where T : Spell
    {
        return GetEnemiesHittable(enemies, spells.Cast<Spell>().ToList());
    }
    protected List<Target> GetEnemiesHittable(GameObject[] enemies, List<Spell> spells)
    {
        //if (enemies == null || spells == null) return null;
        var enemyList = new List<Target>();
        foreach (GameObject enemyObj in enemies)
        {
            Target target = new Target();
            target.targetObj = enemyObj;
            foreach (Spell spell in spells)
            {
                target.spells.Add(spell);
            }
            enemyList.Add(target);
        }
        return enemyList;
    }


    //Return the first cell where the character can move to be in scope of a target with a spell
    //Cells are ordered by closer to farther so the cell returned is the closest possible from char pos
    //[OPTIONAL] Set a limit of PM to use
    protected GameCell ChooseCellToMoveInScopeOfTarget(Target target, int pmLimit = int.MaxValue)
    {
        return ChooseCellToMoveInScopeOfTarget(new List<Target> { target }, pmLimit);
    }
    protected GameCell ChooseCellToMoveInScopeOfTarget(List<Target> targets, int pmLimit = int.MaxValue)
    {
        //Heuristic to optimize the function, we order targets by closer
        targets = targets.OrderBy(x => gridManager.ManhattanDistance(charCell, gridManager.GetCellOf(x.targetObj))).ToList();
        foreach (GameCell cell in GetAllCellsWhereCanMoveTo(pmLimit)) //Foreach cell where character can move to
        {
            foreach (Target target in targets) //Foreach targets
            {
                GameCell targetCell = gridManager.GetCellOf(target.targetObj);
                int manDist = gridManager.ManhattanDistance(targetCell, cell); //To make a heuristic (opti)
                foreach (Spell spell in target.spells) //Foreach spells that can hit a target
                {
                    //Heuristic to optimize the function
                    //We dont check spells that can not hit the target from the current checked cell
                    if (manDist <= spell.maxScope + spell.scope_zone && manDist >= spell.minScope)
                    {
                        //We return the first cell where the character can go to be in scope of one target 
                        if (battleManager.IsInScopeOfTarget(spell, cell, targetCell)) return cell;
                    }
                }
            }
        }
        return null;
    }


    // Choose the best cell to throw a spell
    // /!\ Debuff spell type must be considered as DamageSpell
    protected GameCell ChooseCellToThrowSpell<T>(List<GameCell> cells, Spell spell) where T : class
    {
        List<int> alliesHit = new List<int>();
        List<int> enemiesHit = new List<int>();
        foreach (GameCell c in cells)
        {
            int allies = 0;
            int enemies = 0;
            for (int x = c.ind_x - spell.scope_zone; x <= c.ind_x + spell.scope_zone; x++)
            {
                for (int y = c.ind_y - spell.scope_zone; y <= c.ind_y + spell.scope_zone; y++)
                {
                    if (gridManager.InBounds(x, y)
                        && gridManager.ManhattanDistance(c, gridManager.grid[x, y]) <= spell.scope_zone
                        && !gridManager.IsViewBlocked(c, gridManager.grid[x, y]))
                    {
                        if (gridManager.grid[x, y].occupying != null)
                        {
                            if (gridManager.grid[x, y].occupying.grp == character.grp) allies++;
                            else enemies++;
                        }
                    }
                }
            }
            alliesHit.Add(allies);
            enemiesHit.Add(enemies);
        }

        int ind = 0;
        for (int i = 1; i < cells.Count; ++i)
        {
            //Heal or buff spell
            if (typeof(T) == typeof(HealSpell) || typeof(T) == typeof(BuffSpell))
            {
                //We prefer no hits on enemies for heal and buff spells
                if (enemiesHit[ind] > 0 && enemiesHit[i] == 0) ind = i;
                //If it hits 0 enemy, we sort by allies hits
                else if (enemiesHit[i] == 0 && alliesHit[ind] < alliesHit[i]) ind = i;
            }

            //Debuff or damage spell
            else
            {
                //If ratio enemies - allies is better
                if (enemiesHit[ind] - alliesHit[ind] < enemiesHit[i] - alliesHit[i]) ind = i;

                //If ratio enemies - allies equals, we check the one hitting the most enemies
                else if (enemiesHit[ind] - alliesHit[ind] == enemiesHit[i] - alliesHit[i]
                    && enemiesHit[ind] < enemiesHit[i]) ind = i;
            }
        }
        if (cells.Count == 0 || ind >= cells.Count || ind < 0)
        {
            Debug.Log("XXX");
        }
        return cells[ind];
    }


    //Check the best cell to move on to be in scope of less enemies possible
    protected GameCell ChooseCellToMoveAway(GroupViews views)
    {
        GameCell charCell = gridManager.GetCellOf(gameObject);
        //First we get all the cells where the character can move on
        List<GameCell> cells = new List<GameCell>();
        int charPm = character.pm.current;
        for (int x = charCell.ind_x - charPm; x <= charCell.ind_x + charPm; ++x)
        {
            for (int y = charCell.ind_y - charPm; y <= charCell.ind_y + charPm; ++y)
            {
                if (gridManager.InWalkableBounds(x, y)) //In bounds and the cell is not un trou
                {
                    GameCell cell = gridManager.grid[x, y];
                    //No character on it, the character has enough PM to move on it
                    if (!cells.Contains(cell) && gridManager.IsWalkable(cell))
                    {
                        var path = CanGoTo(cell);
                        if (path != null && path.Count <= charPm) cells.Add(cell);
                    }
                }
            }
        }
        if (cells.Count == 0) return null;
        if (cells.Count == 1) return cells[0];
        //We compute for each cells the number of enemies that can see it
        List<int> nbEnemiesViewingCell = new List<int>(Enumerable.Repeat(0, cells.Count)); //same size than cells size, init with 0
        for (int i = 0; i < cells.Count; ++i)
        {
            View v = views.GetViewOf(cells[i]);
            if (v != null) nbEnemiesViewingCell[i] = v.chars.Count;
        }
        //We keep the cells with the min number of enemies that can see it
        List<GameCell> bestCells = new List<GameCell>();
        int nbMinEnemiesViewingCell = nbEnemiesViewingCell.Min();
        for (int i = 0; i < cells.Count; ++i)
        {
            if (nbMinEnemiesViewingCell == nbEnemiesViewingCell[i]) bestCells.Add(cells[i]);
        }
        //We compute the average distance from enemies for each best cells
        List<GameCell> enemiesCells = gridManager.OrderByNearest(battleManager.EnemyGroupCells(character.grp), charCell);
        List<float> avgDistanceEnemiesBestCells = new List<float>(Enumerable.Repeat(0f, bestCells.Count));
        for (int i = 0; i < bestCells.Count; ++i)
        {
            avgDistanceEnemiesBestCells[i] = enemiesCells.Aggregate(0, (acc, x) => acc += gridManager.ManhattanDistance(bestCells[i], x));
            avgDistanceEnemiesBestCells[i] = avgDistanceEnemiesBestCells[i] / enemiesCells.Count;
        }
        //We return the farthest cell (average) of enemies
        return bestCells[avgDistanceEnemiesBestCells.IndexOf(avgDistanceEnemiesBestCells.Max())];
    }


    //Return all the cells where the character can move to according to his current PM
    //Ordered by closer to character position
    //[OPTIONAL] Set a limit of PM to use
    protected List<GameCell> GetAllCellsWhereCanMoveTo(int pmLimit = int.MaxValue)
    {
        int pm = character.pm.current;
        GameCell charCell = gridManager.GetCellOf(gameObject);
        List<GameCell> cells = new List<GameCell>();
        for (int x = charCell.ind_x - pm; x <= charCell.ind_x + pm; x++)
        {
            for (int y = charCell.ind_y - pm; y <= charCell.ind_y + pm; y++)
            {
                if (gridManager.InWalkableBounds(x, y))
                {
                    var path = CanGoTo(gridManager.grid[x, y]);
                    if (path != null && path.Count <= pmLimit) cells.Add(gridManager.grid[x, y]);
                }
            }
        }
        return cells.OrderBy(x => gridManager.ManhattanDistance(x, charCell)).ToList();
    }




    /* ---------------------------------------------------------------------------------- */
    /* -------- Data structures to facilitate data manipulation in AI functions --------- */
    /* ---------------------------------------------------------------------------------- */
    // Might be tricky but it is only used to manipulate and associate data

    //To define the spells that can hit a target
    protected class Target {
        public GameObject targetObj = null;
        public List<Spell> spells = new List<Spell>();
    }

    //To define the views of a group of characters (1 or 2)
    protected class GroupViews
    {
        public List<View> views = new List<View>();
        public View GetViewOf(GameCell cell)
        {
            foreach(View v in views)
            {
                if (v.cell == cell) return v;
            }
            return null; //else
        }
    }

    //To define characters in view of a cell
    protected class View
    {
        public GameCell cell = null;
        public List<GameObject> chars = new List<GameObject>();
        public View(GameCell cell, GameObject charObj)
        {
            this.cell = cell;
            this.chars.Add(charObj);
        }
    }
}
