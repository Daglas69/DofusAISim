using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QlearnAI : AI
{
    private QlearnManager manager;
    public Player player;
    public Player enemy;


    override public void Init()
    {
        manager = FindObjectOfType<QlearnManager>();
        player = character.grp == 1 ? manager.player1 : manager.player2;
        enemy = character.grp == 1 ? manager.player2 : manager.player1;
    }

    //plusieurs actions par tour ? PB ?
    override public void Execute()
    {
        //on stocke les etats pour pouvoir creer des transitions pour les historiques
        player.previousState = player.currentState;
        enemy.previousState = enemy.currentState;

        // on laisse le player choisir son action et on l'applique
        int action = player.Play();
        QlearnTools.ApplyAction(manager, player, action); // cette ligne change aussi l'etat du j1

        enemy.currentState = player.currentState.Reverse(); // cette ligne change l'etat du j2 en faisant un mirror du j1

        //On recupère la récompense liée à l'action
        int reward = Rewards.tab[action];

        // si le combat est fini ça signifie que la dernière action a été décisive, donc la récompense est caduque et est donc remplacée par celle de victoire
        if (battleManager.IsGameFinished())
        {
            reward = Rewards.win;
            //Bug : If one of them not QlearnAI => not updated
            player.win_nb++;
            enemy.lose_nb++;
        }

        // on ajoute les transitions aux deux historiques
        Transition trans2 = new Transition(enemy.previousState, action, -reward, enemy.currentState);
        enemy.AddTransition(trans2);
        Transition trans = new Transition(player.previousState, action, reward, player.currentState);
        player.AddTransition(trans);

        GameLog.Log("[QLEARN] " + gameObject.name + " state " + player.currentState.distance + " " + player.currentState.agentLife + " " + player.currentState.enemyLife);
    }
}
