using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Coin : NetworkBehaviour
{
    private const int POINT = 1;

    #region NETWORKED_VAR
    // 取得許可フラグ
    private NetworkVariable<bool> notGet = new NetworkVariable<bool>(false);
    // 取得済みフラグ
    private NetworkVariable<bool> isBeenAcquired = new NetworkVariable<bool>(false);
    #endregion NETWORKED_VAR

    /// <summary>
    /// スポーン後初期化
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        notGet.Value = true;
        isBeenAcquired.Value = false;
    }

    void Update()
    {
        transform.Rotate(Vector3.up * 50 * Time.deltaTime, Space.World);

        if (IsServer && isBeenAcquired.Value) Destroy(gameObject);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            if (this.notGet.Value) return;
            var player = collision.gameObject.GetComponent<UnityChanControllerWithNetcode>();
            if  (!player.IsOwner) return;
            Debug.Log(this.gameObject.name + ": プレイヤー（" + player.GetPlayerName() + "）が獲得");
            player.AddPoint(POINT);
            AcquiredServerRpc();
        }
    }

    /// <summary>
    /// 取得済み同期依頼
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void AcquiredServerRpc()
    {
        this.isBeenAcquired.Value = true;
    }

    /// <summary>
    /// ゲームプレイ可能処理
    /// </summary>
    public void PlayGame()
    {
        if (!IsServer) return;
        notGet.Value = false;
    }

    /// <summary>
    /// ゲームプレイ終了処理
    /// </summary>
    public void NoPlayGame()
    {
        if (!IsServer) return;
        notGet.Value = true;
    }
}
