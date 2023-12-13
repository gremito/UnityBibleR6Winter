using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UTJ.NetcodeGameObjectSample
{
    /// <summary>
    /// Netcodeのクライアント処理実装
    /// </summary>
    public class ClientManager : MonoBehaviour
    {
        private static ClientManager instance;
        public static ClientManager Instance
        {
            get { return instance; }
        }

        [SerializeField] private UTJ.NetcodeGameObjectSample.ConfigureConnectionBehaviour configureConnection;
        [SerializeField] private Button stopButton;

        private bool previewConnected;
        
        void Awake()
        {
            instance = this;
        }

        void Update()
        {
            var netMgr = NetworkManager.Singleton;

            var currentConnected = netMgr.IsConnectedClient;

            // 3人以上接続時に切断が呼び出されないので対策
            if (currentConnected != previewConnected)
            {
                if (!currentConnected)
                {
                    Disconnect();
                }
                else
                {
                    OnConnectSelf();
                }
            }

            previewConnected = netMgr.IsConnectedClient;
        }

        public void Setup()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }

        public void SetupManager(ManagerSystem managerSystem)
        {
            managerSystem.ConfigureConnection = this.configureConnection;
        }

        private void ReoveCallbacks()
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnect;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        private void Disconnect()
        {
#if ENABLE_AUTO_CLIENT
            // クライアント接続時に切断したらアプリ終了させます
            if (NetworkUtility.IsBatchModeRun)
            {
                Application.Quit();
            }
#endif
            // UIを戻します
            configureConnection.gameObject.SetActive(true);
            stopButton.gameObject.SetActive(false);
            stopButton.onClick.RemoveAllListeners();

            // コールバックも削除します
            ReoveCallbacks();
        }

        private void OnClickStopButton()
        {
            NetworkManager.Singleton.Shutdown();
            Disconnect();
        }

        private void OnClientConnect(ulong clientId)
        {
            Debug.Log($"Connect Client: {clientId}::{NetworkManager.Singleton.LocalClientId}");
        }

        private void OnClientDisconnect(ulong clientId)
        {
            Debug.Log($"Disconnect Client: {clientId}");
        }

        /// <summary>
        /// 自信が接続した時に呼び出されます
        /// </summary>
        private void OnConnectSelf()
        {
            configureConnection.gameObject.SetActive(false);
            stopButton.GetComponentInChildren<Text>().text = "Disconnect";
            stopButton.onClick.AddListener(this.OnClickStopButton);
            stopButton.gameObject.SetActive(true);
        }
    }
}
