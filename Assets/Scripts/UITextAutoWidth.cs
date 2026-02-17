using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Text))]
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]

public class UITextAutoWidth : MonoBehaviour
{
    public enum WidthMode
    {
        AutoCalculate,    // 텍스트 내용에 맞춰 자동 너비 계산
        CopyFromSource    // 다른 RectTransform에서 너비 복사
    }

    [Header("Width Mode Selection")]
    [Tooltip("너비 조절 모드 선택")]
    public WidthMode widthMode = WidthMode.AutoCalculate;

    // Auto Calculate 모드 설정
    [ShowIf("widthMode", WidthMode.AutoCalculate)]
    [Header("Auto Calculate Settings")]
    [Tooltip("최소 너비 (0이면 제한 없음)")]
    public float minWidth = 0f;
    
    [ShowIf("widthMode", WidthMode.AutoCalculate)]
    [Tooltip("최대 너비 (0이면 제한 없음)")]
    public float maxWidth = 0f;
    
    [ShowIf("widthMode", WidthMode.AutoCalculate)]
    [Tooltip("여백 (텍스트 양쪽에 추가할 공간)")]
    public float padding = 0f;

    // Copy From Source 모드 설정
    [ShowIf("widthMode", WidthMode.CopyFromSource)]
    [Header("Copy From Source Settings")]
    [Tooltip("너비를 복사할 소스 RectTransform")]
    public RectTransform copyWidthFrom;

    // 공통 업데이트 설정
    [Header("Update Settings")]
    [Tooltip("매 프레임 업데이트 (에디터에서 실시간 작동)")]
    public bool updateEveryFrame = true;
    
    [Tooltip("텍스트가 변경될 때만 업데이트")]
    public bool updateOnTextChanged = true;
    
    private bool m_NeedsUpdate = false;

    private void Awake()
    {
        m_Text = GetComponent<Text>();
        m_RectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        UpdateWidth();
    }

    private void OnValidate()
    {
        // 에디터에서 값이 변경되면 업데이트 플래그 설정
        m_NeedsUpdate = true;
    }

    // ExecuteAlways는 에디터에서 매 프레임 실행됨
    private void Update()
    {
        // OnValidate에서 설정된 업데이트 플래그 확인
        if (m_NeedsUpdate)
        {
            m_NeedsUpdate = false;
            UpdateWidthByMode();
            return;
        }
        
        // 에디터에서만 실행 (플레이 모드에서는 필요에 따라)
        if (!Application.isPlaying)
        {
            UpdateWidthByMode();
        }
        else if (updateEveryFrame)
        {
            UpdateWidthByMode();
        }
        else if (updateOnTextChanged)
        {
            CheckForChanges();
        }
    }
    
    private void UpdateWidthByMode()
    {
        switch (widthMode)
        {
            case WidthMode.AutoCalculate:
                UpdateWidth();
                break;
            case WidthMode.CopyFromSource:
                CopyWidthFromSource();
                break;
        }
    }

    private void CheckForChanges()
    {
        bool hasChanged = false;
        
        if (m_Text.text != m_LastText)
        {
            m_LastText = m_Text.text;
            hasChanged = true;
        }
        
        if (m_Text.fontSize != m_LastFontSize)
        {
            m_LastFontSize = m_Text.fontSize;
            hasChanged = true;
        }
        
        if (m_Text.font != m_LastFont)
        {
            m_LastFont = m_Text.font;
            hasChanged = true;
        }
        
        if (hasChanged)
        {
            UpdateWidthByMode();
        }
    }

    public void UpdateWidth()
    {
        if (m_Text == null || m_RectTransform == null)
            return;

        // Text.preferredWidth 사용 (가장 간단하고 정확)
        float textWidth = 0f;
        
        try {
            // Canvas 업데이트 강제
            Canvas.ForceUpdateCanvases();
            
            // preferredWidth 사용
            textWidth = m_Text.preferredWidth;
            
            if (textWidth <= 0)
            {
                // preferredWidth가 0이면 문자 기반 계산 (폴백)
                textWidth = m_Text.text.Length * m_Text.fontSize * 0.6f;
                Debug.LogWarning("[UITextAutoWidth] preferredWidth가 0이어서 문자 기반 계산 사용");
            }
        }
        catch (System.Exception e) {
            // 예외 발생시 문자 기반 계산 (폴백)
            textWidth = m_Text.text.Length * m_Text.fontSize * 0.6f;
            Debug.LogWarning($"[UITextAutoWidth] preferredWidth 실패: {e.Message}, 문자 기반 계산 사용");
        }
        
        // 패딩 없이 텍스트 너비 그대로 사용
        float targetWidth = textWidth;
        
        // 최소/최대 너비 제한 적용
        if (minWidth > 0 && targetWidth < minWidth)
            targetWidth = minWidth;
        
        if (maxWidth > 0 && targetWidth > maxWidth)
            targetWidth = maxWidth;
        
        // RectTransform 너비 설정
        Vector2 currentSize = m_RectTransform.sizeDelta;
        currentSize.x = targetWidth;
        m_RectTransform.sizeDelta = currentSize;
        
        // 간단한 디버그 로그
        Debug.Log($"[UITextAutoWidth] 텍스트: '{m_Text.text}', 너비: {textWidth:F2}, 최종: {targetWidth:F2}");
    }
    
    // 수동으로 너비 업데이트하는 공개 메서드
    [ContextMenu("Update Width Now")]
    public void ForceUpdateWidth()
    {
        UpdateWidthByMode();
    }
    
    // 모드 설정 메서드
    public void SetWidthMode(WidthMode mode)
    {
        widthMode = mode;
        UpdateWidthByMode();
    }
    
    // 현재 모드 가져오기
    public WidthMode GetCurrentMode()
    {
        return widthMode;
    }
    
    // 너비 복사 기능
    private void CopyWidthFromSource()
    {
        if (copyWidthFrom == null || m_RectTransform == null)
        {
            Debug.LogWarning("[UITextAutoWidth] 소스 RectTransform이 null입니다");
            return;
        }
        
        // 소스의 sizeDelta.x 값을 그대로 가져옴
        float sourceWidth = copyWidthFrom.sizeDelta.x;
        
        // 최소/최대 너비 제한 적용
        float targetWidth = sourceWidth;
        if (minWidth > 0 && targetWidth < minWidth)
            targetWidth = minWidth;
        
        if (maxWidth > 0 && targetWidth > maxWidth)
            targetWidth = maxWidth;
        
        // RectTransform 너비 설정
        Vector2 currentSize = m_RectTransform.sizeDelta;
        currentSize.x = targetWidth;
        m_RectTransform.sizeDelta = currentSize;
        
        // 간단한 디버그 로그
        Debug.Log($"[UITextAutoWidth] 너비 복사: 소스={sourceWidth:F2}, 목표={targetWidth:F2}");
    }
    
    // 소스 RectTransform 설정
    public void SetWidthSource(RectTransform source)
    {
        copyWidthFrom = source;
        CopyWidthFromSource();
    }
    
    // Inspector에서 테스트용 메서드
    [ContextMenu("Test with Current Text")]
    public void TestCurrentText()
    {
        UpdateWidthByMode();
        Debug.Log($"[UITextAutoWidth] 현재 모드: {widthMode}, 텍스트: '{m_Text.text}', 너비: {m_RectTransform.sizeDelta.x:F2}");
    }
    
    [ContextMenu("Switch to Auto Calculate")]
    public void SwitchToAutoCalculate()
    {
        SetWidthMode(WidthMode.AutoCalculate);
        Debug.Log($"[UITextAutoWidth] 모드 변경: Auto Calculate");
    }
    
    [ContextMenu("Switch to Copy From Source")]
    public void SwitchToCopyFromSource()
    {
        SetWidthMode(WidthMode.CopyFromSource);
        Debug.Log($"[UITextAutoWidth] 모드 변경: Copy From Source");
    }
    
    [ContextMenu("Copy Width from Source")]
    public void CopyWidthFromSourceMenu()
    {
        if (widthMode == WidthMode.CopyFromSource)
        {
            CopyWidthFromSource();
            if (copyWidthFrom != null)
            {
                Debug.Log($"[UITextAutoWidth] 소스 너비 복사 완료: {copyWidthFrom.sizeDelta.x:F2} → {m_RectTransform.sizeDelta.x:F2}");
            }
            else
            {
                Debug.LogWarning("[UITextAutoWidth] 소스 RectTransform이 설정되지 않았습니다.");
            }
        }
        else
        {
            Debug.LogWarning("[UITextAutoWidth] 현재 모드가 Copy From Source가 아닙니다.");
        }
    }
}
