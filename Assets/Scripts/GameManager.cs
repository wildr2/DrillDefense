using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public Player[] Players { get; private set; }
    public Player TopPlayer { get { return Players[0]; } }
    public Player BotPlayer { get { return Players[1]; } }

    public bool PlayersReady { get; private set; }
    public System.Action onPlayersReady;




    public Player GetLocalPlayer()
    {
        return TopPlayer.isLocalPlayer ? TopPlayer : BotPlayer;
    }

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

        if (TopPlayer != null && BotPlayer != null)
        {
            OnAllPlayersRegistered();
        }
    }

    private void Awake()
    {
        Players = new Player[2];
    }
    private void OnAllPlayersRegistered()
    {
        PlayersReady = true;
        if (onPlayersReady != null)
            onPlayersReady();
    }
}
