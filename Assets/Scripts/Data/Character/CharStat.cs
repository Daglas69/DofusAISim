using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Init character points
public class CharStat : MonoBehaviour
{
    public int maxLife = 150;
    public int maxPa = 6;
    public int maxPm = 4;
    public float damage_resistance = 0f;
    public GameObject[] spellPrefabs;


    void Awake()
    {
        var character = gameObject.GetComponent<Character>();

        //Add stats
        character.life = new Points(maxLife);
        character.pa = new Points(maxPa);
        character.pm = new Points(maxPm);
        character.damage_resistance = damage_resistance;

        //Init buff list
        character.buffs = new List<Buff>();

        //Init spell list
        character.spells = new List<Spell>();
        foreach (GameObject spellPrefab in spellPrefabs)
        {
            GameObject spellObj = Instantiate(spellPrefab); //Create from prefab
            Spell spell = spellObj.GetComponent<Spell>();
            spell.caster = character; //Set caster
            character.spells.Add(spell); //Add to caster spell list
            spellObj.hideFlags = HideFlags.HideInHierarchy; //Desactivate gameObject from editor hierarchy
            spellObj.SetActive(false); //Desactivate gameObject on the scene (we use its data)
        }
    }
}
