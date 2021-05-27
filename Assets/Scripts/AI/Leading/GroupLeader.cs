using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


/* https://lucid.app/lucidchart/invitations/accept/inv_4e982235-3697-4164-afb0-f41f5362aef8?viewport_loc=-849%2C-55%2C3335%2C1551%2C0_0 */

public class GroupLeader : MonoBehaviour
{
    private BattleManager battleManager;
    private GridManager gridManager;
    private CharacterComm comm;
    private int grp_id;
    private List<GameObject> group;
    private List<GameObject> allies;
    private List<GameObject> enemies;
    private bool is_healer;


    //Init
    void Start()
    {
        battleManager = FindObjectOfType<BattleManager>();
        gridManager = FindObjectOfType<GridManager>();
        comm = GetComponent<CharacterComm>();
        grp_id = GetComponent<Character>().grp;
    }


    //Leader is in charge of giving orders
    //New order is given at each update
    void FixedUpdate()
    {
        UpdateGroups();
        Dispatch();
    }


    //Update arrays to compute orders
    private void UpdateGroups()
    {
        group = battleManager.GetGrpChars(grp_id).ToList();
        allies = battleManager.AlliesOf(gameObject, grp_id).ToList();
        enemies = battleManager.GetEnemyGrpChars(grp_id).ToList();
    }


    //Compute orders
    private void Dispatch()
    {
        //Not enough chars to dispatch
        if (group.Count < 2)
        {
            SendOrder(group[0], null);
            Destroy(this);
        }

        //Order for low hp allies
        TreeLowHpAllies();

        //If leader is healer, he will not be candidate for next orders 
        if (is_healer) group.Remove(gameObject); 

        //Order to handle low hp enemies
        TreeLowHpEnemies();

        //Order to handle other enemies
        TreeEnemies();
    }


    //Decision tree for low hp allies
    private void TreeLowHpAllies()
    {
        //All the group already has an order
        if (group.Count == 0) return;
        //All the enemies are already assigned
        if (enemies.Count == 0) return;

        var lowHpAllies = allies.Where(x => x.GetComponent<Character>().life.current <= 0.2 * x.GetComponent<Character>().life.max).ToList();
        List<GameObject> healers = group.Where(x => x.GetComponent<Character>().spells.Where(y => (y as HealSpell) != null).Count() > 0).ToList();
        //If there are healers in the group
        if (healers.Count() > 0)
        {
            //Check for each low hp allie
            foreach (GameObject lowHpAlly in lowHpAllies)
            {
                //Check if ally healer
                var isAllyHealer = lowHpAlly.GetComponent<Character>().spells.Where(y => (y as HealSpell) != null).Count() > 0;
                
                //Check if in scope of healer
                var isInScopeOfHealers = false;
                foreach (GameObject healer in healers)
                {
                    int minScope = healer.GetComponent<Character>().Spells<HealSpell>().Where(x => x.maxScope > 0).Min(x => x.maxScope);
                    if (gridManager.ManhattanDistance(gridManager.GetCellOf(lowHpAlly), gridManager.GetCellOf(healer)) < minScope)
                    {
                        isInScopeOfHealers = true;
                        break;
                    }
                }

                //If allie is not an healer and not in scope of healers
                if (!isAllyHealer && !isInScopeOfHealers)
                {
                    GameObject closestHealer = healers.OrderBy(x => gridManager.ManhattanDistance(gridManager.GetCellOf(lowHpAlly), gridManager.GetCellOf(x))).First();
                    Order order = new Order();
                    order.cell = gridManager.GetCellOf(closestHealer);
                    SendOrder(lowHpAlly, order);
                    group.Remove(lowHpAlly); //Has an order
                }
            }
        }
    }
    

    //Decision tree for low hp enemies
    private void TreeLowHpEnemies()
    {
        //All the enemies are already assigned
        if (enemies.Count == 0) return;

        var lowHpEnemies = new List<GameObject>(enemies).Where(x => x.GetComponent<Character>().life.current <= 0.2 * x.GetComponent<Character>().life.max).ToList();
        foreach (GameObject enemy in lowHpEnemies)
        {
            //All the group already has an order
            if (group.Count == 0) return;

            GameObject nearestAlly = group.OrderBy(x => gridManager.ManhattanDistance(gridManager.GetCellOf(x), gridManager.GetCellOf(enemy))).First();
            Order order = new Order();
            order.targets = new GameObject[1] { enemy };
            SendOrder(nearestAlly, order);
            group.Remove(nearestAlly); //Has an order
            enemies.Remove(enemy);
        }
    }


    //Decision tree for other enemies
    private void TreeEnemies()
    {
        //All the group already has an order
        if (group.Count == 0) return;
        //All the enemies are already assigned
        if (enemies.Count == 0) return;

        List<DistanceToEnemy> distancesToEnemies = ComputeDistancesToEnemies();

        List<GameObject> enemiesByClosest = OrderEnemiesByClosest(enemies);

        //Compute how many allies for one enemy
        int nbEnemies = enemiesByClosest.Count;
        int nbGroup = group.Count;
        int nbAlliePerEnemy = nbGroup / nbEnemies;
        nbAlliePerEnemy = nbAlliePerEnemy < 2 ? 2 : nbAlliePerEnemy; //2 is min when there is enough allies
        if (nbGroup < nbAlliePerEnemy) nbAlliePerEnemy = 1; //if there is not enough allies

        //Assign an enemy to X allies
        for (int i = 0; i < nbGroup / nbAlliePerEnemy; ++i)
        {
            GameObject closestEnemy = enemiesByClosest.First();
            for (int j = 0; j < nbAlliePerEnemy; ++j)
            {
                //We find the closest allie of the chosen enemy
                DistanceToEnemy dte = distancesToEnemies.Find(x => x.enemyObj == closestEnemy);
                GameObject closestAllie = dte.charObj;
                Order order = new Order();
                order.targets = new GameObject[1] { closestEnemy };
                SendOrder(closestAllie, order);
                group.Remove(closestAllie);
                distancesToEnemies.Remove(dte);
            }
            enemiesByClosest.Remove(closestEnemy);
            enemies.Remove(closestEnemy);
        }

        //Odd number in group
        if (group.Count > 0)
        {
            foreach (GameObject allie in group.ToArray()) //Copy of group
            {
                DistanceToEnemy dte = distancesToEnemies.Find(x => x.charObj = allie);
                GameObject closestEnemy = dte.enemyObj;
                Order order = new Order();
                order.targets = new GameObject[1] { closestEnemy };
                SendOrder(allie, order);
                group.Remove(allie);
            }
        }
    }


    //Order the enemies by priority
    private List<GameObject> OrderEnemiesByPriority(List<GameObject> enemyList)
    {
        //We order enemies by priority type : healer, dps and tank
        //Then we order by life
        var healers = enemyList.Where(x => x.GetComponent<Character>().spells.Any(y => (y as HealSpell) != null)).ToList();
        healers = healers.OrderBy(x => x.GetComponent<Character>().life.current).ToList();
        enemyList.RemoveAll(x => healers.Contains(x));
        var dps = enemyList.Where(x => x.GetComponent<Character>().spells.Where(z => z as DamageSpell != null).Any(y => (y as DamageSpell).damage > 100)).ToList();
        dps = dps.OrderBy(x => x.GetComponent<Character>().life.current).ToList();
        enemyList.RemoveAll(x => dps.Contains(x));
        var tanks = enemyList;
        tanks = tanks.OrderBy(x => x.GetComponent<Character>().life.current).ToList();

        return healers.Union(dps).Union(tanks).ToList();
    }


    //Order enemies by closest
    private List<GameObject> OrderEnemiesByClosest(List<GameObject> enemyList)
    {
        //We sum the distance to each allies to order enemies
        return enemyList.OrderBy(x =>
        {
            int sum = 0;
            foreach (GameObject allie in group) sum += gridManager.ManhattanDistance(gridManager.GetCellOf(allie), gridManager.GetCellOf(x));
            return sum;
        }).ToList();
    }


    //Distances between chars of the array group and the array enemies
    private List<DistanceToEnemy> ComputeDistancesToEnemies()
    {
        var distancesToEnemies = new List<DistanceToEnemy>();
        foreach (GameObject charObj in group)
        {
            GameCell charCell = gridManager.GetCellOf(charObj);
            foreach (GameObject enemyObj in enemies)
            {
                DistanceToEnemy dte = new DistanceToEnemy();
                dte.charObj = charObj;
                dte.enemyObj = enemyObj;
                dte.distance = gridManager.ManhattanDistance(charCell, gridManager.GetCellOf(enemyObj));
                distancesToEnemies.Add(dte);
            }
        }
        //Sort on closer
        return distancesToEnemies.OrderBy(x => x.distance).ToList();
    }


    //Send orders to an allie
    private void SendOrder(GameObject allie, Order order)
    {
        int uuid = allie.GetComponent<Character>().UUID();
        comm.SendMessage(Msg<Order>.OrderMsg(order), uuid);
    }


    //Handle destroy action on the component to call LeaderElection
    void OnDestroy()
    {
        //The function is called when quitting the editor and display an error
        //Might get error on dev build but not important
        if (Time.frameCount == 0) return;
        battleManager.NeedNewLeaderFor(grp_id);
    }


    //Static method to choose leader in a group
    public static void LeaderElection(GameObject[] grp_chars)
    {
        //Not enough chars to elect a leader
        if (grp_chars.Length < 2) return;

        int grp = grp_chars[0].GetComponent<Character>().grp;

        GameObject chosenLeader = null;

        //We take healers as candidates
        var candidates = new List<GameObject>(grp_chars).Where(x => x.GetComponent<Character>().spells.Where(y => (y as HealSpell) != null).Count() > 0).ToList();
        bool is_healer = true;

        //If no healer alive, we take all characters as candidates
        if (candidates.Count == 0)
        {
            candidates = new List<GameObject>(grp_chars);
            is_healer = false;
        }

        //We choose the candidate with max life
        chosenLeader = candidates.OrderByDescending(x => x.GetComponent<Character>().life.current).First();

        //We add leader component to new leader
        chosenLeader.AddComponent<GroupLeader>();
        chosenLeader.GetComponent<GroupLeader>().is_healer = is_healer;
        GameLog.Log("[LEADER] Group " + grp + " : " + chosenLeader.name + " is the new leader !");
    }


    //Inner class to store data 
    //distances between a character and enemies 
    private class DistanceToEnemy
    {
        public GameObject charObj = null;
        public GameObject enemyObj = null;
        public int distance;
    }
}