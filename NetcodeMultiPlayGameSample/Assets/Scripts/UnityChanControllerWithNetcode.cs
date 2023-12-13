using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UTJ.NetcodeGameObjectSample;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]

public class UnityChanControllerWithNetcode : NetworkBehaviour
{
    public float animSpeed = 1.5f;
    public float lookSmoother = 3.0f;
    public bool useCurves = true;
    public float useCurvesHeight = 0.5f;

    public float forwardSpeed = 7.0f;
    public float backwardSpeed = 2.0f;
    public float rotateSpeed = 2.0f;

    private CapsuleCollider col;
    private Rigidbody rb;
    private Vector3 velocity;
    private float orgColHight;
    private Vector3 orgVectColCenter;
    private Animator anim;
    private AnimatorStateInfo currentBaseState;

    static int idleState = Animator.StringToHash("Base Layer.Idle");
    static int locoState = Animator.StringToHash("Base Layer.Locomotion");
    static int jumpState = Animator.StringToHash("Base Layer.Jump");
    static int restState = Animator.StringToHash("Base Layer.Rest");

    [SerializeField] private TextMesh namePlate;
    [SerializeField] private Camera camera;

    #region NETWORKED_VAR
    // 獲得コイン
    private NetworkVariable<int> point = new NetworkVariable<int>(0);
    // プレイヤー名
    private NetworkVariable<Unity.Collections.FixedString64Bytes> playerName =
        new NetworkVariable<Unity.Collections.FixedString64Bytes>();
    // 左右移動の値
    private NetworkVariable<float> horizontalSpeed = new NetworkVariable<float>(0.0f);
    // 前後移動の値
    private NetworkVariable<float> verticalSpeed = new NetworkVariable<float>(0.0f);
    #endregion NETWORKED_VAR

    void Awake()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();

        this.camera.gameObject.SetActive(false);

#if UNITY_SERVER
        NetworkUtility.RemoveAllStandaloneComponents(this.gameObject);
#elif ENABLE_AUTO_CLIENT
        if (NetworkUtility.IsBatchModeRun)
        {
            NetworkUtility.RemoveAllStandaloneComponents(this.gameObject);
        }
#endif
    }

    /// <summary>
    /// スポーン後初期化
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // プレイヤー以外のキャラのカメラをオフにする
            this.camera.gameObject.SetActive(false);
            // プレイヤー以外のキャラはネームを逆にする
            this.namePlate.gameObject.transform.Rotate(Vector3.up * 180, Space.World);
            return;
        }

        this.camera.gameObject.SetActive(true);
        SetPlayerNameServerRpc(ConfigureConnectionBehaviour.playerName);
    }

    void Start()
    {
        orgColHight = col.height;
        orgVectColCenter = col.center;
    }

    void Update()
    {
        var playerName = GetPlayerName();
        if (namePlate.text != playerName)
            namePlate.text = playerName;
    }

    void FixedUpdate()
    {
        anim.SetFloat("Speed", verticalSpeed.Value);
        anim.SetFloat("Direction", horizontalSpeed.Value);
        anim.speed = animSpeed;

        if (IsOwner)
        {
            UpdateAsOwner();
        }
    }

    void resetCollider()
    {
        col.height = orgColHight;
        col.center = orgVectColCenter;
    }

    /// <summary>
    /// オーナーとしての処理
    /// </summary>
    private void UpdateAsOwner()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        SetHorizontalSpeedServerRpc(h);
        SetVerticalSpeedServerRpc(v);

        currentBaseState = anim.GetCurrentAnimatorStateInfo(0);

        velocity = new Vector3(0, 0, v);
        velocity = transform.TransformDirection(velocity);

        if (v > 0.1)
        {
            velocity *= forwardSpeed;
        }
        else if (v < -0.1)
        {
            velocity *= backwardSpeed;
        }

        transform.localPosition += velocity * Time.fixedDeltaTime;

        transform.Rotate(0, h * rotateSpeed, 0);

        if (currentBaseState.fullPathHash == locoState)
        {
            if (useCurves)
            {
                resetCollider();
            }
        }
        else if (currentBaseState.fullPathHash == idleState)
        {
            if (useCurves)
            {
                resetCollider();
            }
        }
        else if (currentBaseState.fullPathHash == restState)
        {
            if (!anim.IsInTransition(0))
            {
                anim.SetBool("Rest", false);
            }
        }

        if (transform.position.y < -10.0f)
        {
            var randomPosition = new Vector3(Random.Range(-7, 7), 5.0f, Random.Range(-7, 7));
            transform.position = randomPosition;
        }
    }

    /// <summary>
    /// プレイヤー名を更新依頼
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    private void SetPlayerNameServerRpc(string setName="プレイヤー")
    {
        this.playerName.Value = setName;
    }

    /// <summary>
    /// 移動速度を更新依頼
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    private void SetHorizontalSpeedServerRpc(float speed)
    {
        this.horizontalSpeed.Value = speed;
    }

    /// <summary>
    /// 移動速度を更新依頼
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    private void SetVerticalSpeedServerRpc(float speed)
    {
        this.verticalSpeed.Value = speed;
    }

    /// <summary>
    /// OnCollisionEnter経由でコインからポイントを取得
    /// </summary>
    public void AddPoint(int point)
    {
        if  (!IsOwner) return;

        SetPointServerRpc(point);
    }

    /// <summary>
    /// ポイント更新依頼
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    private void SetPointServerRpc(int point)
    {
        this.point.Value += point;
    }

    /// <summary>
    /// 獲得ポイントを返す
    /// </summary>
    public int GetPoint()
    {
        return this.point.Value;
    }

    /// <summary>
    /// プレイヤー名を返す
    /// </summary>
    public string GetPlayerName()
    {
        return this.playerName.Value.Value;
    }
}
