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
        AutoCalculate,
        CopyFromSource
    }

    [Header("Width Mode Selection")]
    public WidthMode widthMode = WidthMode.AutoCalculate;

    [ShowIf("widthMode", WidthMode.AutoCalculate)]
    [Header("Auto Calculate Settings")]
    public float minWidth = 0f;
    public float maxWidth = 0f;
    public float padding = 0f;

    [ShowIf("widthMode", WidthMode.CopyFromSource)]
    [Header("Copy From Source Settings")]
    public RectTransform copyWidthFrom;

    [Header("Update Settings")]
    public bool updateEveryFrame = true;
    public bool updateOnTextChanged = true;
    
    private bool m_NeedsUpdate = false;
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

    private void OnValidate()
    {
        m_NeedsUpdate = true;
    }

    private void Update()
    {
        if (m_NeedsUpdate)
        {
            m_NeedsUpdate = false;
            UpdateWidthByMode();
            return;
        }
        
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

        float textWidth = 0f;
        
        try {
            Canvas.ForceUpdateCanvases();
            textWidth = m_Text.preferredWidth;
            
            if (textWidth <= 0)
            {
                textWidth = m_Text.text.Length * m_Text.fontSize * 0.6f;
            }
        }
        catch (System.Exception e) {
            textWidth = m_Text.text.Length * m_Text.fontSize * 0.6f;
        }
        
        float targetWidth = textWidth;
        
        if (minWidth > 0 && targetWidth < minWidth)
            targetWidth = minWidth;
        
        if (maxWidth > 0 && targetWidth > maxWidth)
            targetWidth = maxWidth;
        
        Vector2 currentSize = m_RectTransform.sizeDelta;
        currentSize.x = targetWidth;
        m_RectTransform.sizeDelta = currentSize;
    }
    
    [ContextMenu("Update Width Now")]
    public void ForceUpdateWidth()
    {
        UpdateWidthByMode();
    }
    
    public void SetWidthMode(WidthMode mode)
    {
        widthMode = mode;
        UpdateWidthByMode();
    }
    
    public WidthMode GetCurrentMode()
    {
        return widthMode;
    }
    
    private void CopyWidthFromSource()
    {
        if (copyWidthFrom == null || m_RectTransform == null)
            return;
        
        float sourceWidth = copyWidthFrom.sizeDelta.x;
        float targetWidth = sourceWidth;
        
        if (minWidth > 0 && targetWidth < minWidth)
            targetWidth = minWidth;
        
        if (maxWidth > 0 && targetWidth > maxWidth)
            targetWidth = maxWidth;
        
        Vector2 currentSize = m_RectTransform.sizeDelta;
        currentSize.x = targetWidth;
        m_RectTransform.sizeDelta = currentSize;
    }
    
    public void SetWidthSource(RectTransform source)
    {
        copyWidthFrom = source;
        CopyWidthFromSource();
    }
    
    [ContextMenu("Test with Current Text")]
    public void TestCurrentText()
    {
        UpdateWidthByMode();
    }
    
    [ContextMenu("Switch to Auto Calculate")]
    public void SwitchToAutoCalculate()
    {
        SetWidthMode(WidthMode.AutoCalculate);
    }
    
    [ContextMenu("Switch to Copy From Source")]
    public void SwitchToCopyFromSource()
    {
        SetWidthMode(WidthMode.CopyFromSource);
    }
    
    [ContextMenu("Copy Width from Source")]
    public void CopyWidthFromSourceMenu()
    {
        if (widthMode == WidthMode.CopyFromSource)
        {
            CopyWidthFromSource();
        }
    }
}
