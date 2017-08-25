using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

class CustomNetworkManager : NetworkManager
{
    private short connectedPlayers = 0;

    public override void OnStartServer()
    {
        connectedPlayers = 0;
        base.OnStartServer();
    }
    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        DataManager dm = DataManager.Instance;

        Player player = Instantiate(playerPrefab).GetComponent<Player>();
        player.id = connectedPlayers;
        player.ai = connectedPlayers >= dm.NumHumans;

        NetworkServer.AddPlayerForConnection(conn, player.gameObject, playerControllerId);
        ++connectedPlayers;

        // Add additional AI if all clients connected
        if (connectedPlayers >= dm.NumHumans && connectedPlayers < DataManager.numPlayers)
            ClientScene.AddPlayer((short)(playerControllerId + 1));
    }
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        connectedPlayers -= 1;
        base.OnServerDisconnect(conn);
    }
    
}