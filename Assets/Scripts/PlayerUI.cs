using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    private Player player;
    public Text goldText;

    private void Awake()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        gm.onPlayersReady += () => { player = gm.GetLocalPlayer(); };
    }
    private void Update()
    {
        if (player == null) return;

        goldText.text = player.GetGold().ToString();
    }
}
