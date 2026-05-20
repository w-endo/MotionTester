using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ─────────────────────────────────────────────
// アクション1枠分の設定（クリップ＋エフェクト）
// ─────────────────────────────────────────────
[System.Serializable]
public class ActionEntry
{
    [Tooltip("再生するアニメーションクリップ")]
    public AnimationClip clip;

    [Tooltip("生成するエフェクトのプレファブ（空欄可）")]
    public GameObject effectPrefab;

    [Tooltip("アクション開始から何秒後にエフェクトを出すか")]
    public float effectDelay = 0.0f;

    [Tooltip("エフェクトを出す骨（Transform）。空欄の場合はキャラクター自身の位置")]
    public Transform effectBone;

    [Tooltip("ONにすると、エフェクトが骨に追従し続ける（剣の軌跡など）\nOFFにすると、指定時間後の骨の位置に一度だけ生成する")]
    public bool followBone = false;
}

// ─────────────────────────────────────────────
// メインクラス
// ─────────────────────────────────────────────
[RequireComponent(typeof(CharacterController))]
public class CharacterTestBench : MonoBehaviour
{
    [Header("--- 移動設定 ---")]

    [Tooltip("歩く速度")]
    public float walkSpeed = 3.0f;

    [Tooltip("走る速度（Shiftキー）")]
    public float runSpeed = 6.0f;

    [Tooltip("振り向く速さ")]
    public float turnSpeed = 360.0f;

    [Tooltip("ジャンプ力")]
    public float jumpForce = 5.0f;


    [Tooltip("重力加速度（通常は 9.81）")]
    public float gravity = 9.81f;

    [Header("--- モーション設定 ---")]
    public AnimationClip idle;
    public AnimationClip walk;
    public AnimationClip run;
    public AnimationClip jumpClip;
    public ActionEntry[] actions = new ActionEntry[10];
    public AnimationClip[] otherIdle;

    [Header("--- キー設定 ---")]
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode idleSwitchKey = KeyCode.Tab;
    public KeyCode jumpKey = KeyCode.LeftControl;

    public List<KeyCode> actionKeys = new List<KeyCode>() {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
        KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
    };

    // 内部変数
    private CharacterController characterController;
    private Animator anim;
    private int IdleIndex = 0;
    private AnimationClip[] idles;

    // 垂直方向の速度（ジャンプ・落下で使う）
    private float verticalVelocity = 0f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();

        if (anim == null) Debug.LogError("エラー：子オブジェクトにAnimatorが見つかりません！");

        idles = new[] { idle }.Concat(otherIdle).ToArray();
        SetAnimationClip();
    }

    void Update()
    {
        if (anim == null) return;

        bool isActionState = anim.GetCurrentAnimatorStateInfo(0).IsTag("Action");

        if (isActionState)
        {
            // アクション中：水平移動は止めるが重力・垂直速度は継続
            ApplyGravityOnly();
            return;
        }

        HandleMovement();
        HandleJump();
        HandleActions();
        HandleIdleSwitch();
    }

    void SetAnimationClip()
    {
        AnimatorOverrideController overrideController = new AnimatorOverrideController(anim.runtimeAnimatorController);

        overrideController["DummyIdle"] = idles[IdleIndex];
        overrideController["DummyWalk"] = walk;
        overrideController["DummyRun"] = run;
        overrideController["DummyJump"] = jumpClip;

        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i] != null && actions[i].clip != null)
            {
                overrideController["DummyAction " + i] = actions[i].clip;
            }
        }

        anim.runtimeAnimatorController = overrideController;
    }

    // ─────────────────────────────────────────────
    // 重力のみ適用（アクション中など、水平移動を止めたい場面で使う）
    // ─────────────────────────────────────────────
    void ApplyGravityOnly()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f; // 接地状態を安定させる最低限の下向き速度
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        characterController.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    // 移動処理（水平方向）
    // ─────────────────────────────────────────────
    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0, vertical);
        if (direction.magnitude > 1.0f) direction.Normalize();

        bool isRunning = Input.GetKey(runKey);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        // 重力・垂直速度の更新
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        if (direction.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // 水平移動 ＋ 垂直速度を合わせてMoveで適用
        Vector3 horizontalVelocity = direction * (direction.magnitude >= 0.1f ? currentSpeed : 0f);
        Vector3 moveVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
        characterController.Move(moveVelocity * Time.deltaTime);

        // アニメーション Speed パラメータ
        float animSpeedValue = 0f;
        if (direction.magnitude >= 0.1f)
        {
            animSpeedValue = isRunning ? 2.0f : 1.0f;
        }
        anim.SetFloat("Speed", animSpeedValue, 0.1f, Time.deltaTime);

        // 接地状態をアニメーターに伝える
        anim.SetBool("IsGrounded", characterController.isGrounded);
    }

    // ─────────────────────────────────────────────
    // ジャンプ処理
    // ─────────────────────────────────────────────
    void HandleJump()
    {
        // 接地中にジャンプキーを押したときだけ発動
        if (characterController.isGrounded && Input.GetKeyDown(jumpKey))
        {
            verticalVelocity = jumpForce;
            anim.SetTrigger("Jump");
        }
    }

    // ─────────────────────────────────────────────
    // アクション処理
    // ─────────────────────────────────────────────
    void HandleActions()
    {
        for (int i = 0; i < actionKeys.Count; i++)
        {
            if (!Input.GetKeyDown(actionKeys[i])) continue;

            ActionEntry entry = (i < actions.Length) ? actions[i] : null;

            if (entry == null || entry.clip == null)
            {
                Debug.Log("アクションが設定されていません: Action" + (i + 1));
                continue;
            }

            string triggerName = "Action" + i;
            anim.SetTrigger(triggerName);
            Debug.Log("アクション実行: " + triggerName);

            if (entry.effectPrefab != null)
            {
                StartCoroutine(SpawnEffectAfterDelay(entry));
            }
        }
    }

    IEnumerator SpawnEffectAfterDelay(ActionEntry entry)
    {
        if (entry.effectDelay > 0f)
        {
            yield return new WaitForSeconds(entry.effectDelay);
        }

        Vector3 spawnPosition;
        Quaternion spawnRotation;
        Transform parent = null;

        if (entry.effectBone != null)
        {
            spawnPosition = entry.effectBone.position;
            spawnRotation = entry.effectBone.rotation;

            // 追従モードのときは骨を親にセット
            if (entry.followBone) parent = entry.effectBone;
        }
        else
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        GameObject effect = Instantiate(entry.effectPrefab, spawnPosition, spawnRotation);

        // 追従モード：骨の子にする（位置・回転が自動的に追従する）
        if (parent != null)
        {
            effect.transform.SetParent(parent, worldPositionStays: true);
        }
    }

    // ─────────────────────────────────────────────
    // 待機モーション切り替え
    // ─────────────────────────────────────────────
    void HandleIdleSwitch()
    {
        if (Input.GetKeyDown(idleSwitchKey))
        {
            IdleIndex++;
            if (IdleIndex >= idles.Length) IdleIndex = 0;
            SetAnimationClip();
        }
    }
}