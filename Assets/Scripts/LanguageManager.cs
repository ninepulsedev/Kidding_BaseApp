#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using System.Globalization;
using System.IO;
using Utils;

[ExecuteInEditMode]
public class LanguageManager : MonoBehaviour
{
    #region Singleton & Enums
    public enum Language { English, Korean, Russian, Uzbek }
    public static LanguageManager m_Instance;
    #endregion

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        m_Instance = null;
    }

    #region Serializable Classes
    [System.Serializable]
    private class LanguageSettings
    {
        public bool systemLanguage;
        public Language selectedLanguage;
    }

    [System.Serializable]
    public class LocalizedTableItem
    {
        [HorizontalGroup("Group00"), HideLabel] public bool isActive = false;
        [HorizontalGroup("Group00"), HideLabel] public LocalizedStringTable tableAsset;
    }

    [System.Serializable]
    public class LocalizableText
    {
        [FoldoutGroup("Properties"), PropertyOrder(2)] public Text m_TextComponent;
        [FoldoutGroup("Properties"), ReadOnly, PropertyOrder(3)] public string m_Key;
        [FoldoutGroup("Properties"), PropertyOrder(3)][ListDrawerSettings(IsReadOnly = true)] public LanguageTextConfig[] m_LanguageSettings;
        [FoldoutGroup("Properties"), PropertyOrder(3)] public List<LocalizedTableItem> m_LocalizedAssetTables = new List<LocalizedTableItem>();

        [HideInInspector] public int currentIndex = 0;
        [HideInInspector] public int tableCount = 0;
        [HideInInspector] public Language currentLanguage;

        #region Buttons
        [HorizontalGroup("Properties/Group00"), PropertyOrder(4)]
        [Button("Before", ButtonSizes.Large), GUIColor(0, 1, 1)]
        private void Before()
        {
            if (!ValidateTable()) return;
            currentIndex--;
            ApplyCurrentText();
        }

        [HorizontalGroup("Properties/Group00"), PropertyOrder(5)]
        [Button("Next", ButtonSizes.Large), GUIColor(0, 1, 1)]
        private void Next()
        {
            if (!ValidateTable()) return;
            currentIndex++;
            ApplyCurrentText();
        }
        #endregion

        private bool ValidateTable()
        {
            return m_LocalizedAssetTables != null && m_LocalizedAssetTables.Count > 0 && m_TextComponent != null;
        }

        public void ApplyCurrentText()
        {
            if (m_TextComponent == null) return;
            if (m_LocalizedAssetTables == null || m_LocalizedAssetTables.Count == 0)
            {
                m_TextComponent.text = "<No Table>";
                return;
            }

            _ = ApplyCurrentTextAsync();
        }

        private async System.Threading.Tasks.Task ApplyCurrentTextAsync()
        {
            try
            {
                if (!LocalizationSettings.InitializationOperation.IsDone)
                    await LocalizationSettings.InitializationOperation.Task;

                List<StringTableEntry> allEntries = new List<StringTableEntry>();

                foreach (var tableItem in m_LocalizedAssetTables)
                {
                    if (tableItem == null || tableItem.tableAsset == null) continue;
                    if (!tableItem.isActive) continue;

                    var tableOp = tableItem.tableAsset.GetTableAsync();
                    await tableOp.Task;

                    if (tableOp.Result is StringTable stringTable)
                    {
                        foreach (var kv in stringTable)
                            allEntries.Add(kv.Value);
                    }
                }

                tableCount = allEntries.Count;
                if (tableCount == 0)
                {
                    m_TextComponent.text = "<Empty Table>";
                    return;
                }

                if (currentIndex < 0 || currentIndex >= allEntries.Count)
                    currentIndex = 0;

                m_Key = allEntries[currentIndex].Key;
                ApplyTextFromKey();
            }
            catch
            {
                m_TextComponent.text = "<Error Loading Table>";
            }
        }

        public void ApplyTextFromKey()
        {
            if (m_TextComponent == null) return;
            if (string.IsNullOrEmpty(m_Key))
            {
                m_TextComponent.text = "<No Key>";
                return;
            }

            _ = ApplyTextFromKeyAsync();
        }

        private async System.Threading.Tasks.Task ApplyTextFromKeyAsync()
        {
            try
            {
                if (!LocalizationSettings.InitializationOperation.IsDone)
                    await LocalizationSettings.InitializationOperation.Task;

                foreach (var tableItem in m_LocalizedAssetTables)
                {
                    if (tableItem == null || tableItem.tableAsset == null) continue;
                    if (!tableItem.isActive) continue;

                    var tableOp = tableItem.tableAsset.GetTableAsync();
                    await tableOp.Task;

                    if (tableOp.Result is StringTable stringTable)
                    {
                        var entry = stringTable.GetEntry(m_Key);
                        if (entry != null)
                        {
                            m_TextComponent.text = entry.LocalizedValue;
                            return;
                        }
                    }
                }

                m_TextComponent.text = "<Key Missing>";
            }
            catch
            {
                m_TextComponent.text = "<Error Loading Key>";
            }
        }

        public void UpdateLanguage(Language newLanguage)
        {
            currentLanguage = newLanguage;
            ApplyCurrentText();
        }

        public void SnapToStep()
        {
            if (m_LanguageSettings != null)
            {
                foreach (var ls in m_LanguageSettings)
                {
                    ls.sizePercentage = Mathf.Clamp(ls.sizePercentage, 0.1f, 2f);
                    ls.sizePercentage = Mathf.Round(ls.sizePercentage / 0.05f) * 0.05f;

                    ls.lineSpacing = Mathf.Clamp(ls.lineSpacing, 0.1f, 2f);
                    ls.lineSpacing = Mathf.Round(ls.lineSpacing / 0.05f) * 0.05f;
                }
            }
        }

        [ShowInInspector, ReadOnly, PropertyOrder(0)]
        public string CurrentIndexDisplay
        {
            get
            {
                int total = tableCount;
                if (total == 0) return "0/0";
                int idx = Mathf.Clamp(currentIndex, 0, total - 1);
                return $"{idx + 1}/{total}";
            }
        }

        public void SetActiveTable(int selectedIndex)
        {
            for (int i = 0; i < m_LocalizedAssetTables.Count; i++)
            {
                m_LocalizedAssetTables[i].isActive = (i == selectedIndex);
            }
            currentIndex = 0;
            ApplyCurrentText();
        }
    }

    [System.Serializable]
    public class LanguageTextConfig
    {
        [ReadOnly] public Language language;
        [Range(0.1f, 2f), OnValueChanged("OnSizePercentageChanged")] public float sizePercentage = 1f;
        [Range(0.1f, 2f), OnValueChanged("OnLineSpacingChanged")] public float lineSpacing = 1f;
        [HideInInspector] public Text m_TextComponent;
        
        // 에디터에서 실시간 적용을 위한 콜백
        private void OnSizePercentageChanged()
        {
            if (m_Instance != null)
            {
                m_Instance.ApplyAllTextSettings();
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(m_Instance);
                }
#endif
            }
        }
        
        private void OnLineSpacingChanged()
        {
            if (m_Instance != null)
            {
                m_Instance.ApplyAllTextSettings();
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(m_Instance);
                }
#endif
            }
        }
    }

    [System.Serializable]
    public class LanguageConfig
    {
        [ReadOnly] public Language language;
        public Font m_Font;
        [Range(1, 100), OnValueChanged("OnGlobalTextSizeChanged")] public int m_GlobalTextSize = 40;
        [Range(0.1f, 2f), OnValueChanged("OnGlobalLineSpacingChanged")] public float m_GlobalLineSpacing = 1f;
        
        // 에디터에서 실시간 적용을 위한 콜백
        private void OnGlobalTextSizeChanged()
        {
            if (m_Instance != null)
            {
                m_Instance.ApplyAllTextSettings();
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(m_Instance);
                }
#endif
            }
        }
        
        private void OnGlobalLineSpacingChanged()
        {
            if (m_Instance != null)
            {
                m_Instance.ApplyAllTextSettings();
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(m_Instance);
                }
#endif
            }
        }
    }
    #endregion
    #region Inspector Fields
    //[FoldoutGroup("Setting"), PropertyOrder(0), ToggleLeft]
    [HorizontalGroup("Setting/Group00"), ToggleLeft, SerializeField, OnValueChanged("OnSystemLanguageChanged")] private bool m_SystemLanguage = true;

    [HorizontalGroup("Setting/Group00"), HideLabel, PropertyOrder(0)]
    [SerializeField, OnValueChanged("OnLanguageChanged")] public Language m_SelectedLanguage;

    [FoldoutGroup("Setting"), PropertyOrder(1)]
    [ListDrawerSettings(IsReadOnly = true)] public LanguageConfig[] m_LanguageConfigs;

    [FoldoutGroup("Dynamic Text"), PropertyOrder(2)]
    public List<LocalizableText> m_LocalizableTexts = new List<LocalizableText>();

    [FoldoutGroup("Dev"), ToggleLeft, PropertyOrder(2)] public bool m_EnableDebugLogs = false;
    #endregion

    private readonly string jsonFilePath = Paths.LanguagePath;
    private StringTable koTable;

    #region Runtime
    private void Start()
    {
        if (m_Instance == null) m_Instance = this;
        else if (m_Instance != this) Destroy(gameObject);

        AutoGenerateLanguageSettings();

        if (Application.isPlaying)
        {
            LoadLanguageSettings();
            ApplyAllTextSettings();
            StartCoroutine(SetLocaleAndApplyTexts());
        }
        else
        {
            ApplyAllTextSettings();
        }
    }

    private IEnumerator SetLocaleAndApplyTexts()
    {
        yield return LocalizationSettings.InitializationOperation;

        if (m_SystemLanguage)
            m_SelectedLanguage = DetectSystemLanguage();

        ApplySelectedLanguageRealtime();
    }

    private void OnSystemLanguageChanged()
    {
        if (Application.isPlaying)
        {
            if (m_SystemLanguage)
                m_SelectedLanguage = DetectSystemLanguage();

            ApplySelectedLanguageRealtime();
        }
#if UNITY_EDITOR
        else
        {
            ApplySelectedLanguageEditor();
        }
#endif
    }

    private void OnLanguageChanged()
    {
        if (Application.isPlaying)
        {
            ApplySelectedLanguageRealtime();
        }
#if UNITY_EDITOR
        else
        {
            ApplySelectedLanguageEditor();
        }
#endif
    }


    public void SetLanguage(Language language)
    {
        if (!Application.isPlaying) return;
        m_SystemLanguage = false;
        m_SelectedLanguage = language;
        ApplySelectedLanguageRealtime();
    }

    private void ApplySelectedLanguageRealtime()
    {
        if (!Application.isPlaying) return;

        SetLocale(m_SelectedLanguage);

        if (m_LocalizableTexts == null) return;

        foreach (var item in m_LocalizableTexts)
        {
            if (item == null) continue;
            item.UpdateLanguage(m_SelectedLanguage);
        }
    }

#if UNITY_EDITOR
    private void ApplySelectedLanguageEditor()
    {
        string localeCode = GetLocaleCode(m_SelectedLanguage);
        var locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
        if (locale != null) LocalizationSettings.SelectedLocale = locale;

        foreach (var item in m_LocalizableTexts)
        {
            if (item == null || item.m_LocalizedAssetTables == null || item.m_TextComponent == null) continue;

            item.UpdateLanguage(m_SelectedLanguage);

            var lse = item.m_TextComponent.GetComponent<LocalizeStringEvent>();
            if (lse != null)
            {
                for (int i = 0; i < item.m_LocalizedAssetTables.Count; i++)
                {
                    if (item.m_LocalizedAssetTables[i].isActive && item.m_LocalizedAssetTables[i].tableAsset != null)
                    {
                        lse.StringReference.TableReference = item.m_LocalizedAssetTables[i].tableAsset.TableReference;
                        lse.RefreshString();
                        break;
                    }
                }
            }

            EditorUtility.SetDirty(item.m_TextComponent);
        }
    }
#endif

    private void SetLocale(Language language)
    {
        string localeCode = GetLocaleCode(language);
        var locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
        if (locale != null) LocalizationSettings.SelectedLocale = locale;
    }

    /// <summary>
    /// 텍스트 컴포넌트에 있는 한글을 읽어서 테이블에서 Key를 찾아 LocalizeStringEvent에 적용
    /// </summary>
    /// <summary>
    /// Text 컴포넌트에 들어있는 문자열을 "한글 Key"로 간주하고
    /// 해당 Key를 기준으로 LocalizeStringEvent를 연결.
    /// </summary>
    public void TriggerTextEvent()
    {
        if (!Application.isPlaying) return;
        if (m_LocalizableTexts == null) return;

        foreach (var item in m_LocalizableTexts)
        {
            if (item == null || item.m_TextComponent == null) continue;

            var lse = item.m_TextComponent.GetComponent<LocalizeStringEvent>();
            if (lse == null)
            {
                continue;
            }

            string currentKeyCandidate = FileNameUtils.GetTranslationKey(item.m_TextComponent.text);
            if (string.IsNullOrEmpty(currentKeyCandidate)) continue;

            bool keyFound = false;

            foreach (var tableItem in item.m_LocalizedAssetTables)
            {
                if (tableItem == null || tableItem.tableAsset == null) continue;

                var tableOp = tableItem.tableAsset.GetTableAsync();
                tableOp.WaitForCompletion();

                if (tableOp.Result is StringTable stringTable)
                {
                    // 🔑 양쪽 모두 공백 제거하여 매칭 (Text 내용과 테이블 키 모두 정규화)
                    StringTableEntry matchedEntry = null;
                    foreach (var kv in stringTable)
                    {
                        string normalizedTableKey = FileNameUtils.GetTranslationKey(kv.Value.Key);
                        if (normalizedTableKey == currentKeyCandidate)
                        {
                            matchedEntry = kv.Value;
                            break;
                        }
                    }

                    if (matchedEntry != null)
                    {
                        // ✅ 해당 Key로 LocalizeStringEvent 세팅
                        lse.StringReference = new LocalizedString
                        {
                            TableReference = tableItem.tableAsset.TableReference,
                            TableEntryReference = matchedEntry.Key // 원본 Key 사용
                        };
                        lse.RefreshString();
                        keyFound = true;
                        break;
                    }
                }

                if (keyFound) break;
            }

            if (!keyFound)
            {
                // Key not found
            }
        }
    }

    #endregion

#if UNITY_EDITOR
    #region EditorPreview
    private void OnEnable()
    {
        if (!Application.isPlaying) EditorApplication.update += EditorUpdate;
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) EditorApplication.update -= EditorUpdate;
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) EditorApplication.update -= EditorUpdate;
    }

    private void EditorUpdate()
    {
        if (!Application.isPlaying) ApplyAllTextSettings();
    }

    private void OnValidate()
    {
        AutoGenerateLanguageSettings();

        if (m_SystemLanguage)
        {
            m_SelectedLanguage = DetectSystemLanguage();
        }

        ApplyAllTextSettings();

        if (m_LocalizableTexts != null)
        {
            foreach (var lt in m_LocalizableTexts) lt?.SnapToStep();
        }

        if (m_LanguageConfigs != null)
        {
            foreach (var cfg in m_LanguageConfigs)
                cfg.m_GlobalLineSpacing = SnapToStep(cfg.m_GlobalLineSpacing);
        }
    }

    [FoldoutGroup("Dynamic Text"), PropertyOrder(1)]
    [Button("Get Localized Texts", ButtonSizes.Large), GUIColor(0.6f, 0.9f, 1f)]
    private void CollectLocalizedTexts()
    {
        if (Application.isPlaying) return;

        if (m_LocalizableTexts == null)
            m_LocalizableTexts = new List<LocalizableText>();

        var found = FindObjectsByType<LocalizeStringEvent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        m_LocalizableTexts.RemoveAll(x => x == null || x.m_TextComponent == null || x.m_TextComponent.GetComponent<LocalizeStringEvent>() == null);

        foreach (var lse in found)
        {
            var txt = lse.GetComponent<Text>();
            if (txt == null) continue;

            bool exists = m_LocalizableTexts.Exists(x => x.m_TextComponent == txt);
            if (!exists)
            {
                m_LocalizableTexts.Add(new LocalizableText { m_TextComponent = txt });
            }
        }

        EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
    #endregion
#endif

    #region Shared
    private void ApplyAllTextSettings()
    {
        if (m_LanguageConfigs == null || m_LocalizableTexts == null) return;

        var cfg = System.Array.Find(m_LanguageConfigs, x => x.language == m_SelectedLanguage);
        if (cfg == null) return;

        foreach (var item in m_LocalizableTexts)
        {
            if (item == null || item.m_TextComponent == null) continue;

            float finalSize = cfg.m_GlobalTextSize;
            float finalSpacing = cfg.m_GlobalLineSpacing;

            if (item.m_LanguageSettings != null)
            {
                var langSetting = System.Array.Find(item.m_LanguageSettings, x => x.language == m_SelectedLanguage);
                if (langSetting != null)
                {
                    finalSize *= langSetting.sizePercentage;
                    // 글로벌 스페이싱 * 라인스페이싱 값 적용
                    finalSpacing = cfg.m_GlobalLineSpacing * langSetting.lineSpacing;
                    langSetting.m_TextComponent = item.m_TextComponent;
                }
            }

            finalSize = Mathf.Round(finalSize);
            finalSpacing = SnapToStep(finalSpacing);

            item.m_TextComponent.font = cfg.m_Font != null ? cfg.m_Font : item.m_TextComponent.font;
            item.m_TextComponent.fontSize = Mathf.RoundToInt(finalSize);
            item.m_TextComponent.lineSpacing = finalSpacing;

            item.m_TextComponent.alignByGeometry = true;
            item.m_TextComponent.resizeTextForBestFit = false;
            item.m_TextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            item.m_TextComponent.verticalOverflow = VerticalWrapMode.Overflow;

            item.ApplyCurrentText();
        }
    }

    [HorizontalGroup("Setting/SaveLoadButtons"), PropertyOrder(0), Button("Save Language Setting", ButtonSizes.Large), GUIColor(0, 1, 1)]
    private void SaveLanguageSettings()
    {
        try
        {
            LanguageSettings settings = new LanguageSettings
            {
                systemLanguage = m_SystemLanguage,
                selectedLanguage = m_SelectedLanguage
            };

            string json = JsonUtility.ToJson(settings, true);
            string directory = Path.GetDirectoryName(jsonFilePath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(jsonFilePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save language settings: {e.Message}");
        }
    }

    [HorizontalGroup("Setting/SaveLoadButtons"), PropertyOrder(0), Button("Load Language Setting", ButtonSizes.Large), GUIColor(0, 1, 1)]
    private void LoadLanguageSettings()
    {
        try
        {
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                LanguageSettings settings = JsonUtility.FromJson<LanguageSettings>(json);
                if (settings != null)
                {
                    m_SystemLanguage = settings.systemLanguage;
                    m_SelectedLanguage = settings.selectedLanguage;
                }

                ApplyAllTextSettings();
            }
            else
            {
                // Language settings file not found. Using default settings.
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load language settings: {e.Message}");
        }
    }

    private Language DetectSystemLanguage()
    {
        string code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return code switch
        {
            "en" => Language.English,
            "ko" => Language.Korean,
            "ru" => Language.Russian,
            "uz" => Language.Uzbek,
            _ => Language.English
        };
    }

    private string GetLocaleCode(Language language)
    {
        return language switch
        {
            Language.English => "en",
            Language.Korean => "ko",
            Language.Russian => "ru",
            Language.Uzbek => "uz-Cyrl-UZ",
            _ => "en"
        };
    }

    private float SnapToStep(float value)
    {
        value = Mathf.Clamp(value, 0.1f, 2f);
        return Mathf.Round(value / 0.05f) * 0.05f;
    }

    private void AutoGenerateLanguageSettings()
    {
        var allLanguages = System.Enum.GetValues(typeof(Language)) as Language[];

        if (m_LanguageConfigs == null || m_LanguageConfigs.Length != allLanguages.Length)
        {
            m_LanguageConfigs = new LanguageConfig[allLanguages.Length];
            for (int i = 0; i < allLanguages.Length; i++)
            {
                if (m_LanguageConfigs[i] == null) m_LanguageConfigs[i] = new LanguageConfig();
                m_LanguageConfigs[i].language = allLanguages[i];
            }
        }

        if (m_LocalizableTexts != null)
        {
            foreach (var item in m_LocalizableTexts)
            {
                if (item.m_LanguageSettings == null || item.m_LanguageSettings.Length != allLanguages.Length)
                {
                    item.m_LanguageSettings = new LanguageTextConfig[allLanguages.Length];
                    for (int i = 0; i < allLanguages.Length; i++)
                    {
                        if (item.m_LanguageSettings[i] == null) item.m_LanguageSettings[i] = new LanguageTextConfig();
                        item.m_LanguageSettings[i].language = allLanguages[i];
                        item.m_LanguageSettings[i].sizePercentage = 1f;
                        item.m_LanguageSettings[i].lineSpacing = 1f;
                    }
                }
            }
        }
    }
    #endregion
}

