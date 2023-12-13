using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManagerSystem : MonoBehaviour
{
    const int MAX_COIN = 20;
    const int MAX_TIME = 30;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private SceneSystem sceneSystem;
    [SerializeField] private Text timeText;

    private PlayerSystem player;
    private List<Coin> coinList = new List<Coin>();

    private float gameTime = 0;

    private bool isGame = false;

    void Awake()
    {
        InitGame();
    }

    void Start()
    {
        var go = Instantiate(playerPrefab);
        player = go.GetComponent<PlayerSystem>();
        CreateCoin();
    }

    void Update()
    {
        if (!this.isGame) return;

        CountDownTime();
        ShowTimeCount();

        if(this.gameTime > 0) return;

        EndPlay();
    }

    /// <summary>
    /// コインをランダムに配置
    /// </summary>
    void CreateCoin()
    {
        for (int i = 0 ; i < MAX_COIN ; i++)
        {
            var coin = Instantiate(coinPrefab.gameObject);
            coin.transform.position = new Vector3(Random.Range(-30,30), 0.5f, Random.Range(-30,30));
            this.coinList.Add(coin.GetComponent<Coin>());
        }
    }

    /// <summary>
    /// 初期化
    /// </summary>
    void InitGame()
    {
        this.gameTime = MAX_TIME;
        this.isGame = true;
    }

    /// <summary>
    /// タイムカウント
    /// </summary>
    void CountDownTime()
    {
        this.gameTime -= Time.deltaTime;
        if(this.gameTime < 0)
        {
            this.gameTime = 0;
        }
    }

    /// <summary>
    /// 制限時間表示
    /// </summary>
    void ShowTimeCount()
    {
        this.timeText.text = this.gameTime.ToString("0.00");
    }

    /// <summary>
    /// ゲーム終了処理
    /// </summary>
    void EndPlay()
    {
        this.isGame = false;
        this.coinList.ForEach(delegate(Coin c)
        {
            if (c == null) return;
            c.notGet = true;
        });
        StartCoroutine(CountDownResult());
    }

    /// <summary>
    /// 5秒後にリザルト画面に遷移
    /// </summary>
    IEnumerator CountDownResult()
    {
        // プレイ結果保存
        PlayerPrefs.SetInt("GaemResult", player.Point);
        PlayerPrefs.Save();

        // 画面遷移
        yield return new WaitForSeconds(5.0f);
        sceneSystem.GoResultScene();
    }
}
