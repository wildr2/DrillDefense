using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    private Player player;
    public Text goldText;
    public Image connectingScreen;
    public AudioSource buildSound;
    public AudioSource launchDrillSound;

    private void Awake()
    {
        connectingScreen.gameObject.SetActive(true);

        GameManager gm = FindObjectOfType<GameManager>();
        gm.DoOncePlayersReady(() => { OnPlayersReady(gm); });
    }
    private void OnPlayersReady(GameManager gm)
    {
        connectingScreen.gameObject.SetActive(false);

        player = gm.LocalHuman;
        player.onInputBuild += () => buildSound.Play();
        player.onInputLaunchDrill += () => launchDrillSound.Play();
    }
    private void Update()
    {
        if (player == null) return;

        goldText.text = player.GetGold().ToString();
    }
}
