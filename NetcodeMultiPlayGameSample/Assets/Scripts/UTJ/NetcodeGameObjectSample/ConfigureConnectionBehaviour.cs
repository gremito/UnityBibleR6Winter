using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

namespace UTJ.NetcodeGameObjectSample
{
    // 接続設定や接続をするUIのコンポーネント
    public class ConfigureConnectionBehaviour : MonoBehaviour
    {
        // IPアドレス表示用
        [SerializeField] private Text localIpInfoText;
        // Relayサーバー使用するかチェックボックス
        [SerializeField] private Toggle useRelayToggle;
        // 接続先IP入力ボックス
        [SerializeField] private InputField ipInputField;
        // 接続ポート入力フィールド
        [SerializeField] private InputField portInputField;
        // Relay コード入力ボックス
        [SerializeField] private InputField relayJoinCodeInputField;
        // プレイヤー名入力フィールド
        [SerializeField] private InputField playerNameInputFiled;
        // リセットボタン
        [SerializeField] private Button resetButton;
        // ホストとして接続ボタン
        [SerializeField] private Button hostButton;
        // クライアントボタン
        [SerializeField] private Button clientButton;
        // // ホスト（サーバー）立上げ後に諸々管理するマネージャー
        // [SerializeField] private ServerManager serverManager;
        // // Client接続後に諸々管理するマネージャー
        // [SerializeField] private ClientManager clientManager;
        // スタート・リザルト画面専用カメラ
        [SerializeField] private Camera mainCamera;
        // スタート・リザルト画面上のタイトルテキスト
        [SerializeField] private Text titleText;
        // ゲーム中のタイムテキスト
        [SerializeField] private Text timeText;
        // リザルト画面上の結果テキスト
        [SerializeField] private Text resultText;

        // 接続設定
        private ConnectInfo connectInfo;
        // タイトル
        private string startTitle;

        // 一旦Player名の保存箇所です
        public static string playerName;

        // ローカルのIPアドレス
        private string localIPAddr;

        void Awake()
        {
            // ターゲットフレームレートをちゃんと設定する
            // ※60FPSにしないと Headlessサーバーや、バッチクライアントでブン回りしちゃうんで…
            Application.targetFrameRate = 60;

            localIPAddr = NetworkUtility.GetLocalIP();
            this.localIpInfoText.text = "あなたのIPアドレスは、" + localIPAddr;

            this.connectInfo = ConnectInfo.LoadFromFile();
            ApplyConnectInfoToUI();

            this.resetButton.onClick.AddListener(OnClickReset);
            this.hostButton.onClick.AddListener(OnClickHost);
            this.clientButton.onClick.AddListener(OnClickClient);

            this.startTitle = this.titleText.text;
        }

        void Start()
        {
            // サーバービルド時
#if UNITY_SERVER 
            Debug.Log("Server Build.");
            ApplyConnectInfoToNetworkManager(true);
            // this.serverManager.Setup(this.connectInfo);
            ServerManager.Instance.Setup(this.connectInfo);
            // あと余計なものをHeadless消します
            NetworkUtility.RemoveUpdateSystemForHeadlessServer();

            // サーバー起動
            NetworkManager.Singleton.StartServer();
#elif ENABLE_AUTO_CLIENT
            if (NetworkUtility.IsBatchModeRun)
            {
                // バッチモードでは余計なシステム消します
                NetworkUtility.RemoveUpdateSystemForBatchBuild();
                OnClickClient();
            }
#endif
        }

        // Hostとして起動ボタンを押したとき
        private void OnClickHost()
        {
            GenerateConnectInfoValueFromUI();

            ApplyConnectInfoToNetworkManager(true);

            this.connectInfo.SaveToFile();

            // 既にクライアントとして起動していたら、クライアントを止めます
            if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
            }

            // ServerManagerでコールバック回りを設定
            // this.serverManager.Setup(this.connectInfo);
            ServerManager.Instance.Setup(this.connectInfo);

            // Relayを使用する場合
            if (connectInfo.useRelay)
            {
                RelayServiceUtility.StartUnityRelayHost(
                    () =>
                    {
                        // this.serverManager.SetInformationTextWithRelay(RelayServiceUtility.HostJoinCode);
                        ServerManager.Instance.SetInformationTextWithRelay(RelayServiceUtility.HostJoinCode);
                    },
                    null);
            }
            // Relayを利用しないなら即ホストとして起動
            else
            {
                // this.serverManager.SetInformationText(connectInfo, localIPAddr);
                ServerManager.Instance.SetInformationText(connectInfo, localIPAddr);
                var result = NetworkManager.Singleton.StartHost();
            }
        }

        // Clientとして起動ボタンを押したとき
        private void OnClickClient()
        {
            GenerateConnectInfoValueFromUI();

            ApplyConnectInfoToNetworkManager(false);

            this.connectInfo.SaveToFile();

            // ClientManagerでMLAPIのコールバック等を設定
            // this.clientManager.Setup();
            ClientManager.Instance.Setup();

            // クライアントとして起動
            if (connectInfo.useRelay)
            {
                RelayServiceUtility.StartClientUnityRelayModeAsync(connectInfo.relayCode);
            }
            // Relay使用しないならそのまま
            else
            {
                var result = NetworkManager.Singleton.StartClient();
            }
        }

        // Resetボタンを押したとき
        public void OnClickReset()
        {
            this.connectInfo = ConnectInfo.GetDefault();
            ApplyConnectInfoToUI();
        }

        // ロードした接続設定をUIに反映させます
        private void ApplyConnectInfoToUI()
        {
            this.useRelayToggle.isOn        = this.connectInfo.useRelay;
            this.ipInputField.text          = this.connectInfo.ipAddr;
            this.portInputField.text        = this.connectInfo.port.ToString();
            this.playerNameInputFiled.text  = this.connectInfo.playerName;
        }

        // 接続設定をUIから構築します
        private void GenerateConnectInfoValueFromUI()
        {
            this.connectInfo.useRelay = this.useRelayToggle.isOn;
            this.connectInfo.ipAddr = this.ipInputField.text;
            this.connectInfo.relayCode = this.relayJoinCodeInputField.text;
            int.TryParse(this.portInputField.text, out this.connectInfo.port);
            this.connectInfo.playerName = this.playerNameInputFiled.text;
        }

        // 接続設定をMLAPIのネットワーク設定に反映させます
        private void ApplyConnectInfoToNetworkManager(bool isServer)
        {
            // NetworkManagerから通信実体のTransportを取得します
            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;

            // ※UnityTransportとして扱います
            var unityTransport = transport as Unity.Netcode.Transports.UTP.UnityTransport;
            if (unityTransport != null)
            {
                // サーバーはAnyから受け付けます
                if (isServer)
                {
                    // ここのConnectionDataが、何処から受け付けるかの設定になる！！！
                    unityTransport.SetConnectionData(
                        IPAddress.Any.ToString(),
                        (ushort)this.connectInfo.port
                    );
                }
                else
                {
                    unityTransport.SetConnectionData(
                        this.connectInfo.ipAddr.Trim(),
                        (ushort)this.connectInfo.port
                    );
                }
            }

            // あとPlayer名をStatic変数に保存しておきます
            playerName = this.connectInfo.playerName;
        }

        /// <summary>
        /// ゲーム開始 初期化
        /// </summary>
        public void ShowGameScene()
        {
            this.mainCamera.gameObject.SetActive(false);
            this.titleText.gameObject.SetActive(false);
            this.timeText.text = "Ready?";
            this.timeText.gameObject.SetActive(true);
        }

        /// <summary>
        /// 制限時間表示
        /// </summary>
        public void SetShowTimeCount(float time)
        {
            this.timeText.text = time.ToString("0.00");
        }

        /// <summary>
        /// スタート画面 初期化
        /// </summary>
        public void ShowResultScene(string resultPlayInfo)
        {
            this.timeText.text = "";
            this.timeText.gameObject.SetActive(false);
            this.titleText.text = "結果発表";
            this.titleText.gameObject.SetActive(true);
            // this.resultText.text = this.serverManager.GetResultAndEndPlay();
            this.resultText.text = resultPlayInfo;
            this.resultText.gameObject.SetActive(true);
            this.mainCamera.gameObject.SetActive(true);
        }

        /// <summary>
        /// スタート画面 初期化
        /// </summary>
        public void ShowStartScene()
        {
            this.titleText.text = this.startTitle;
            this.titleText.gameObject.SetActive(true);
            this.resultText.text = "";
            this.resultText.gameObject.SetActive(false);
        }
    }
}
