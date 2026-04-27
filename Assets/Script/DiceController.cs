using UnityEngine;
using System.Collections;

public class DiceController : MonoBehaviour
{
    public Rigidbody rb;
    public float tossForce = 5f;
    public Vector3 spawnOffset = new Vector3(0, 5, 0);
    public PlayerMovement player;

    // 建立一個結構來對應「方向」與「點數」
    [System.Serializable]
    public struct DiceFace
    {
        public int value;
        public Vector3 localDirection; // 這是關鍵：該點數在模型上的箭頭方向
    }

    public DiceFace[] diceFaces = new DiceFace[6];

    private Vector3 initialPosition;
    private bool isRolling = false;

    void Awake()
    {
        initialPosition = transform.position;
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    public void ClickRollButton()
    {
        if (isRolling) return;
        StartCoroutine(PhysicsRollRoutine());
    }

    IEnumerator PhysicsRollRoutine()
    {
        isRolling = true;

        // 1. 投擲準備
        rb.isKinematic = true;
        transform.position = initialPosition + spawnOffset;
        transform.rotation = Random.rotation;

        yield return new WaitForSeconds(0.1f);

        // 2. 物理投擲
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(Vector3.up * tossForce, ForceMode.Impulse);
        rb.AddTorque(new Vector3(Random.Range(500, 1000), Random.Range(500, 1000), Random.Range(500, 1000)));

        // 3. 等待停止
        yield return new WaitForSeconds(1f);
        while (rb.linearVelocity.magnitude > 0.05f || rb.angularVelocity.magnitude > 0.05f)
        {
            yield return new WaitForSeconds(0.2f);
        }

        // 4. 【重點】偵測點數 (不靠預設數字，靠物理結果)
        int finalResult = GetDetectedValue();

        // 5. 【平滑修正】只把骰子「放平」，不改變它的點數
        yield return StartCoroutine(SettleDice());

        isRolling = false;
        Debug.Log($"偵測完成！眼睛看到的點數是：{finalResult}");
        player.StartMove(finalResult);
    }

    int GetDetectedValue()
    {
        float maxDot = -2f;
        int result = 1;

        foreach (var face in diceFaces)
        {
            // 將本地向量轉為世界向量
            Vector3 worldDir = transform.TransformDirection(face.localDirection);
            // 計算與世界「正上方」的對齊度
            float dot = Vector3.Dot(worldDir, Vector3.up);

            if (dot > maxDot)
            {
                maxDot = dot;
                result = face.value;
            }
        }
        return result;
    }

    IEnumerator SettleDice()
    {
        rb.isKinematic = true;
        Quaternion startRot = transform.rotation;

        // 計算「最接近的 90 度倍數」，但保留原本的轉向
        Vector3 currentEuler = transform.eulerAngles;
        Vector3 targetEuler = new Vector3(
            Mathf.Round(currentEuler.x / 90) * 90,
            currentEuler.y, // 保留 Y 軸旋轉，才不會一頓一頓的
            Mathf.Round(currentEuler.z / 90) * 90
        );

        Quaternion targetRot = Quaternion.Euler(targetEuler);
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 3f;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        transform.rotation = targetRot;
    }
}