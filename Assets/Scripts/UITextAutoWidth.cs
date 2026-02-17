using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
[RequireComponent(typeof(RectTransform))]
public class UITextAutoWidth : MonoBehaviour
{
    [Header("Auto Width Settings")]
    [Tooltip("최소 너비 (0이면 제한 없음)")]
    public float minWidth = 0f;
    
    [Tooltip("최대 너비 (0이면 제한 없음)")]
    public float maxWidth = 0f;
    
    [Tooltip("여백 (텍스트 양쪽에 추가할 공간)")]
    public float padding = 10f;
    
    [Tooltip("매 프레임 업데이트 (성능에 영향을 줄 수 있음)")]
    public bool updateEveryFrame = false;
    
    [Tooltip("텍스트가 변경될 때만 업데이트")]
    public bool updateOnTextChanged = true;
    
    private Text m_Text;
    private RectTransform m_RectTransform;
    private string m_LastText = "";
    private float m_LastFontSize = 0f;
    private Font m_LastFont = null;

    private void Awake()
    {
        m_Text = GetComponent<Text>();
        m_RectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        UpdateWidth();
    }

    private void Update()
    {
        if (updateEveryFrame)
        {
            UpdateWidth();
        }
        else if (updateOnTextChanged)
        {
            CheckForChanges();
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
            UpdateWidth();
        }
    }

    public void UpdateWidth()
    {
        if (m_Text == null || m_RectTransform == null)
            return;

        // 텍스트의 실제 너비 계산
        TextGenerator generator = m_Text.cachedTextGenerator;
        
        // 설정 가져오기 및 텍스트 생성
        TextGenerationSettings settings = m_Text.GetGenerationSettings(Vector2.zero);
        generator.Populate(m_Text.text, settings);
        
        // 생성된 텍스트의 너비 계산
        float textWidth = generator.preferredWidth;
        
        // 패딩 추가
        float targetWidth = textWidth + (padding * 2f);
        
        // 최소/최대 너비 제한 적용
        if (minWidth > 0 && targetWidth < minWidth)
            targetWidth = minWidth;
        
        if (maxWidth > 0 && targetWidth > maxWidth)
            targetWidth = maxWidth;
        
        // RectTransform 너비 설정
        Vector2 currentSize = m_RectTransform.sizeDelta;
        currentSize.x = targetWidth;
        m_RectTransform.sizeDelta = currentSize;
        
        // 디버그 로그 (선택적)
        // Debug.Log($"[UITextAutoWidth] 텍스트: '{m_Text.text}', 너비: {textWidth:F2}, 최종: {targetWidth:F2}");
    }
    
    // 수동으로 너비 업데이트하는 공개 메서드
    [ContextMenu("Update Width Now")]
    public void ForceUpdateWidth()
    {
        UpdateWidth();
    }
    
    // 텍스트 설정과 동시에 너비 업데이트
    public void SetText(string newText)
    {
        if (m_Text != null)
        {
            m_Text.text = newText;
            UpdateWidth();
        }
    }
    
    // Inspector에서 테스트용 메서드
    [ContextMenu("Test with Current Text")]
    public void TestCurrentText()
    {
        UpdateWidth();
        Debug.Log($"[UITextAutoWidth] 현재 텍스트: '{m_Text.text}', 계산된 너비: {m_RectTransform.sizeDelta.x:F2}");
    }
}
