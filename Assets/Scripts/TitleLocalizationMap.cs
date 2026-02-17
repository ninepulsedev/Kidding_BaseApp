using UnityEngine;
using UnityEngine.Localization;
using System.Collections.Generic;

[System.Serializable]
public class TitleMapping
{
    public string koreanTitle;
    public string tableReference;
    public string tableEntryReference;
}

[System.Serializable]
public class TitleLocalizationData
{
    public List<TitleMapping> mappings;
}

public static class TitleLocalizationMap
{
    private static Dictionary<string, LocalizedString> titleMappings = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        if (titleMappings != null) return;

        titleMappings = new Dictionary<string, LocalizedString>();

        // Resources 폴더에서 JSON 파일 로드
        TextAsset jsonFile = Resources.Load<TextAsset>("title_localization");
        if (jsonFile == null)
        {
            Debug.LogError("JSON 파일을 찾을 수 없습니다: title_localization.json");
            return;
        }

        try
        {
            TitleLocalizationData data = JsonUtility.FromJson<TitleLocalizationData>(jsonFile.text);
            foreach (var mapping in data.mappings)
            {
                titleMappings[mapping.koreanTitle] = new LocalizedString
                {
                    TableReference = mapping.tableReference,
                    TableEntryReference = mapping.tableEntryReference
                };
            }
            Debug.Log($"✅ JSON에서 로드된 매핑 수: {titleMappings.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON 파싱 실패: {e.Message}");
        }
    }

    public static LocalizedString GetLocalizedString(string koreanTitle)
    {
        Initialize(); // 지연 초기화
        if (titleMappings.TryGetValue(koreanTitle, out LocalizedString localizedString))
        {
            return localizedString;
        }
        Debug.LogWarning($"매핑되지 않은 제목: {koreanTitle}");
        return null;
    }
}