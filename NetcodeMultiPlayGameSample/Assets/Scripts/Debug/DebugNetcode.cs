using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class DebugNetcode : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject plane;

    [SerializeField] private ProjectSceneManager projectSceneManager;

    [SerializeField] private RectTransform connects;
    [SerializeField] private Button backBtn;
    [SerializeField] private Button nextSceneBtn;

    void Start()
    {
        InitConnect();
    }

    private void InitConnect()
    {
        backBtn.onClick.AddListener(this.OnClickStopButton);
        nextSceneBtn.onClick.AddListener(this.projectSceneManager.GoScene);

        SetConnects(true);

        if (NetworkManager.Singleton.IsServer)
        {
            Unity.Netcode.NetworkManager.Singleton.OnServerStarted += this.OnStartServer;
            Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback += this.OnClientConnect;
            Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback += this.OnClientDisconnect;
        }
        else
        {
            Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback += this.OnClientConnect;
            Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback += this.OnClientDisconnect;
        }
    }

    private void SetConnects(bool active)
    {
        this.connects.gameObject.SetActive(active);
        this.backBtn.gameObject.SetActive(!active);
        this.nextSceneBtn.gameObject.SetActive(!active);
    }

    public void StartHost()
    {
        SetConnects(false);
        Unity.Netcode.NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        SetConnects(false);
        Unity.Netcode.NetworkManager.Singleton.StartClient();
    }

    private void OnStartServer()
    {
        var clientId = Unity.Netcode.NetworkManager.ServerClientId;
        Debug.Log("Start Server client: " + clientId);
    }

    private void OnClientConnect(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Connect Client: " + clientId);
            SpawnPlayer(clientId);
        }
        else
        {
            Debug.Log($"Connect Client: {clientId}::{Unity.Netcode.NetworkManager.Singleton.LocalClientId}");
        }
    }

    void SpawnPlayer(ulong clientId)
    {
        var randomPosition = new Vector3(
            Random.Range(-5, 5),
            plane.transform.position.y + 1f,
            Random.Range(-5, 5)
        );
        var gmo = GameObject.Instantiate(playerPrefab, randomPosition, playerPrefab.transform.rotation);
        var netObject = gmo.GetComponent<NetworkObject>();
        // netObject.SpawnWithOwnership(clientId);
        netObject.SpawnAsPlayerObject(clientId);
    }

    private void OnClientDisconnect(ulong clientId)
    {
        Debug.Log("Disconnect Client: " + clientId);
    }

    private void OnClickStopButton()
    {
        Unity.Netcode.NetworkManager.Singleton.Shutdown();
        ReoveCallbacks();
        InitConnect();
    }

    private void ReoveCallbacks()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Unity.Netcode.NetworkManager.Singleton.OnServerStarted -= this.OnStartServer;
            Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback -= this.OnClientConnect;
            Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback -= this.OnClientDisconnect;
        }
        else
        {
            Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback -= this.OnClientConnect;
            Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback -= this.OnClientDisconnect;
        }
    }
}
