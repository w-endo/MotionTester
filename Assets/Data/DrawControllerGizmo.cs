using UnityEngine;

public class DrawControllerGizmo : MonoBehaviour
{
    // 選択していなくても線を引く設定
    private void OnDrawGizmos()
    {
        CharacterController controller = GetComponent<CharacterController>();
        if (controller == null) return;

        // 線の色を緑色に設定（好きな色に変えられます）
        Gizmos.color = Color.green;

        // カプセルの中心位置を計算
        Vector3 pos = transform.position + controller.center;

        // 簡易的なワイヤーフレームを表示（簡易的な位置確認用）
        // より正確なカプセル形状が必要な場合は以下
        float height = controller.height;
        float radius = controller.radius;

        // カプセルを描画する
        Gizmos.DrawWireSphere(pos + Vector3.up * (height / 2 - radius), radius);
        Gizmos.DrawWireSphere(pos - Vector3.up * (height / 2 - radius), radius);
    }
}