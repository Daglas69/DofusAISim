using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Character : MonoBehaviour
{
    [HideInInspector] public Points life; //Life points
    [HideInInspector] public Points pa; //Attack points per turn
    [HideInInspector] public Points pm; //Movement points per turn
    [HideInInspector] public float damage_resistance; //between 0 and 1 (1 = 100%)
    [HideInInspector] public int grp; //Group in the battle
    [HideInInspector] public int grp_id; //ID in the grp 
    [HideInInspector] public int uuid; //ID in the game
    [HideInInspector] public List<Spell> spells; //Spells of the character
    [HideInInspector] public List<Buff> buffs; //Active buffs and debuffs of the character
    [HideInInspector] private bool is_anim_on = false; //True when the character is doing an animation
    [HideInInspector] private readonly float walkTime = 0.4f; //Time to walk to a cell
    [HideInInspector] public DijkstraObject dijkstra; //Optimized dijkstra algo for each character (based on current position)


    public void Start()
    {
        GridManager gridManager = FindObjectOfType<GridManager>();
        dijkstra = new DijkstraObject(gridManager.GetCellOf(gameObject), gridManager, gameObject);
    }


    //Return spells of a type
    public List<TypeSpell> Spells<TypeSpell>() where TypeSpell : class
    {
        if (spells != null)
        {
            return spells.Where(x => (x as TypeSpell) != null).Select(x => x as TypeSpell).ToList();
        }
        else return new List<TypeSpell>();
    }


    //Return spells available from a list (enough PA, no active cooldown)
    public List<TypeSpell> GetAvailableSpells<TypeSpell>() where TypeSpell : class
    {
        return spells
            .Where(x => (x as TypeSpell) != null) //We keep TypeSpell spells
            .Where(x => !x.HasActiveCooldown()) //We remove spells with active cooldown 
            .Where(x => pa.current - x.pa >= 0) //We remove spells with too much PA 
            .Select(x => x as TypeSpell) //Convert Spell To TypeSpell
            .ToList();
    }


    //Get UUID of the character (ID in the game)
    public int UUID()
    {
        return uuid;
    }


    //Verdad si esta muerto
    public bool IsMuerto()
    {
        return life.current <= 0;
    }


    //Must be called at the begenning of the character turn
    public void UpdateTurn()
    {
        //Update the buffs durations
        foreach (Buff b in buffs.ToArray()) //Copy of the list to be able to remove items while iterating
        {
            b.UpdateTurn();
            if (b.IsDurationOver())
            {
                b.RemoveOn(this);
                buffs.Remove(b);
            }
        };

        //Update spells cooldown
        foreach (Spell spell in spells)
        {
            spell.UpdateCooldown();
        }

        //If the character is controlled by an AI, we make it able to play
        AI ai = GetComponent<AI>();
        if (ai != null) ai.BeginTurn();
    }


    //Must be called at the end of the character turn
    public void EndTurn()
    {
        //Reset character points (movement & attack)
        ResetPoints();
    }


    //Reset character points (movement & attack)
    public void ResetPoints()
    {
        pa.Reset();
        pm.Reset();
    }


    //True if character has current points for the round
    public bool HasPoints()
    {
        return (pa.current > 0 || pm.current > 0);
    }


    //Use this function to include damage resistance
    public void RemoveLife(int x)
    {
        life.Remove((int)(x - x*damage_resistance));
    }


    //Return true if the character can die by inflicting x damages
    //Used to test damage on a character with damage resistance
    public bool CanDie(int x)
    {
        return (life.current - (int)(x - x*damage_resistance) <= 0);
    }

    //Add a buff to a character
    public void AddBuff(Buff buff)
    {
        buffs.Add(buff);

        //Apply buff to character
        buff.ApplyOn(this);
    }


    //Return true if the character is currently buffed
    //Only check "positive" buffs (no debuffs)
    public bool IsBuffed()
    {
        if (buffs.Count == 0) return false;
        foreach (Buff b in buffs)
        {
            if (b.type == BuffType.BUFF) return true;
        }
        return false;
    }


    //Return true if the character is low hp
    //Low hp is < 25% of his max life
    public bool IsLowHP()
    {
        return (float)life.current <= (float)life.max/4f;
    }


    //True if the character is doing an animation
    public bool IsAnimOn()
    {
        return is_anim_on;
    }


    //Return true if character has no more points for the turn
    //TODO : Find a more elegant way to determine it
    public bool HasFinishedToPlay()
    {
        AI ai = GetComponent<AI>();
        if (ai == null) return pa.current == 0 && pm.current == 0; //Does not have AI
        else return !ai.IsPlaying(); //AI indicates when it can no more play
    }


    public void Update()
    {
        //Sprite sorting order (according to grid position)
        //Should be modified...
        GameCell c = FindObjectOfType<GridManager>().WorldToCell(transform.position);
        GetComponent<Renderer>().sortingOrder = - (c.ind_x + c.ind_y); //Lower is the order, later it will be rendered 
    }


    //Moving from a pos with a direction
    public IEnumerator WalkTo(Vector3 pos, Vector3Int dir)
    {
        is_anim_on = true;
        float elapsedTime = 0;
        Vector3 startingPos = transform.position;
        Animator anim = GetComponent<Animator>();

        string walk_str = "walk_";
        string dir_str = null;
        if (anim != null)
        {
            dir_str = GetStrFromDir(dir);
            if (dir_str != null) anim.Play(walk_str + dir_str);
        }
        
        while (elapsedTime < walkTime)
        {
            transform.position = Vector3.Lerp(startingPos, pos, (elapsedTime / walkTime));
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        transform.position = pos;

        is_anim_on = false;
        if (anim != null && dir_str != null) anim.Play("idle_" + dir_str);
    }


    //(ONLY) Animation of the character throwing a spell
    public IEnumerator ThrowSpellAnim(Vector3Int dir)
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            is_anim_on = true;
            string dir_str = GetStrFromDir(dir);
            if (dir_str == null)
            {
                if (anim.GetCurrentAnimatorStateInfo(0).IsName("idle_up")) dir_str = "up";
                if (anim.GetCurrentAnimatorStateInfo(0).IsName("idle_down")) dir_str = "down";
                if (anim.GetCurrentAnimatorStateInfo(0).IsName("idle_left")) dir_str = "left";
                if (anim.GetCurrentAnimatorStateInfo(0).IsName("idle_right")) dir_str = "right";
            }
            anim.Play("spell_" + dir_str);
            yield return new WaitForSeconds(1f);
            is_anim_on = false;
            //We check again as we waited x seconds 
            if (anim != null) anim.Play("idle_" + dir_str);
        }
    }


    //Return a string from a direction
    private string GetStrFromDir(Vector3Int dir)
    {
        if (dir.x > 0) return "up";
        if (dir.x < 0) return "down";
        if (dir.y > 0) return "left";
        if (dir.y < 0) return "right";
        else return null;
    }


    //Handle messages received 
    public void HandleMessage<T>(Msg<T> msg)
    {
        //Order message
        if (msg.type == MsgType.OrderMsg)
        {
            Order order = (msg.msg as Order);
            AI ai = GetComponent<AI>();
            if (ai != null) ai.SetOrder(order);
            return;
        }

        //... Here add action according to the type of the message

        //Else
        //Can not handle message
        else
        {
            GameLog.Log("[CHAR] " + gameObject.name + " can not handle message : " + msg.type);
        }
    }
}



//Data class used for points with max and current
public class Points
{
    public int max;
    public int current;

    public Points(int p)
    {
        max = p;
        current = p;
    }

    public void Reset()
    {
        current = max;
    }

    public void Remove(int x)
    {
        current -= x;
    }

    public void Add(int x)
    {
        current = current + x > max ? max : current + x;
    }

    public bool IsEnough(int x)
    {
        return current >= x;
    }
}