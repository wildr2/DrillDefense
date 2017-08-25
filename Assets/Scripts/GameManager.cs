using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public Player[] Players { get; private set; }
    public Player TopPlayer { get { return Players[0]; } }
    public Player BotPlayer { get { return Players[1]; } }

    public bool IsPlaying { get; private set; }
    public bool PlayersReady { get; private set; }
    private System.Action onPlayersReady;


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
    public void DoOncePlayersReady(System.Action action)
    {
        if (PlayersReady) action();
        onPlayersReady += action;
    }

    private void Awake()
    {
        Players = new Player[2];
        IsPlaying = false;
    }
    private void OnAllPlayersRegistered()
    {
        PlayersReady = true;
        IsPlaying = true;

        if (onPlayersReady != null)
            onPlayersReady();
    }
}
