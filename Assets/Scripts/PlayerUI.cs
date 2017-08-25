using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    private Player player;
    public Text goldText;
    public AudioSource buildSound;
    public AudioSource launchDrillSound;

    private void Awake()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        gm.DoOncePlayersReady(() => { SetPlayer(gm.GetLocalPlayer()); });
    }
    private void SetPlayer(Player player)
    {
        this.player = player;
        player.onInputBuild += () => buildSound.Play();
        player.onInputLaunchDrill += () => launchDrillSound.Play();
    }
    private void Update()
    {
        if (player == null) return;

        goldText.text = player.GetGold().ToString();
    }
}
