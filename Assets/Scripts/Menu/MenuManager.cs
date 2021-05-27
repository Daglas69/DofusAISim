using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using QuantumTek.SimpleMenu;

public class MenuManager : MonoBehaviour
{
    public GameObject[] chars;
    public GameObject[] qlearn_chars;

    public void Start()
    {
        InitVariables.Reset();
    }

    public void RunDungeon()
    {
        try
        {
            //Group 1
            List<GameObject> grp1_chars = new List<GameObject>();
            for (int i = 0; i < 6; ++i) //We limit to 6 characters for one group (performance issues)
            {
                //[WARNING] Index in option list must be the equal to index in chars array
                //[WARNING] Game object inputs must have the right name
                int char_ind = GameObject.Find("Input_char_" + (i + 1)).GetComponent<SM_OptionList>().current;
                if (char_ind < chars.Length) grp1_chars.Add(chars[char_ind]);
            }
            if (grp1_chars.Count == 0) return; //No chars chosen
            bool grp1_leader = GameObject.Find("Input_grp1_leading_dungeon").GetComponent<Toggle>().isOn;
            bool full_speed = GameObject.Find("Input_speed_dungeon").GetComponent<SM_OptionList>().currentOption == "Full speed";
            bool group_play_together = GameObject.Find("Input_groupPlayTogether_dungeon").GetComponent<Toggle>().isOn;

            //To indicate that game must used these vars
            InitVariables.is_init_from_menu = true;
            InitVariables.full_speed = full_speed;
            InitVariables.loop_parties = 1;
            InitVariables.group_play_together = group_play_together;
            InitVariables.grp1_leader = grp1_leader;
            InitVariables.grp1_chars = grp1_chars.ToArray();
            InitVariables.grp2_leader = false;
            InitVariables.grp2_chars = new GameObject[1]; //Will be init in BattleManager
            InitVariables.is_dungeon_mode = true;

            //Load first scene
            SceneManager.LoadScene("Donjon-1");
        }
        catch (Exception _)
        {
            Debug.Log("Error " + _.Message);
        }
    }


    public void RunFight()
    {
        try
        {
            //Group 1
            List<GameObject> grp1_chars = new List<GameObject>();
            for (int i = 0; i < 6; ++i) //We limit to 6 characters for one group (performance issues)
            {
                //[WARNING] Index in option list must be the equal to index in chars array
                //[WARNING] Game object inputs must have the right name
                int char_ind = GameObject.Find("Input_grp1_" + (i+1)).GetComponent<SM_OptionList>().current;
                if (char_ind < chars.Length) grp1_chars.Add(chars[char_ind]);
            }
            if (grp1_chars.Count == 0) return; //No chars chosen
            bool grp1_leader = GameObject.Find("Input_grp1_leading").GetComponent<Toggle>().isOn;

            //Group 2
            List<GameObject> grp2_chars = new List<GameObject>();
            for (int i = 0; i < 6; ++i) //We limit to 6 characters for one group (performance issues)
            {
                //[WARNING] Index in option list must be the equal to index in chars array
                //[WARNING] Game object inputs must have the right name
                int char_ind = GameObject.Find("Input_grp2_" + (i + 1)).GetComponent<SM_OptionList>().current;
                if (char_ind < chars.Length) grp2_chars.Add(chars[char_ind]);
            }
            if (grp2_chars.Count == 0) return; //No chars chosen
            bool grp2_leader = GameObject.Find("Input_grp2_leading").GetComponent<Toggle>().isOn;

            bool full_speed = GameObject.Find("Input_speed_fight").GetComponent<SM_OptionList>().currentOption == "Full speed";
            bool group_play_together = GameObject.Find("Input_groupPlayTogether_fight").GetComponent<Toggle>().isOn;
            int nbGames = Int32.Parse(GameObject.Find("Input_nbGames_fight").GetComponent<TMP_InputField>().text);

            //To indicate that game must used these vars
            InitVariables.is_init_from_menu = true;
            InitVariables.full_speed = full_speed;
            InitVariables.loop_parties = nbGames;
            InitVariables.group_play_together = group_play_together;
            InitVariables.grp1_leader = grp1_leader;
            InitVariables.grp1_chars = grp1_chars.ToArray();
            InitVariables.grp2_leader = grp2_leader;
            InitVariables.grp2_chars = grp2_chars.ToArray();

            //Load scene
            string scene = GameObject.Find("Input_dungeonRoom").GetComponent<SM_OptionList>().currentOption;
            SceneManager.LoadScene(scene);
        }
        catch (Exception _)
        {
            Debug.Log("Error " + _.Message);
        }
    }


    public void RunQlearn()
    {
        try
        {
            double epsilon = Double.Parse(GameObject.Find("Input_epsilon").GetComponent<TMP_InputField>().text);
            double coeffEpsilon = Double.Parse(GameObject.Find("Input_coeffEpsilon").GetComponent<TMP_InputField>().text);
            int trainingRatio = Int32.Parse(GameObject.Find("Input_trainingRatio").GetComponent<TMP_InputField>().text);
            int nbGames = Int32.Parse(GameObject.Find("Input_nbGames").GetComponent<TMP_InputField>().text);
            bool full_speed = GameObject.Find("Input_speed").GetComponent<SM_OptionList>().currentOption == "Full speed";
            bool trainable = GameObject.Find("Input_trainable").GetComponent<SM_OptionList>().currentOption == "Yes";
            string p1 = GameObject.Find("Input_player1").GetComponent<SM_OptionList>().currentOption;
            string p2 = GameObject.Find("Input_player2").GetComponent<SM_OptionList>().currentOption;
            int p1_ind = GetQlearnCharInd(p1);
            int p2_ind = GetQlearnCharInd(p2);

            //Game vars
            InitVariables.is_init_from_menu = true;
            InitVariables.full_speed = full_speed;
            InitVariables.grp1_chars = new GameObject[1] { qlearn_chars[p1_ind] };
            InitVariables.grp2_chars = new GameObject[1] { qlearn_chars[p2_ind] };

            //Qlearn vars
            QlearnVariables.EPSILON = epsilon;
            QlearnVariables.COEFF_EPSILON = coeffEpsilon;
            QlearnVariables.TRAINING_RATIO = trainingRatio;
            QlearnVariables.NUMBER_OF_TRAINING_GAMES = nbGames;
            QlearnVariables.TRAINABLE = trainable;

            SceneManager.LoadScene("Donjon-qlearn");
        }
        catch (Exception _)
        {
            Debug.Log("Error " + _.Message);
        }
    }


    public void Exit()
    {
        Application.Quit();
    }


    //Hardcoded
    //We did not have time to make it more adaptable
    private int GetQlearnCharInd(string charName)
    {
        if (charName == "AI Qlearn") return 0;
        if (charName == "AI Simple") return 1;
        if (charName == "Player") return 2;
        else throw new Exception("Player not found " + charName);
    }
}
