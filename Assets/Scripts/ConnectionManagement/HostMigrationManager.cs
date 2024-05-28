using System;
using System.Collections;
using System.Collections.Generic;
using Unity.BossRoom.ConnectionManagement;
using Unity.Netcode;
using UnityEngine;
using Unity.Networking.Transport.TLS;
using Unity.Multiplayer.Samples.Utilities;
using UnityEngine.SceneManagement;

public class HostMigrationManager : NetworkBehaviour
{
    private static HostMigrationManager _instance;
    public static HostMigrationManager instance => _instance;

    //Used for chache clients connection data, to send "good" connection to other players
    public PlayerConnectionDataIp LocalConnection;

    private List<PlayerConnectionDataIp> PlayerConnections_Server = new List<PlayerConnectionDataIp>();
    private PlayerConnectionDataIp[] PlayerConnections = new PlayerConnectionDataIp[0]; // First is allways host

    int LastIndex = 0;

    [SerializeField] private ConnectionManager connectionManager;
    bool isHostMigration = false;

    private void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(this);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (LocalConnection != null)
        {
            AddConnectionIp_ServerRpc(LocalConnection);
        }
    }

    public void SetLocalConnection(PlayerConnectionDataIp connection)
    {
        LocalConnection = connection;

        if (IsSpawned)
            AddConnectionIp_ServerRpc(connection);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddConnectionIp_ServerRpc(PlayerConnectionDataIp data)
    {
        if(PlayerConnections_Server.Find(e => e.Equals(data)) == null) //Add only unique connections
        {
            PlayerConnections_Server.Add(data);
            UpdateConnections_ClientRpc(PlayerConnections_Server.ToArray());

        }
    }

    [ClientRpc]
    public void UpdateConnections_ClientRpc(PlayerConnectionDataIp[] connections)
    {
        PlayerConnections = connections;
    }

    public PlayerConnectionDataIp GetNextPossibleConnection()
    {
        LastIndex++;

        PlayerConnectionDataIp playerConnectionDataIp = null;
        if (LastIndex < PlayerConnections.Length)
            playerConnectionDataIp = PlayerConnections[LastIndex];

        return playerConnectionDataIp;
    }

    public void StartHostMigration()
    {
        isHostMigration = true;

        PlayerConnectionDataIp newHostIp = GetNextPossibleConnection();
        if (newHostIp == null)
        {
            StopHostMigration();
            return;
        }

        //Wait for other players
        StartCoroutine(HostMigrationCoroutine(newHostIp));
    }

    public void StopHostMigration()
    {
        isHostMigration = false;
    }

    private IEnumerator HostMigrationCoroutine(PlayerConnectionDataIp newHostIp)
    {
        //Wait untill main menu is loaded
        yield return new WaitUntil(() => SceneManager.GetActiveScene() == SceneManager.GetSceneByName("MainMenu"));

        if (LocalConnection.Equals(newHostIp))
        {
            connectionManager.StartHostIp(newHostIp.playerName, newHostIp.ipaddress, newHostIp.port);
        }
        else
        {
            connectionManager.StartClientIp(LocalConnection.playerName, LocalConnection.ipaddress, LocalConnection.port);
        }

        //if(NetworkManager.Singleton.IsServer)
        //{
        //    yield return new WaitUntil(() => NetworkManager.Singleton.ConnectedClientsIds.Count == PlayerConnections_Server.Count); //Here we can start game again
        //}

        StopHostMigration();
    }
}
