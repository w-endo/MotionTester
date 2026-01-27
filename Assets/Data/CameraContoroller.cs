using UnityEngine;

public class CameraContoroller : MonoBehaviour
{
    public GameObject player; // プレイヤーオブジェクトへの参照
    public enum CameraType
    {
        Fixed,
        Follow
    }

    [Header("--- タイプ（固定/追従） ---")]
    public CameraType cameraType = CameraType.Fixed;

    [Header("--- 追従するときの速度 ---")]
    [Range(0.001f, 1f)]
    public float followSpeed = 0.01f; // エディタで調整可能なLerp値

    private Vector3 offset; // プレイヤーとカメラの位置関係を保持する変数

    void Start()
    {
        offset = transform.position - player.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (cameraType == CameraType.Follow)
        {
            Vector3 pos = player.transform.position + offset;
            transform.position = Vector3.Lerp(transform.position, pos, followSpeed);
        }
    }
}
