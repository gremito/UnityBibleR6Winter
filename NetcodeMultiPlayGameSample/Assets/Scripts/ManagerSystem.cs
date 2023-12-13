using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UTJ.NetcodeGameObjectSample;

public class ManagerSystem : NetworkBehaviour
{
    const int MAX_STAND_BY_TIME = 10;
    const int MAX_TIME = 25;

    [SerializeField]
    private ConfigureConnectionBehaviour configureConnection;
    public ConfigureConnectionBehaviour ConfigureConnection
    {
        set { this.configureConnection = value; }
    }

    #region NETWORKED_VAR
    // ゲームプレイフラグ
    private NetworkVariable<bool> isGame = new NetworkVariable<bool>(false);
    // 待機時間
    private NetworkVariable<float> standByTime = new NetworkVariable<float>(MAX_STAND_BY_TIME);
    // ゲームの時間制限
    private NetworkVariable<float> gameTime = new NetworkVariable<float>(MAX_TIME);
    // 結果情報
    private NetworkVariable<Unity.Collections.FixedString64Bytes> resultPlayInfo
        = new NetworkVariable<Unity.Collections.FixedString64Bytes>();
    #endregion NETWORKED_VAR

    /// <summary>
    /// スポーン後初期化
    /// </summary>
    public override void OnNetworkSpawn()
    {
        this.isGame.OnValueChanged += OnChangedIsGame;
        this.standByTime.OnValueChanged += OnChangedStandByTime;
        this.gameTime.OnValueChanged += OnChangedGameTime;

        InitGame();
    }

    void Update()
    {
        CountDownReadyTime();

        if (!this.isGame.Value) return;

        CountDownGameTime();
    }

    /// <summary>
    /// ゲームプレイフラグ更新後処理
    /// </summary>
    private void OnChangedIsGame(bool prev, bool current)
    {
        // UnityEngine.Debug.Log("OnChangedIsGame: prev=" + prev + " → current=" + current);
        // ゲーム終了後処理
        if (prev && !current)
        {
            EndPlayGame();
        }
    }

    /// <summary>
    /// 待機時間更新後処理
    /// </summary>
    private void OnChangedStandByTime(float prev, float current)
    {
        // UnityEngine.Debug.Log("OnChangedStandByTime: prev=" + prev + " → current=" + current);
        // ゲームプレイ開始
        if (current == 0)
        {
            PlayStart();
        }
    }

    /// <summary>
    /// ゲーム時間制限更新後処理
    /// </summary>
    private void OnChangedGameTime(float prev, float current)
    {
        // UnityEngine.Debug.Log("OnChangedGameTime: prev=" + prev + " → current=" + current);
        // 表示更新
        if (prev != current)
        {
            if (Math.Sign(current) > -1) ShowTimeCount();
        }
        // プレイ終了
        if (current == 0)
        {
            if (IsServer) this.isGame.Value = false;
        }
    }

    /// <summary>
    /// 初期化
    /// </summary>
    private void InitGame()
    {
        UnityEngine.Debug.Log("Game Status: Ready?");

        if (IsServer)
        {
            ServerManager.Instance.SetupManager(this);
        }
        else if(IsClient)
        {
            ClientManager.Instance.SetupManager(this);
        }

        this.configureConnection.ShowGameScene();
    }
    
    /// <summary>
    /// ゲームプレイ開始UTJ.NetcodeGameObjectSample
    /// </summary>
    private void PlayStart()
    {
        UnityEngine.Debug.Log("Game Status: Go Play!");

        ServerManager.Instance.SetActiveCoin(true);

        if(!IsServer) return;

        this.isGame.Value = true;
    }

    /// <summary>
    /// 開示時間カウントダウン
    /// </summary>
    private void CountDownReadyTime()
    {
        if (!IsServer) return;
        if (this.standByTime.Value == 0) return;

        this.standByTime.Value = this.standByTime.Value - Time.deltaTime;
        if(this.standByTime.Value < 0)
        {
            this.standByTime.Value = 0;
        }
    }

    /// <summary>
    /// ゲームタイムカウントダウン
    /// </summary>
    private void CountDownGameTime()
    {
        if (!IsServer) return;
        if (this.gameTime.Value == 0) return;

        this.gameTime.Value = this.gameTime.Value - Time.deltaTime;
        if(this.gameTime.Value < 0)
        {
            this.gameTime.Value = 0;
        }
    }

    /// <summary>
    /// 制限時間表示
    /// </summary>
    private void ShowTimeCount()
    {
        this.configureConnection.SetShowTimeCount(this.gameTime.Value);
    }

    /// <summary>
    /// リザルト画面に表示する結果情報更新
    /// </summary>
    public void SetResultPlayInfo(string info)
    {
        if (!IsServer) return;

        this.resultPlayInfo.Value = info;
    }

    /// <summary>
    /// ゲーム終了処理
    /// </summary>
    private void EndPlayGame()
    {
        UnityEngine.Debug.Log("Game Status: Finish!!");

        // コイン取得不可状態
        ServerManager.Instance.SetActiveCoin(false);

        // 集計
        SetResultPlayInfo(ServerManager.Instance.GetResultAndEndPlay());

        StartCoroutine(CountDownResult());
    }

    /// <summary>
    /// 5秒後にリザルト画面に遷移
    /// </summary>
    private IEnumerator CountDownResult()
    {
        yield return new WaitForSeconds(5.0f);

        this.configureConnection.ShowResultScene(this.resultPlayInfo.Value.Value);
    }
}
