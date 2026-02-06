using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine.UI;
using System.IO;

public class Mediapipe_Setting : MonoBehaviour
{
    // Enum 정의
    public enum CheckType
    {
        UpperBody, // 0
        FullBody   // 1
    }

    // Inspector에서 선택 가능한 변수
    [FoldoutGroup("Setting")][SerializeField] private Canvas m_MPCanvas;
    [FoldoutGroup("Setting")][SerializeField] private CheckType m_CurrentDirection;
    [FoldoutGroup("Setting")][SerializeField] public GameObject m_AnnotatableScreen;
    [FoldoutGroup("Setting")][SerializeField] public GameObject m_PointListAnnotation;
    [FoldoutGroup("Setting")][SerializeField] public GameObject m_PoseRoiAnnotation;
    [FoldoutGroup("Setting")][SerializeField] public bool m_UseDownOffSet = true;
    [FoldoutGroup("Save")][SerializeField][Range(0, 30)] public int m_DownOffSetPerceent = 10;
    [FoldoutGroup("Save")][SerializeField][Range(2, 5)] public int m_ShoulderWideSizeDivide = 3;
    [FoldoutGroup("Save")][SerializeField][Range(0, 1)][OnValueChanged("Set_Rawimage")] private float m_ScreenAlpha = 0.3f;

    // 저장 경로
    private readonly string savePath = @"C:\KidingKiding_App\MPSettings.json";

    // 저장 데이터 구조
    [System.Serializable]
    private class MPSetting
    {
        public int downOffsetPercent;
        public int shoulderWideSizeDivide;
        public float screenAlpha;
    }

    void Start()
    {
        // 설정 로드
        LoadSettings();

        if (m_PoseRoiAnnotation != null)
        {
            var lineRenderer = m_PoseRoiAnnotation.GetComponent<LineRenderer>();
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            else
                Debug.LogWarning("m_PoseRoiAnnotation에 LineRenderer가 없습니다.");
        }
        else
        {
            Debug.LogWarning("m_PoseRoiAnnotation이 할당되지 않았습니다.");
        }
        EnsureBoxCollider();
    }

    // Odin 버튼으로 설정 저장
    [FoldoutGroup("Save")]
    [Button("Save Settings")]
    private void SaveSettings()
    {
        MPSetting data = new MPSetting
        {
            downOffsetPercent = m_DownOffSetPerceent,
            shoulderWideSizeDivide = m_ShoulderWideSizeDivide,
            screenAlpha = m_ScreenAlpha
        };

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"설정이 저장되었습니다: {savePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"설정 저장 실패: {e.Message}");
        }
    }

    // Odin 버튼으로 설정 로드
    [FoldoutGroup("Save")]
    [Button("Load Settings")]
    private void LoadSettings()
    {
        try
        {
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                MPSetting data = JsonUtility.FromJson<MPSetting>(json);
                m_DownOffSetPerceent = data.downOffsetPercent;
                m_ShoulderWideSizeDivide = data.shoulderWideSizeDivide;
                m_ScreenAlpha = data.screenAlpha;
                Debug.Log($"설정이 로드되었습니다: DownOffsetPercent={m_DownOffSetPerceent}, ShoulderWideSizeDivide={m_ShoulderWideSizeDivide}, ScreenAlpha={m_ScreenAlpha}");
            }
            else
            {
                Debug.LogWarning($"설정 파일을 찾을 수 없습니다: {savePath}. 기본값 사용.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"설정 로드 실패: {e.Message}");
        }

        Set_Rawimage();
    }

    private void Set_Rawimage()
    {
        RawImage rawImage = m_AnnotatableScreen.GetComponent<RawImage>();

        Color color = rawImage.color;
        color.a = m_ScreenAlpha; // 즉시 투명도 설정
        rawImage.color = color;
    }

    void Update()
    {
        if (m_PointListAnnotation == null || m_AnnotatableScreen == null || m_MPCanvas == null)
        {
            Debug.LogWarning("m_PointListAnnotation, m_AnnotatableScreen, 또는 m_Canvas가 할당되지 않았습니다.");
            return;
        }

        BoxCollider boxCollider = m_PointListAnnotation.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogWarning("m_PointListAnnotation에 BoxCollider가 없습니다.");
            return;
        }

        if (m_CurrentDirection == CheckType.FullBody)
        {
            UpdateBoxColliderSize_FullBody();
        }
        else if (m_CurrentDirection == CheckType.UpperBody)
        {
            UpdateBoxColliderSize_UpperBody();
            AlignScreenToCanvasYCenter(boxCollider);
        }

        AnnotatableScreenSize();
    }

    private void AlignScreenToCanvasYCenter(BoxCollider boxCollider)
    {
        if (boxCollider == null)
        {
            Debug.LogWarning("BoxCollider가 null입니다.");
            return;
        }

        float colliderYCenter = boxCollider.bounds.center.y;
        RectTransform canvasRect = m_MPCanvas.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            Debug.LogWarning("m_Canvas에 RectTransform이 없습니다.");
            return;
        }

        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);
        float canvasYCenter = (canvasCorners[0].y + canvasCorners[2].y) / 2f;
        float canvasHeight = Mathf.Abs(canvasCorners[2].y - canvasCorners[0].y);
        float offset = m_UseDownOffSet ? canvasHeight * (m_DownOffSetPerceent / 100f) : 0f;
        float targetCanvasYCenter = canvasYCenter - offset;
        Vector3 screenPosition = m_AnnotatableScreen.transform.position;

        Renderer screenRenderer = m_AnnotatableScreen.GetComponent<Renderer>();
        float screenYCenterOffset = 0f;
        if (screenRenderer != null)
        {
            screenYCenterOffset = screenRenderer.bounds.center.y - screenPosition.y;
        }
        else
        {
            Debug.LogWarning("m_AnnotatableScreen에 Renderer가 없습니다. y 오프셋을 0으로 설정합니다.");
        }

        float targetScreenY = targetCanvasYCenter - (colliderYCenter - screenPosition.y);
        float currentY = screenPosition.y;
        float smoothSpeed = 10f;
        float smoothedY = Mathf.Lerp(currentY, targetScreenY, Time.deltaTime * smoothSpeed);

        screenPosition.y = smoothedY;
        m_AnnotatableScreen.transform.position = screenPosition;

        Debug.Log($"m_AnnotatableScreen y-위치 정렬: {screenPosition.y}, BoxCollider y-중심: {colliderYCenter}, Canvas y-중심: {canvasYCenter}, 오프셋: {offset}, 목표 Canvas y-중심: {targetCanvasYCenter}");
    }

    private void EnsureBoxCollider()
    {
        if (m_PointListAnnotation == null)
        {
            Debug.LogWarning("m_PointListAnnotation이 할당되지 않았습니다.");
            return;
        }

        BoxCollider boxCollider = m_PointListAnnotation.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = m_PointListAnnotation.AddComponent<BoxCollider>();
            Debug.Log("m_PointListAnnotation에 BoxCollider가 추가되었습니다.");
        }
    }

    private void UpdateBoxColliderSize_FullBody()
    {
        if (m_PointListAnnotation == null)
        {
            Debug.LogWarning("m_PointListAnnotation이 할당되지 않았습니다.");
            return;
        }

        BoxCollider boxCollider = m_PointListAnnotation.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogWarning("m_PointListAnnotation에 BoxCollider가 없습니다.");
            return;
        }

        Renderer[] renderers = m_PointListAnnotation.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogWarning("m_PointListAnnotation의 하위 오브젝트에 Renderer가 없습니다 (비활성화 포함).");
            return;
        }

        Debug.Log($"FullBody Renderers: {string.Join(", ", renderers.Select(r => r.gameObject.name))}");
        Bounds combinedBounds = renderers[0].bounds;

        foreach (Renderer renderer in renderers)
        {
            combinedBounds.Encapsulate(renderer.bounds);
        }

        Debug.Log($"FullBody Combined Bounds Size: {combinedBounds.size}");
        boxCollider.center = m_PointListAnnotation.transform.InverseTransformPoint(combinedBounds.center);

        Vector3 localScale = m_PointListAnnotation.transform.lossyScale;
        boxCollider.size = new Vector3(
            Mathf.Abs(localScale.x) > 0.0001f ? combinedBounds.size.x / Mathf.Abs(localScale.x) : combinedBounds.size.x,
            Mathf.Abs(localScale.y) > 0.0001f ? combinedBounds.size.y / Mathf.Abs(localScale.y) : combinedBounds.size.y,
            Mathf.Abs(localScale.z) > 0.0001f ? combinedBounds.size.z / Mathf.Abs(localScale.z) : combinedBounds.size.z
        );

        Debug.Log($"FullBody BoxCollider Size: {boxCollider.size}");
    }

    private void UpdateBoxColliderSize_UpperBody()
    {
        if (m_PointListAnnotation == null)
        {
            Debug.LogWarning("m_PointListAnnotation이 할당되지 않았습니다.");
            return;
        }

        BoxCollider boxCollider = m_PointListAnnotation.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogWarning("m_PointListAnnotation에 BoxCollider가 없습니다.");
            return;
        }

        Renderer[] renderers = m_PointListAnnotation.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogWarning("m_PointListAnnotation의 하위 오브젝트에 Renderer가 없습니다 (비활성화 포함).");
            return;
        }

        var upperBodyRenderers = renderers.Take(25).ToArray();
        if (upperBodyRenderers.Length == 0)
        {
            Debug.LogWarning("m_PointListAnnotation의 하위 오브젝트에 인덱스 0-24번 Renderer가 없습니다.");
            return;
        }

        Debug.Log($"UpperBody Renderers: {string.Join(", ", upperBodyRenderers.Select(r => r.gameObject.name))}");
        Bounds combinedBounds = upperBodyRenderers[0].bounds;

        foreach (Renderer renderer in upperBodyRenderers)
        {
            combinedBounds.Encapsulate(renderer.bounds);
        }

        Debug.Log($"UpperBody Combined Bounds Size: {combinedBounds.size}");
        boxCollider.center = m_PointListAnnotation.transform.InverseTransformPoint(combinedBounds.center);

        Vector3 localScale = m_PointListAnnotation.transform.lossyScale;
        boxCollider.size = new Vector3(
            Mathf.Abs(localScale.x) > 0.0001f ? combinedBounds.size.x / Mathf.Abs(localScale.x) : combinedBounds.size.x,
            Mathf.Abs(localScale.y) > 0.0001f ? combinedBounds.size.y / Mathf.Abs(localScale.y) : combinedBounds.size.y,
            Mathf.Abs(localScale.z) > 0.0001f ? combinedBounds.size.z / Mathf.Abs(localScale.z) : combinedBounds.size.z
        );

        Debug.Log($"UpperBody BoxCollider Size: {boxCollider.size}");
    }

    private void AnnotatableScreenSize()
    {
        Transform leftShoulder = m_PointListAnnotation.transform.GetChild(11);
        Transform rightShoulder = m_PointListAnnotation.transform.GetChild(12);

        float shoulderDistance = Mathf.Abs(leftShoulder.transform.localPosition.x - rightShoulder.transform.localPosition.x);
        float canvasWidth = m_MPCanvas.GetComponent<RectTransform>().rect.width;
        float referenceShoulderDistance = canvasWidth / m_ShoulderWideSizeDivide;
        float targetScale = referenceShoulderDistance / shoulderDistance;

        Vector3 currentScale = m_AnnotatableScreen.transform.localScale;
        float smoothSpeed = 5f;
        float smoothedScale = Mathf.Lerp(currentScale.x, targetScale, Time.deltaTime * smoothSpeed);
        m_AnnotatableScreen.transform.localScale = new Vector3(smoothedScale, smoothedScale, smoothedScale);
    }
}