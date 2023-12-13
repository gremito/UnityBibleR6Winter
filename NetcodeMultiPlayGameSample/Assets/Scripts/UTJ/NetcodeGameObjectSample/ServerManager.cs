using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

namespace UTJ.NetcodeGameObjectSample
{
    /// <summary>
    /// Netcodeのサーバー処理の実装
    /// </summary>
    public class ServerManager : MonoBehaviour
    {
        const int MAX_COIN = 20;

        private static ServerManager instance;
        public static ServerManager Instance
        {
            get { return instance; }
        }

        [SerializeField] private GameObject playerNetworkedPrefab;
        [SerializeField] private GameObject coinNetworkedPrefab;
        [SerializeField] private GameObject managerSystemPrefab;
        [SerializeField] private GameObject serverInfoRoot;
        [SerializeField] private GameObject gameStagePlane;
        [SerializeField] private UTJ.NetcodeGameObjectSample.ConfigureConnectionBehaviour configureConnection;
        [SerializeField] private Button stopButton;
        [SerializeField] private Text serverInfoText;

        private List<NetworkObject> coinList;

        private ConnectInfo cachedConnectInfo;

        void Awake()
        {
            instance = this;
            this.coinList = new List<NetworkObject>();
        }

        public void Setup(ConnectInfo connectInfo)
        {
            this.cachedConnectInfo = connectInfo;
            // サーバーとして起動したときのコールバック設定
            NetworkManager.Singleton.OnServerStarted += this.OnStartServer;
            // クライアントが接続された時のコールバック設定
            NetworkManager.Singleton.OnClientConnectedCallback += this.OnClientConnect;
            // クライアントが切断された時のコールバック設定
            NetworkManager.Singleton.OnClientDisconnectCallback += this.OnClientDisconnect;
            // transportの初期化
            NetworkManager.Singleton.NetworkConfig.NetworkTransport.Initialize();
        }

        public void SetupManager(ManagerSystem managerSystem)
        {
            managerSystem.ConfigureConnection = this.configureConnection;
        }

        /// <summary>
        /// Information用 テキスト設定
        /// </summary>
        public void SetInformationText(ConnectInfo connectInfo, string localIp) {
            if (!connectInfo.useRelay)
            {
                var stringBuilder = new System.Text.StringBuilder(256);
                this.serverInfoRoot.SetActive(true);
                stringBuilder.Append("サーバー接続情報\n").
                    Append("接続先IP:").Append(localIp).Append("\n").
                    Append("Port番号:").Append(connectInfo.port);
                this.serverInfoText.text = stringBuilder.ToString();
            }
        }

        /// <summary>
        /// Information用(Relay) テキスト設定
        /// </summary>
        public void SetInformationTextWithRelay(string joinCode)
        {
            var stringBuilder = new System.Text.StringBuilder(256);
            this.serverInfoRoot.SetActive(true);
            stringBuilder.Append("Relay接続情報\n").Append("コード:").Append(joinCode);
            this.serverInfoText.text = stringBuilder.ToString();
            UnityEngine.Debug.Log($"\n##################\n{this.serverInfoText.text}\n##################\n");
        }

        /// <summary>
        /// ゲームマネージャーをスポーンする
        /// </summary>
        private void SpawnGameManager()
        {
            if (!NetworkManager.Singleton.IsServer) return;

            var msObj = GameObject.Instantiate(this.managerSystemPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            var managerSystem = msObj.GetComponent<ManagerSystem>();
            managerSystem.ConfigureConnection = this.configureConnection;
            var nwObj = msObj.GetComponent<NetworkObject>();
            nwObj.Spawn();
        }

        /// <summary>
        /// コインの状態設定
        /// </summary>
        public void SetActiveCoin(bool active)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            foreach(var nwObj in this.coinList)
            {
                if (nwObj == null) continue;
                var c = nwObj.GetComponent<Coin>();
                if (active)
                    c.PlayGame();
                else
                    c.NoPlayGame();
                // UnityEngine.Debug.Log($"{nwObj.gameObject.name} - active: {nwObj.gameObject.active}/notGet: {c.notGet}");
            }
        }

        /// <summary>
        /// サーバー開始時の処理
        /// </summary>
        private void OnStartServer()
        {
            UnityEngine.Debug.Log("Start Server");
            var clientId = NetworkManager.ServerClientId;
            // hostならば生成します

            configureConnection.gameObject.SetActive(false);

            stopButton.GetComponentInChildren<Text>().text = "Stop Host";
            stopButton.onClick.AddListener(OnClickDisconnectButton);
            stopButton.gameObject.SetActive(true);

            SpawnGameManager();
            SpawnCoin();
        }

        /// <summary>
        /// クライアントが接続してきたときの処理
        /// </summary>
        private void OnClientConnect(ulong clientId)
        {
            UnityEngine.Debug.Log("Connect Client " + clientId);
            SpawnPlayer(clientId);
        }

        /// <summary>
        /// プレイヤーキャラをランダムに配置
        /// </summary>
        private void SpawnPlayer(ulong clientId)
        {
            var randomPosition = new Vector3(
                Random.Range(-5, 5),
                this.gameStagePlane.transform.position.y + 5.0f,
                Random.Range(-5, 5)
            );
            var gmo = GameObject.Instantiate(this.playerNetworkedPrefab, randomPosition, this.playerNetworkedPrefab.transform.rotation);
            var netObject = gmo.GetComponent<NetworkObject>();
            netObject.SpawnAsPlayerObject(clientId);
        }

        /// <summary>
        /// コインをランダムに配置
        /// </summary>
        private void SpawnCoin()
        {
            for (int i = 0 ; i < MAX_COIN ; i++)
            {
                var randomPosition = new Vector3(
                    Random.Range(-30,30),
                    this.gameStagePlane.transform.position.y + 0.75f,
                    Random.Range(-30,30)
                );
                var gmo = GameObject.Instantiate(this.coinNetworkedPrefab, randomPosition, this.coinNetworkedPrefab.transform.rotation);
                gmo.name = $"Coin_{i+1}";
                var netObject = gmo.GetComponent<NetworkObject>();
                netObject.Spawn();
                this.coinList.Add(netObject);
            }
        }

        /// <summary>
        /// ネットワーク同期するNetworkPrefabを生成
        /// </summary>
        private GameObject SpawnNetworkPrefab(GameObject prefab, Vector3 spawnPosition, ulong? clientId=null)
        {
            UnityEngine.Debug.Log("SpawnNetworkPrefab");
            var gmo = GameObject.Instantiate(prefab, spawnPosition, prefab.transform.rotation);
            var netObject = gmo.GetComponent<NetworkObject>();
            if (clientId.HasValue)
            {
                netObject.SpawnWithOwnership(clientId.Value);
            }
            else
            {
                netObject.Spawn();
            }
            return gmo;
        }

        /// <summary>
        /// ゲームプレイ結果集計
        /// </summary>
        public string GetResultAndEndPlay()
        {
            if (!NetworkManager.Singleton.IsServer) return "";

            // 集計
            var recordList = new List<KeyValuePair<string, int>>();
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                var ps = client.Value.PlayerObject.gameObject.GetComponent<UnityChanControllerWithNetcode>();
                UnityEngine.Debug.Log("プレイヤー名: " + ps.GetPlayerName() + " - ポインt: " + ps.GetPoint());
                recordList.Add(new KeyValuePair<string, int>(ps.GetPlayerName(), ps.GetPoint()));
            }

            // 降順ソート
            recordList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
            // プレイ結果情報作成
            return string.Join('\n', recordList.Select(pair => $"{pair.Key}: {pair.Value}"));
        }

        /// <summary>
        /// 切断ボタンが呼び出された時の処理
        /// </summary>
        private void OnClickDisconnectButton()
        {
            // all remove
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                if (client.Value.PlayerObject == null) continue;
                Destroy(client.Value.PlayerObject.gameObject);
            }

            this.coinList.ForEach(delegate(NetworkObject nwObj)
            {
                if (nwObj != null) Destroy(nwObj.gameObject);
            });
            this.coinList = new List<NetworkObject>();

            NetworkManager.Singleton.Shutdown();

            this.RemoveCallBack();
            this.configureConnection.ShowStartScene();
            this.configureConnection.gameObject.SetActive(true);
            this.stopButton.gameObject.SetActive(false);
            this.serverInfoRoot.SetActive(false);
        }

        /// <summary>
        /// 切断処理
        /// </summary>
        private void RemoveCallBack()
        {
            // サーバーとして起動したときのコールバック設定
            NetworkManager.Singleton.OnServerStarted -= this.OnStartServer;
            // クライアントが接続された時のコールバック設定
            NetworkManager.Singleton.OnClientConnectedCallback -= this.OnClientConnect;
            // クライアントが切断された時のコールバック設定
            NetworkManager.Singleton.OnClientDisconnectCallback -= this.OnClientDisconnect;
        }

        /// <summary>
        /// クライアントが切断した時の処理
        /// </summary>
        private void OnClientDisconnect(ulong clientId)
        {
            UnityEngine.Debug.Log("Disconnect Client " + clientId);
        }
    }
}
