using UnityEngine;
using UnityEngine.UI;

public class PanelManager : MonoBehaviour
{
    protected BattleManager battleManager;
    public GameObject grp1_info;
    public GameObject grp2_info;
    public GameObject game_info;


    public void Start()
    {
        battleManager = FindObjectOfType<BattleManager>();
        Init();
    }


    public void Init()
    {
        grp1_info.GetComponent<Text>().text = "";
        grp2_info.GetComponent<Text>().text = "";
        game_info.GetComponent<Text>().text = "";
    }


    //Update the UI
    public void Update()
    {
        //GROUP 1
        grp1_info.GetComponent<Text>().text = GetGroupInfo(1, battleManager.grp1_chars);

        //GROUP 2
        grp2_info.GetComponent<Text>().text = GetGroupInfo(2, battleManager.grp2_chars);

        //Update current group turn + played games
        game_info.GetComponent<Text>().text =
            "Nb played games : " + battleManager.nb_parties_ended.ToString() + " / " + battleManager.loop_parties.ToString() + "\n"
            + "Group turn : " + battleManager.grp_turn.ToString() + "\n"
            + (battleManager.is_paused ? "GAME PAUSED" : "");
    }


    //Return a string containing the info of a group of characters passed as argument
    private string GetGroupInfo(int grp, GameObject[] chars)
    {
        string info = "";

        //Nb win
        info += "Nb wins : " + (grp == 1 ? battleManager.nb_wins_grp1 : battleManager.nb_wins_grp2).ToString();
        info += "\n\n";

        //Stats of the chars
        foreach (GameObject charObj in chars)
        {
            if (charObj != null)
            {
                var character = charObj.GetComponent<Character>();
                info +=
                    "Name : " + charObj.name + "\n" +
                    "Life : " + character.life.current + "/" + character.life.max + "\n" +
                    "PA : " + character.pa.current + "/" + character.pa.max + "   " +
                    "PM : " + character.pm.current + "/" + character.pm.max;
                var qlearnAI = charObj.GetComponent<QlearnAI>();
                if (qlearnAI != null) info += "\nEpsilon : " + qlearnAI.player.epsilon;
                info += "\n\n";
            }
        }

        return info;
    }


    //Handle actions of buttons
    public void Btn_Pause()
    {
        battleManager.Action_Pause();
    }
    public void Btn_ChangeTurn()
    {
        battleManager.Action_ChangeTurn();
    }
    public void Btn_Exit()
    {
        battleManager.Action_ReturnMenu();
    }
}
