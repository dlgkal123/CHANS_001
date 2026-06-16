using UnityEngine;

/// <summary>
/// 좀비를 Y축 아래 방향으로 이동시키는 스크립트
/// 포트폴리오용 - 세로 슈팅 게임 적 이동 로직
/// </summary>
public class ZombieMoveDown : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] 
    [Tooltip("초당 이동 속도 (Unity Units/sec)")]
    private float moveSpeed = 2.0f; // 기본 속도 2 유닛/초
    
    void Update()
    {
        // Time.deltaTime을 곱해서 프레임 독립적인 이동 구현 (60fps, 144fps 상관없이 동일한 속도)
        // Vector3.down은 (0, -1, 0)과 동일 - Y축 아래 방향
        transform.position += Vector3.down * moveSpeed * Time.deltaTime;
    }
}
