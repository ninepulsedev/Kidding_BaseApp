using Sirenix.OdinInspector;
using UnityEngine;

public class PointsPos : MonoBehaviour
{
    // 설정 관련 인스펙터 변수들
    [FoldoutGroup("Setting")][OnValueChanged("ToggleHolisticLandmarks")][SerializeField] private bool showHolisticLandmarks = false;
    [FoldoutGroup("Setting")][SerializeField] private GameObject holisticLandmarks;

    [FoldoutGroup("Setting")] public GameObject m_BodyPointAnnotation;
    [FoldoutGroup("Setting")] public GameObject m_LHandPointAnnotation;
    [FoldoutGroup("Setting")] public GameObject m_RHandPointAnnotation;

    [FoldoutGroup("Setting")][SerializeField] private GameObject m_LeftPoint;
    [FoldoutGroup("Setting")][SerializeField] private GameObject m_RightPoint;
    //[FoldoutGroup("Setting")][Range(1, 50)][SerializeField] private int m_SmoothSpeed = 10;

    // 손 위치 체크 관련 변수
    [FoldoutGroup("PosCheck")]
    [HorizontalGroup("PosCheck/Group00"), ToggleLeft] public bool m_LHand_Fist;
    [HorizontalGroup("PosCheck/Group00"), ToggleLeft] public bool m_RHand_Fist;
    [HorizontalGroup("PosCheck/Group01"), ToggleLeft] public bool m_LHand_Paper;
    [HorizontalGroup("PosCheck/Group01"), ToggleLeft] public bool m_RHand_Paper;
    [HorizontalGroup("PosCheck/Group02"), ToggleLeft] public bool m_LHand_Scissors;
    [HorizontalGroup("PosCheck/Group02"), ToggleLeft] public bool m_RHand_Scissors;
    [Space]
    [HorizontalGroup("PosCheck/Group03"), ToggleLeft] public bool m_LHand_Up_Out;
    [HorizontalGroup("PosCheck/Group03"), ToggleLeft] public bool m_RHand_Up_Out;
    [HorizontalGroup("PosCheck/Group04"), ToggleLeft] public bool m_LHand_Up_In;
    [HorizontalGroup("PosCheck/Group04"), ToggleLeft] public bool m_RHand_Up_In;
    [Space]
    [HorizontalGroup("PosCheck/Group05"), ToggleLeft] public bool m_LHand_Down_Out;
    [HorizontalGroup("PosCheck/Group05"), ToggleLeft] public bool m_RHand_Down_Out;
    [HorizontalGroup("PosCheck/Group06"), ToggleLeft] public bool m_LHand_Down_In;
    [HorizontalGroup("PosCheck/Group06"), ToggleLeft] public bool m_RHand_Down_In;
    [Space]
    [HorizontalGroup("PosCheck/Group07"), ToggleLeft] public bool m_LRHandCross_Up;
    [HorizontalGroup("PosCheck/Group07"), ToggleLeft] public bool m_LR_HandLeft_Up;
    [HorizontalGroup("PosCheck/Group07"), ToggleLeft] public bool m_LR_HandRight_Up;
    [Space]
    [HorizontalGroup("PosCheck/Group08"), ToggleLeft] public bool m_LRHandCross_Down;
    [HorizontalGroup("PosCheck/Group08"), ToggleLeft] public bool m_LR_HandLeft_Down;
    [HorizontalGroup("PosCheck/Group08"), ToggleLeft] public bool m_LR_HandRight_Down;

    // 캐싱된 Transform
    private Transform m_BodyPointAnnotationTransform;
    private Transform m_LHandPointAnnotationTransform;
    private Transform m_RHandPointAnnotationTransform;
    private Transform m_LeftPointTransform;
    private Transform m_RightPointTransform;
    private float m_SmoothFactor;
    int m_SmoothSpeed = 5;

    private void Start()
    {
        if (Application.isEditor)
        {
#if UNITY_EDITOR
            showHolisticLandmarks = true;
#endif
        }
        else
        {
            showHolisticLandmarks = false;
        }
        // Transform 캐싱
        m_BodyPointAnnotationTransform = m_BodyPointAnnotation.transform;
        m_LHandPointAnnotationTransform = m_LHandPointAnnotation.transform;
        m_RHandPointAnnotationTransform = m_RHandPointAnnotation.transform;
        m_LeftPointTransform = m_LeftPoint.transform;
        m_RightPointTransform = m_RightPoint.transform;
        m_SmoothFactor = m_SmoothSpeed * Time.deltaTime;

        if (WebCamTexture.devices.Length == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        holisticLandmarks.SetActive(true);
        Invoke(nameof(ToggleHolisticLandmarks), 0.1f);
    }

    private void Update()
    {
        UpdatePointPositions();
        Check_HandsFinger();
        Check_Hands_Up_Out();
        Check_Hands_Up_In();
        Check_LRHandsCross_Up();
        Check_Hands_Down_Out();
        Check_Hands_Down_In();
        Check_m_LR_HandLeft();
        Check_m_LR_HandRight();
    }

    private void UpdatePointPositions()
    {
        Vector3 targetLeftPos = m_LHandPointAnnotationTransform.parent.gameObject.activeSelf
            ? m_LHandPointAnnotationTransform.GetChild(9).position
            : m_BodyPointAnnotationTransform.GetChild(19).position;

        Vector3 targetRightPos = m_RHandPointAnnotationTransform.parent.gameObject.activeSelf
            ? m_RHandPointAnnotationTransform.GetChild(9).position
            : m_BodyPointAnnotationTransform.GetChild(20).position;

        m_LeftPointTransform.position = Vector3.Lerp(
            m_LeftPointTransform.position,
            targetLeftPos,
            m_SmoothFactor
        );

        m_RightPointTransform.position = Vector3.Lerp(
            m_RightPointTransform.position,
            targetRightPos,
            m_SmoothFactor
        );
    }

    private void ToggleHolisticLandmarks()
    {
        if (holisticLandmarks != null)
        {
            holisticLandmarks.SetActive(showHolisticLandmarks);
        }
    }

    private void Check_Hands_Up_Out()
    {
        Transform bodyTransform = m_BodyPointAnnotationTransform;
        Vector3 leftWrist = bodyTransform.GetChild(19).position;
        Vector3 rightWrist = bodyTransform.GetChild(20).position;
        Vector3 leftShoulder = bodyTransform.GetChild(11).position;
        Vector3 rightShoulder = bodyTransform.GetChild(12).position;

        m_LHand_Up_Out = leftWrist.y > leftShoulder.y && leftWrist.x < leftShoulder.x;
        m_RHand_Up_Out = rightWrist.y > rightShoulder.y && rightWrist.x > rightShoulder.x;
    }

    private void Check_Hands_Up_In()
    {
        Transform bodyTransform = m_BodyPointAnnotationTransform;
        Vector3 leftWrist = bodyTransform.GetChild(19).position;
        Vector3 rightWrist = bodyTransform.GetChild(20).position;
        Vector3 leftShoulder = bodyTransform.GetChild(11).position;
        Vector3 rightShoulder = bodyTransform.GetChild(12).position;

        m_LHand_Up_In = leftWrist.y > leftShoulder.y && leftWrist.x > leftShoulder.x;
        m_RHand_Up_In = rightWrist.y > rightShoulder.y && rightWrist.x < rightShoulder.x;
    }

    private void Check_Hands_Down_Out()
    {
        Transform bodyTransform = m_BodyPointAnnotationTransform;
        Vector3 leftWrist = bodyTransform.GetChild(19).position;
        Vector3 rightWrist = bodyTransform.GetChild(20).position;
        Vector3 leftShoulder = bodyTransform.GetChild(11).position;
        Vector3 rightShoulder = bodyTransform.GetChild(12).position;

        m_LHand_Down_Out = leftWrist.y < leftShoulder.y && leftWrist.x < leftShoulder.x;
        m_RHand_Down_Out = rightWrist.y < rightShoulder.y && rightWrist.x > rightShoulder.x;
    }

    private void Check_Hands_Down_In()
    {
        Transform bodyTransform = m_BodyPointAnnotationTransform;
        Vector3 leftWrist = bodyTransform.GetChild(19).position;
        Vector3 rightWrist = bodyTransform.GetChild(20).position;
        Vector3 leftShoulder = bodyTransform.GetChild(11).position;
        Vector3 rightShoulder = bodyTransform.GetChild(12).position;

        m_LHand_Down_In = leftWrist.y < leftShoulder.y && leftWrist.x > leftShoulder.x;
        m_RHand_Down_In = rightWrist.y < rightShoulder.y && rightWrist.x < rightShoulder.x;
    }

    private void Check_LRHandsCross_Up()
    {
        Transform bodyTransform = m_BodyPointAnnotationTransform;
        Vector3 leftWrist = bodyTransform.GetChild(19).position;
        Vector3 rightWrist = bodyTransform.GetChild(20).position;
        Vector3 leftShoulder = bodyTransform.GetChild(11).position;
        Vector3 rightShoulder = bodyTransform.GetChild(12).position;

        m_LRHandCross_Up = leftWrist.y > leftShoulder.y && rightWrist.y > rightShoulder.y && leftWrist.x > rightWrist.x;
        m_LRHandCross_Down = leftWrist.y < leftShoulder.y && rightWrist.y < rightShoulder.y && leftWrist.x > rightWrist.x;
    }

    private void Check_m_LR_HandLeft()
    {
        Transform bodyTransform = m_BodyPointAnnotationTransform;
        Vector3 leftWrist = bodyTransform.GetChild(19).position;
        Vector3 rightWrist = bodyTransform.GetChild(20).position;
        Vector3 leftShoulder = bodyTransform.GetChild(11).position;
        Vector3 rightShoulder = bodyTransform.GetChild(12).position;

        m_LR_HandRight_Up = leftWrist.x > leftShoulder.x && leftWrist.y > leftShoulder.y &&
                            rightWrist.x > rightShoulder.x && rightWrist.y > rightShoulder.y;
        m_LR_HandRight_Down = leftWrist.x > leftShoulder.x && leftWrist.y < leftShoulder.y &&
                              rightWrist.x > rightShoulder.x && rightWrist.y < rightShoulder.y;
    }

    private void Check_m_LR_HandRight()
    {
        Transform bodyTransform = m_BodyPointAnnotationTransform;
        Vector3 leftWrist = bodyTransform.GetChild(19).position;
        Vector3 rightWrist = bodyTransform.GetChild(20).position;
        Vector3 leftShoulder = bodyTransform.GetChild(11).position;
        Vector3 rightShoulder = bodyTransform.GetChild(12).position;

        m_LR_HandLeft_Up = leftWrist.x < leftShoulder.x && leftWrist.y > leftShoulder.y &&
                           rightWrist.x < rightShoulder.x && rightWrist.y > rightShoulder.y;
        m_LR_HandLeft_Down = leftWrist.x < leftShoulder.x && leftWrist.y < leftShoulder.y &&
                             rightWrist.x < rightShoulder.x && rightWrist.y < rightShoulder.y;
    }

    private void Check_HandsFinger()
    {
        // 왼손
        if (m_LHandPointAnnotationTransform.parent.gameObject.activeSelf)
        {
            bool index = IsFingerExtended(m_LHandPointAnnotationTransform, 5, 6, 7, 8);
            bool middle = IsFingerExtended(m_LHandPointAnnotationTransform, 9, 10, 11, 12);
            bool ring = IsFingerExtended(m_LHandPointAnnotationTransform, 13, 14, 15, 16); // 인덱스 수정
            bool pinky = IsFingerExtended(m_LHandPointAnnotationTransform, 17, 18, 19, 20);

            m_LHand_Fist = !index && !middle && !ring && !pinky;
            m_LHand_Paper = index && middle && ring && pinky;
            m_LHand_Scissors = index && middle && !ring && !pinky;

            if (!m_LHand_Fist && !m_LHand_Paper && !m_LHand_Scissors)
            {
                m_LHand_Fist = m_LHand_Paper = m_LHand_Scissors = false;
            }
        }
        else
        {
            m_LHand_Fist = m_LHand_Paper = m_LHand_Scissors = false;
        }

        // 오른손
        if (m_RHandPointAnnotationTransform.parent.gameObject.activeSelf)
        {
            bool index = IsFingerExtended(m_RHandPointAnnotationTransform, 5, 6, 7, 8);
            bool middle = IsFingerExtended(m_RHandPointAnnotationTransform, 9, 10, 11, 12);
            bool ring = IsFingerExtended(m_RHandPointAnnotationTransform, 13, 14, 15, 16); // 인덱스 수정
            bool pinky = IsFingerExtended(m_RHandPointAnnotationTransform, 17, 18, 19, 20);

            m_RHand_Fist = !index && !middle && !ring && !pinky;
            m_RHand_Paper = index && middle && ring && pinky;
            m_RHand_Scissors = index && middle && !ring && !pinky;

            if (!m_RHand_Fist && !m_RHand_Paper && !m_RHand_Scissors)
            {
                m_RHand_Fist = m_RHand_Paper = m_RHand_Scissors = false;
            }
        }
        else
        {
            m_RHand_Fist = m_RHand_Paper = m_RHand_Scissors = false;
        }
    }

    private bool IsFingerExtended(Transform hand, int knuckleIdx, int midIdx, int secondIdx, int tipIdx)
    {
        Vector3 knuckle = hand.GetChild(knuckleIdx).position;
        Vector3 mid = hand.GetChild(midIdx).position;
        Vector3 tip = hand.GetChild(tipIdx).position;

        Vector3 tipToMid = (tip - mid).normalized;
        Vector3 midToKnuckle = (mid - knuckle).normalized;
        float straightness = Vector3.Dot(tipToMid, midToKnuckle);
        float distanceSqr = (tip - knuckle).sqrMagnitude;

        return straightness > 0.8f && distanceSqr > 0.0016f; // 0.04f^2 = 0.0016f
    }
}