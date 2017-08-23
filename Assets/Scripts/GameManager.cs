using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public Player[] Players { get; private set; }
    public Player TopPlayer { get { return Players[0]; } }
    public Player BotPlayer { get { return Players[1]; } }


    public void RegisterPlayer(Player player)
    {
        Players[player.id] = player;

        if (player.isLocalPlayer && !player.ai)
        {
            // local human player - flip camera if bot player
            if (!player.IsTop)
                Camera.main.transform.Rotate(Vector3.forward, 180);
        }

        Tools.Log("Registered Player " + player.id + (player.ai ? " (AI)" : ""), Color.blue);
    }

    private void Awake()
    {
        Players = new Player[2];
    }
}
