using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Spell : MonoBehaviour
{
    [HideInInspector] public Character caster; //Caster of the spell
    [HideInInspector] public string spellName; //Name of the spell
    public int pa; //cost points
    public int minScope; //minimum scope (Manhattan Distance)
    public int maxScope; //maximum scope (Manhattan Distance)
    public bool is_line_scope; //If true, can be thrown only if target aligned (in x or y) to character
    public int scope_zone; //Scope zone (Manhattan Distance)
    public int cooldown; //Cooldown that prevent caster to throw spell if used before
    private int currentCooldown; //The spell can be thrown if <= 0


    public void Awake()
    {
        spellName = gameObject.name.Split('(')[0].Split('_')[1]; //Tqt mon reuf, to be changed later
        gameObject.name = spellName;
        ResetCooldown();
    }


    public abstract void ApplyOnCell(GridManager gridManager, GameCell cell);


    //To handle spell cooldown
    public void UpdateCooldown() { currentCooldown -= currentCooldown == 0 ? 0 : 1; }
    public void PutCooldown() { currentCooldown = cooldown; }
    public void ResetCooldown() { currentCooldown = 0; }
    public bool HasActiveCooldown() { return currentCooldown > 0; }
}
