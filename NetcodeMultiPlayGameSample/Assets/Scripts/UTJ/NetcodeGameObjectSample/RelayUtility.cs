using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace UTJ.NetcodeGameObjectSample
{
    /// <summary>
    /// Relayに関する処理
    /// ref: https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop
    /// </summary>
    public class RelayServiceUtility
    {
        // Host側のコード
        #region HOST_CODE

        // RelayでJoinするためのコード
        public static string HostJoinCode { get; private set; }
        // Relayサーバーでの最大接続数
        private static readonly int k_MaxUnityRelayConnections = 10;

        /// <summary>
        /// リレーサーバーで中継する際のホスト処理に必要なデータ群
        /// </summary>
        public struct RelayHostData
        {
            public string ipv4Address;
            public ushort port;
            public byte[] allocationIdBytes;
            public byte[] connectionData;
            public byte[] key;
            public string joinCode;

            public RelayHostData(
                string ipv4Address,
                ushort port,
                byte[] allocationIdBytes,
                byte[] connectionData,
                byte[] key,
                string joinCode)
            {
                this.ipv4Address = ipv4Address;
                this.port = port;
                this.allocationIdBytes = allocationIdBytes;
                this.connectionData = connectionData;
                this.key = key;
                this.joinCode = joinCode;
            }
        }

        /// <summary>
        /// リレー ホスト処理 開始
        /// </summary>
        public static async void StartUnityRelayHost(
            Action onSuccess,
            Action onFailed)
        {
            try
            {
                // Unityサービスの初期化及びSignIn
                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    var playerId = AuthenticationService.Instance.PlayerId;
                    Debug.Log("認証後のPlayerID："+playerId);
                }

                var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();

                // Relayサーバーの確保をしてJoinコード取得
                var serverRelayUtilityTask = AllocateRelayServerAndGetJoinCode(k_MaxUnityRelayConnections);
                await serverRelayUtilityTask;

                // Relayサーバーの情報を取得してTransportに設定
                var relayHostData = serverRelayUtilityTask.Result;
                utp.SetRelayServerData(
                    // Set ipv4Address
                    relayHostData.ipv4Address,
                    // Set port
                    relayHostData.port,
                    // Set allocationIdBytes
                    relayHostData.allocationIdBytes,
                    // Set keyBytes
                    relayHostData.key,
                    // Set connectionDataBytes
                    relayHostData.connectionData);

                HostJoinCode = relayHostData.joinCode;
                NetworkManager.Singleton.StartHost();

                if (onSuccess == null) return;

                onSuccess();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                if (onFailed == null) return;

                onFailed();
            }
        }

        /// <summary>
        /// リレーサーバー 確保
        /// </summary>
        private static async Task<RelayHostData> AllocateRelayServerAndGetJoinCode(
            int maxConnections,
            string region = null)
        {
            Allocation allocation;
            string joinCode;

            try
            {
                allocation = await Relay.Instance.CreateAllocationAsync(maxConnections, region);

                //Debug.Log($"server: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
                //Debug.Log($"server: {allocation.AllocationId}");

                joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

            }
            catch (Exception exception)
            {
                throw new Exception($"Creating allocation request has failed: \n {exception.Message}");
            }

            return new RelayHostData(
                // Set ipv4Address
                allocation.RelayServer.IpV4,
                // Set port
                (ushort) allocation.RelayServer.Port,
                // Set allocationIdBytes
                allocation.AllocationIdBytes,
                // Set connectionData
                allocation.ConnectionData,
                // Set key
                allocation.Key,
                // Set joinCode
                joinCode);
        }

        #endregion HOST_CODE

        // Client側のコード
        #region CLIENT_CODE

        // リレーサーバーで中継する際のクライアント処理に必要なデータ群
        public struct RelayClientData
        {
            public string ipv4Address;
            public ushort port;
            public byte[] allocationIdBytes;
            public byte[] connectionData;
            public byte[] hostConnectionData;
            public byte[] key;

            public RelayClientData(
                string ipv4Address,
                ushort port,
                byte[] allocationIdBytes,
                byte[] connectionData,
                byte[] hostConnectionData,
                byte[] key)
            {
                this.ipv4Address = ipv4Address;
                this.port = port;
                this.allocationIdBytes = allocationIdBytes;
                this.connectionData = connectionData;
                this.hostConnectionData = hostConnectionData;
                this.key = key;
            }
        }

        /// <summary>
        /// リレー クライアント処理 開始
        /// </summary>
        public static async void StartClientUnityRelayModeAsync(string joinCode)
        {
            try
            {
                // UnityServiceを初期化してSignIn
                await UnityServices.InitializeAsync();

                //Debug.Log(AuthenticationService.Instance);
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    var playerId = AuthenticationService.Instance.PlayerId;
                    //Debug.Log(playerId);
                }

                // Joinコードから接続に関する情報を取得してセット
                var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
                var clientRelayUtilityTask = JoinRelayServerFromJoinCode(joinCode);
                await clientRelayUtilityTask;

                var relayClientData = clientRelayUtilityTask.Result;
                utp.SetRelayServerData(
                    // Set ipv4Address
                    relayClientData.ipv4Address,
                    // Set port
                    relayClientData.port,
                    // Set allocationIdBytes
                    relayClientData.allocationIdBytes,
                    // Set keyBytes
                    relayClientData.key,
                    // Set connectionDataBytes
                    relayClientData.connectionData,
                    // Set hostConnectionDataBytes
                    relayClientData.hostConnectionData);

                NetworkManager.Singleton.StartClient();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

        }

        /// <summary>
        /// JoinCodeから接続情報諸々を取得する
        /// </summary>
        private static async Task<RelayClientData> JoinRelayServerFromJoinCode(string joinCode)
        {
            JoinAllocation joinAllocation;

            try
            {
                joinAllocation = await Relay.Instance.JoinAllocationAsync(joinCode);

                // Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
                // Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
                // Debug.Log($"client: {allocation.AllocationId}");
            }
            catch (Exception exception)
            {
                throw new Exception($"Creating join code request has failed: \n {exception.Message}");
            }

            return new RelayClientData(
                // Set ipv4Address
                joinAllocation.RelayServer.IpV4,
                // Set port
                (ushort) joinAllocation.RelayServer.Port,
                // Set allocationIdBytes
                joinAllocation.AllocationIdBytes,
                // Set connectionData
                joinAllocation.ConnectionData,
                // Set hostConnectionData
                joinAllocation.HostConnectionData,
                // Set key
                joinAllocation.Key);
        }

        #endregion CLIENT_CODE
    }
}
