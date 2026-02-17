using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class OverlayTextManager : MonoBehaviour
{
    [System.Serializable]
    public struct TextConfig
    {
        public VideoContentType videoContentType;
        public string customMessage;
    }
    
    [FoldoutGroup("Overlay Settings")]
    [Header("Custom Text Configuration")]
    public List<TextConfig> textConfigs;
    
    public string GetCustomText(VideoContentType type)
    {
        var config = textConfigs?.Find(x => x.videoContentType == type);
        return config?.customMessage ?? "";
    }
    
    private void Start()
    {
        if (textConfigs == null || textConfigs.Count == 0)
        {
            textConfigs = new List<TextConfig>
            {
                new TextConfig { videoContentType = VideoContentType.GreekMythology, customMessage = "그리스 신화의 전설" },
                new TextConfig { videoContentType = VideoContentType.Story, customMessage = "이야기의 시작" },
                new TextConfig { videoContentType = VideoContentType.MV, customMessage = "음악과 함께" }
            };
        }
    }
}
