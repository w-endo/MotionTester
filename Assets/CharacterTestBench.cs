using UnityEngine;
using System.Collections.Generic;

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

    [Header("--- キー設定 ---")]
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode idleSwitchKey = KeyCode.Space;

    [Header("--- 単発アクションのキー設定 ---")]
    [Tooltip("Action1～Action10を発動させるキーを指定してください")]
    public List<KeyCode> actionKeys = new List<KeyCode>() {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
        KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
    };

    [Header("--- 特殊待機のリスト ---")]
    [Tooltip("スペースキーで切り替える待機モーションの名前（Bool型）")]
    public List<string> specialIdleParameters = new List<string>();

    // 内部変数
    private CharacterController characterController;
    private Animator anim;
    private int currentIdleIndex = -1;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();

        if (anim == null) Debug.LogError("エラー：子オブジェクトにAnimatorが見つかりません！");
    }

    void Update()
    {
        if (anim == null) return;

        // 1. 今、アクション中かどうかをチェックする
        // Animatorのステートに「Action」というタグがついているか確認
        bool isActionState = anim.GetCurrentAnimatorStateInfo(0).IsTag("Action");

        // 2. アクション中なら移動処理をスキップ（リターン）して、その場で止まる
        // ※ただし、重力処理だけは継続しないと空中で止まってしまうので注意
        if (isActionState)
        {
            // 重力のみ適用して、関数の残りの処理はやらない
            characterController.SimpleMove(Vector3.zero);
            return;
        }

        // 3. 移動と入力の処理（アクション中でなければここが動く）
        HandleMovement();
        HandleActions();
        HandleIdleSwitch();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0, vertical);
        if (direction.magnitude > 1.0f) direction.Normalize();

        bool isRunning = Input.GetKey(runKey);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        if (direction.magnitude >= 0.1f)
        {
            // 向きを変える
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            // 移動する
            characterController.SimpleMove(direction * currentSpeed);
        }
        else
        {
            // 止まっていても重力はかける
            characterController.SimpleMove(Vector3.zero);
        }

        // アニメーション速度更新
        float animSpeedValue = 0;
        if (direction.magnitude >= 0.1f)
        {
            animSpeedValue = isRunning ? 2.0f : 1.0f;
        }
        anim.SetFloat("Speed", animSpeedValue, 0.1f, Time.deltaTime);
    }

    void HandleActions()
    {
        // 設定されたキーの数だけチェックする
        for (int i = 0; i < actionKeys.Count; i++)
        {
            // もしリストのキーが押されたら
            if (Input.GetKeyDown(actionKeys[i]))
            {
                // 固定の名前 "Action1", "Action2"... を作成
                string triggerName = "Action" + (i + 1);

                // トリガーを発動
                anim.SetTrigger(triggerName);
                Debug.Log("アクション実行: " + triggerName);
            }
        }
    }

    void HandleIdleSwitch()
    {
        if (specialIdleParameters.Count == 0) return;

        if (Input.GetKeyDown(idleSwitchKey))
        {
            if (currentIdleIndex != -1)
                anim.SetBool(specialIdleParameters[currentIdleIndex], false);

            currentIdleIndex++;
            if (currentIdleIndex >= specialIdleParameters.Count) currentIdleIndex = -1;
            else anim.SetBool(specialIdleParameters[currentIdleIndex], true);
        }
    }
}