using UnityEngine;
using System.Collections.Generic;

// 必要なコンポーネントを自動で追加します
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

    [Header("--- 特殊待機のリスト ---")]
    [Tooltip("スペースキーで切り替える待機モーションの名前（Bool型）")]
    public List<string> specialIdleParameters = new List<string>();

    [Header("--- 単発アクションの設定 (1~0キー) ---")]
    [Tooltip("1〜0のキーに対応させるAnimatorのTrigger名（最大10個）")]
    public string[] actionTriggers = new string[10];

    // 内部で使う変数
    private CharacterController characterController;
    private Animator anim;
    private int currentIdleIndex = -1; // -1は通常待機

    void Start()
    {
        // 自分のについているCharacterControllerを取得
        characterController = GetComponent<CharacterController>();

        // 【重要】モデルは子オブジェクトにあるので、子供からAnimatorを探す
        anim = GetComponentInChildren<Animator>();

        if (anim == null)
        {
            Debug.LogError("エラー：子オブジェクトにAnimatorが見つかりません！モデルを配置してください。");
        }
    }

    void Update()
    {
        // Animatorが見つからなかったら動かないようにする安全装置
        if (anim == null) return;

        HandleMovement();      // 移動の処理
        HandleActions();       // 単発アクションの処理
        HandleIdleSwitch();    // 待機モーション切り替えの処理
    }

    // 移動の処理（CharacterController使用版）
    void HandleMovement()
    {
        // キーボード入力の取得
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 入力から移動する方向を作る
        Vector3 direction = new Vector3(horizontal, 0, vertical);

        // 入力の強さを制限（斜め移動対策）
        if (direction.magnitude > 1.0f) direction.Normalize();

        // 走っているかどうか
        bool isRunning = Input.GetKey(runKey);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        // 入力がある場合のみ、向きを変えて移動する
        if (direction.magnitude >= 0.1f)
        {
            // 1. カメラの向きではなく「画面上の見た目の方向」に動かすための計算
            // （カメラが回転しない前提の簡易版です。TPS視点にする場合はカメラのTransformが必要です）

            // 2. キャラクターの向きを徐々に変える
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );

            // 3. CharacterControllerで移動させる
            // SimpleMoveは「重力」を自動で計算してくれる便利な命令です
            // ※SimpleMoveには Time.deltaTime を掛けなくてOKです（内部でやってくれます）
            Vector3 velocity = direction * currentSpeed;
            characterController.SimpleMove(velocity);
        }
        else
        {
            // 動いていない時も重力をかけるために(0,0,0)で実行し続ける
            characterController.SimpleMove(Vector3.zero);
        }

        // --- アニメーションの更新 ---

        // 移動しているスピードを計算（実際の移動速度ではなく入力値ベースで判定）
        float animSpeedValue = 0;
        if (direction.magnitude >= 0.1f)
        {
            animSpeedValue = isRunning ? 2.0f : 1.0f;
        }

        // Animatorの「Speed」パラメータを滑らかに変更
        anim.SetFloat("Speed", animSpeedValue, 0.1f, Time.deltaTime);
    }

    // 単発アクション（1〜0キー）の処理
    void HandleActions()
    {
        // 1〜0のキーコード配列
        KeyCode[] keyCodes = {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
        };

        for (int i = 0; i < keyCodes.Length; i++)
        {
            if (Input.GetKeyDown(keyCodes[i]))
            {
                // 設定された名前が有効ならTriggerを起動
                if (i < actionTriggers.Length && !string.IsNullOrEmpty(actionTriggers[i]))
                {
                    anim.SetTrigger(actionTriggers[i]);
                    Debug.Log($"アクション実行 [Key {i + 1}]: {actionTriggers[i]}");
                }
            }
        }
    }

    // 待機モーション切り替えの処理
    void HandleIdleSwitch()
    {
        if (specialIdleParameters.Count == 0) return;

        if (Input.GetKeyDown(idleSwitchKey))
        {
            // 現在の特殊待機をOFFにする
            if (currentIdleIndex != -1)
            {
                anim.SetBool(specialIdleParameters[currentIdleIndex], false);
            }

            // 次のインデックスへ
            currentIdleIndex++;

            // リストの最後まで行ったらリセット(-1)
            if (currentIdleIndex >= specialIdleParameters.Count)
            {
                currentIdleIndex = -1;
                Debug.Log("待機モード: 通常");
            }
            else
            {
                // 新しい特殊待機をONにする
                anim.SetBool(specialIdleParameters[currentIdleIndex], true);
                Debug.Log($"待機モード: {specialIdleParameters[currentIdleIndex]}");
            }
        }
    }
}