using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using System.Collections;
[ExecuteAlways] // 에디터와 런타임 모두에서 실행
[RequireComponent(typeof(RectTransform), typeof(BoxCollider), typeof(Rigidbody))] // 필수 컴포넌트
public class CollisionDetector : MonoBehaviour
{
    // 설정 관련 변수
    [FoldoutGroup("Settings")] public GameObject m_ImageOn; // 충돌 시 표시할 이미지
    [FoldoutGroup("Settings")] public GameObject m_ImageClick; // 클릭 시 표시할 이미지
    [FoldoutGroup("Settings")] public Color m_ButtonNormalColor = Color.white; // 버튼 기본 색상
    [FoldoutGroup("Settings")] public Color m_ButtonHighlightColor = Color.cyan; // 버튼 하이라이트 색상
    [FoldoutGroup("Settings")] public GameObject m_ClickParticle; // 클릭 시 재생할 파티클

    // 이벤트 관련 변수
    [FoldoutGroup("Events"), HideInInspector] public bool m_IsColliding; // 현재 충돌 상태
    [FoldoutGroup("Events"), ToggleLeft] public bool m_UseOneClick = true; // 한 번 클릭만 허용 여부
    [FoldoutGroup("Events"), ToggleLeft] public bool m_UseImageEffects; // 이미지 효과 사용 여부
    [FoldoutGroup("Events"), ToggleLeft] public bool m_UseButtonColor; // 버튼 색상 변경 사용 여부


    [FoldoutGroup("Collider")][SerializeField] private float zDepth = 10f; // BoxCollider의 Z축 깊이 (기본값: 1)
                                                                           // 컴포넌트 참조
    private Button m_Button; // 충돌 대상 버튼
    private ParticleSystem m_ParticleSystem; // 파티클 시스템
    private PointsPos m_PointsPos; // 손 위치 정보
    private RectTransform rectTransform;
    private BoxCollider boxCollider;



    // 가장 빠른 초기화 시점
    private void Awake()
    {
        if (m_ImageOn != null) m_ImageOn.SetActive(false);
        if (m_ImageClick != null) m_ImageClick.SetActive(false);
        if (m_ClickParticle != null)
        {
            m_ClickParticle.transform.localPosition = new Vector3(0, 0, -1);
            m_ClickParticle.SetActive(false);
            m_ParticleSystem = m_ClickParticle.GetComponent<ParticleSystem>();
        }
        m_Button = GetComponent<Button>();
    }
    // Start에서 초기화 (참조 연결)
    private void Start()
    {
        // 컴포넌트 초기화
        rectTransform = GetComponent<RectTransform>();
        boxCollider = GetComponent<BoxCollider>();
        UpdateColliderSize();

        m_PointsPos = transform.parent.GetComponent<PointsPos>();
    }

    void UpdateColliderSize()
    {
        if (rectTransform == null || boxCollider == null) return;

        Vector2 rectSize = rectTransform.sizeDelta;
        boxCollider.size = new Vector3(rectSize.x, rectSize.y, zDepth);
    }
    // 오브젝트가 활성화될 때 호출됨 (외부에서 강제로 활성화된 경우 방지)
    private void OnEnable()
    {
        if (!m_IsColliding && m_ImageOn != null && m_UseImageEffects)
        {
            m_ImageOn.SetActive(false);
        }
    }


    // 충돌 시작 시
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsValidButtonCollision(collision)) return;

        m_IsColliding = false;
        SetImageOnState(true, collision);
    }

    // 충돌 유지 중
    private void OnCollisionStay(Collision collision)
    {
        if (m_IsColliding || !IsValidButtonCollision(collision)) return;

        m_Button = collision.gameObject.GetComponent<Button>();
        if (m_Button == null) return;

        if (m_UseButtonColor)
        {
            UpdateButtonColor(m_ButtonHighlightColor);
        }

        if (IsHandClickConditionMet())
        {
            m_Button.onClick.Invoke();

            if (m_ClickParticle != null)
                StartCoroutine(ParticleLifetimeRoutine());

            if (m_UseImageEffects && m_ImageClick != null)
                m_ImageClick.SetActive(true);

            var otherDetector = collision.gameObject.GetComponent<CollisionDetector>();
            if (otherDetector != null && otherDetector.m_UseImageEffects && otherDetector.m_ImageClick != null)
                otherDetector.m_ImageClick.SetActive(true);

            m_IsColliding = true;
        }
    }

    // 충돌 종료 시
    private void OnCollisionExit(Collision collision)
    {
        if (!IsValidButtonCollision(collision)) return;

        m_Button = collision.gameObject.GetComponent<Button>();
        if (m_Button != null && m_UseButtonColor)
        {
            UpdateButtonColor(m_ButtonNormalColor);
        }

        SetImageOnState(false, collision);

        if (m_ImageClick != null)
            m_ImageClick.SetActive(false);

        var otherDetector = collision.gameObject.GetComponent<CollisionDetector>();
        if (otherDetector != null && otherDetector.m_ImageClick != null)
            otherDetector.m_ImageClick.SetActive(false);

        if (m_ClickParticle != null)
            m_ClickParticle.SetActive(false);

        m_IsColliding = false;
    }

    // 버튼 태그 검사
    private bool IsValidButtonCollision(Collision collision)
    {
        return collision.gameObject.CompareTag("Btn");
    }

    // 버튼 색상 업데이트
    private void UpdateButtonColor(Color color)
    {
        var colors = m_Button.colors;
        colors.normalColor = color;
        m_Button.colors = colors;
    }

    // 손의 클릭 조건
    private bool IsHandClickConditionMet()
    {
        if (m_PointsPos == null) return false;
        return (transform.name == "LPoint" && m_PointsPos.m_LHand_Fist) ||
               (transform.name == "RPoint" && m_PointsPos.m_RHand_Fist);
    }

    // 이미지 온 상태 설정
    private void SetImageOnState(bool state, Collision collision)
    {
        if (m_UseImageEffects && m_ImageOn != null)
            m_ImageOn.SetActive(state);

        var otherDetector = collision.gameObject.GetComponent<CollisionDetector>();
        if (otherDetector != null && otherDetector.m_UseImageEffects && otherDetector.m_ImageOn != null)
            otherDetector.m_ImageOn.SetActive(state);
    }

    // 파티클 재생 및 이미지 비활성화 루틴
    private IEnumerator ParticleLifetimeRoutine()
    {
        if (m_ParticleSystem == null) yield break;

        m_ClickParticle.SetActive(true);
        m_ParticleSystem.Play();

        while (m_ParticleSystem.IsAlive(true))
        {
            yield return null;
        }

        if (m_ImageClick != null)
            m_ImageClick.SetActive(false);

        if (m_Button != null)
        {
            var otherDetector = m_Button.gameObject.GetComponent<CollisionDetector>();
            if (otherDetector != null && otherDetector.m_ImageClick != null)
                otherDetector.m_ImageClick.SetActive(false);
        }

        m_ClickParticle.SetActive(false);

        if (!m_UseOneClick)
            m_IsColliding = false;
    }
}
