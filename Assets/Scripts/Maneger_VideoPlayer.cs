using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using MPUIKIT;
using UnityEngine.Localization.Components;
using UnityEngine.Localization;
using UnityEngine.Networking;
using Utils;

public enum VideoContentType
{
    GreekMythology,
    Story,
    MV
}

public class VideoPlayerManager : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [FoldoutGroup("Setting")] public Canvas m_MP_Cavas;
    [FoldoutGroup("Setting")] public List_Contents m_List_Contents;
    [FoldoutGroup("Setting")] public LanguageManager m_LanguageManager;
    [FoldoutGroup("Setting")] public VideoPlayer m_VideoPlayer;
    [FoldoutGroup("Setting")] public GameObject m_PlayerSetting;
    [FoldoutGroup("Setting")] public Toggle m_PlayerToggle;
    [FoldoutGroup("Setting")] public GameObject m_ProgressFill;
    [FoldoutGroup("Setting")] public Text m_TextCount;
    [FoldoutGroup("Setting")] public Text m_SubTitle;
    [FoldoutGroup("Setting")] public RectTransform rectTransform; // 슬라이더의 RectTransform
    [FoldoutGroup("Setting")] public Image progressFillImage;     // 진행 바 이미지
    [FoldoutGroup("Setting")] public GameObject m_Points;         // 기존 m_Points 필드
    [FoldoutGroup("Setting")] public string m_VideoType;         // 기존 m_Points 필드
    [FoldoutGroup("Setting")] public MPImage m_NextVideo;
    [FoldoutGroup("Setting")] public Text m_TitleText;
    [FoldoutGroup("Setting")] public Text m_Message;
    [FoldoutGroup("Setting")] public AudioSource m_AudioSource; // 영상 오디오 전용
    [FoldoutGroup("Setting")] public Text m_OverlayTextImage;
    [FoldoutGroup("Setting")] public Text m_OverlayText;
    [FoldoutGroup("Overlay Settings")]
    public OverlayTextManager overlayTextManager;

    private Image m_ProgressFillImage;
    private bool m_IsVideoPlaying;
    private List<SubtitleEntry> m_Subtitles = new List<SubtitleEntry>();
    private int m_CurrentSubtitleIndex = -1;
    private bool m_HasShownOverlay = false;
    private double m_LastVideoTime = 0.0;
    
    // Fade in/out variables
    private float fadeInDuration = 1f;
    private float fadeOutDuration = 1f;
    private CanvasGroup m_OverlayTextImageCanvasGroup;
    private CanvasGroup m_OverlayTextCanvasGroup;

    string Path = Paths.MoviePath;
    string audioBasePath = Paths.AudioPath;

    private class SubtitleEntry
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public string Text { get; set; }
    }

    private IEnumerator LoadAudioClip(string audioPath, System.Action<AudioClip> onComplete)
    {
        if (!File.Exists(audioPath))
        {
            Debug.LogError($"오디오 파일을 찾을 수 없습니다: {audioPath}");
            onComplete?.Invoke(null);
            yield break;
        }

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + audioPath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    onComplete?.Invoke(clip);
                }
                else
                {
                    Debug.LogError($"오디오 클립 로드 실패: {audioPath}");
                    onComplete?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"오디오 로드 실패: {audioPath}, 오류: {www.error}");
                onComplete?.Invoke(null);
            }
        }
    }

    private void Start()
    {
        if (m_Points == null) Debug.LogError("m_Points가 설정되지 않았습니다.");
        if (m_ProgressFill == null) Debug.LogError("m_ProgressFill이 설정되지 않았습니다.");
        if (m_SubTitle == null) Debug.LogError("m_SubTitle이 설정되지 않았습니다.");
        if (m_LanguageManager == null) Debug.LogError("m_LocalizationManager가 설정되지 않았습니다.");
        if (m_VideoPlayer == null) Debug.LogError("m_VideoPlayer가 설정되지 않았습니다.");

        m_PlayerSetting.SetActive(false);
        Reset_Progress();
        InitializeCanvasGroups();
    }

    void Update()
    {
        if (m_IsVideoPlaying && m_VideoPlayer != null && m_VideoPlayer.isPrepared && m_VideoPlayer.isPlaying)
        {
            if (m_VideoPlayer.length > 0)
            {
                float progress = (float)(m_VideoPlayer.time / m_VideoPlayer.length);
                if (m_ProgressFillImage != null)
                {
                    m_ProgressFillImage.fillAmount = Mathf.Clamp01(progress);
                }
            }

            // 오디오 동기화 (미세 조정)
            if (m_AudioSource != null && m_AudioSource.isPlaying)
            {
                float timeDiff = Mathf.Abs((float)m_VideoPlayer.time - m_AudioSource.time);
                if (timeDiff > 0.1f) // 0.1초 이상 차이면 동기화
                {
                    m_AudioSource.time = (float)m_VideoPlayer.time;
                }
            }

            UpdateSubtitles(); // 매 프레임 자막 갱신
        }
        
        // 오버레이 타이밍 체크
        CheckOverlayTiming();
    }

    private void Reset_Progress()
    {
        if (m_VideoPlayer == null)
        {
            Debug.LogError("m_Player가 설정되지 않았습니다.");
            return;
        }
        m_VideoPlayer = m_VideoPlayer.GetComponent<VideoPlayer>();
        if (m_VideoPlayer == null)
        {
            Debug.LogError("m_Player에 VideoPlayer 컴포넌트가 없습니다.");
            return;
        }

        if (m_ProgressFill != null)
        {
            m_ProgressFillImage = m_ProgressFill.GetComponent<Image>();
            if (m_ProgressFillImage != null)
            {
                m_ProgressFillImage.fillAmount = 0f;
            }
        }

        m_Subtitles.Clear();
        m_CurrentSubtitleIndex = -1;
        if (m_SubTitle != null)
        {
            m_SubTitle.text = "";
            m_SubTitle.gameObject.SetActive(true);
        }
    }

    public void Play_Story()
    {
        m_VideoType = "Story";

        if (m_List_Contents.m_StoryIndex + 1 < m_List_Contents.m_ListStory.Count)
        {
            m_NextVideo.gameObject.SetActive(true);
            m_TitleText.gameObject.SetActive(true);
            m_Message.gameObject.SetActive(false);

            m_NextVideo.sprite = m_List_Contents.m_ListStory[m_List_Contents.m_StoryIndex + 1];

            string fullName = m_List_Contents.m_ListStory[m_List_Contents.m_StoryIndex + 1].name;
            string[] name = fullName.Split('_');

            if (name.Length > 1)
                m_TitleText.text = name[1];
            else
                m_TitleText.text = fullName; // '_'가 없으면 전체 파일명 사용
            TriggerTextEvent();
        }
        else
        {
            m_NextVideo.gameObject.SetActive(false);
            m_TitleText.gameObject.SetActive(false);
            m_Message.gameObject.SetActive(true);
        }

        if (m_LanguageManager == null || string.IsNullOrEmpty(m_LanguageManager.m_SelectedLanguage.ToString()))
        {
            Debug.LogError("LocalizationManager 또는 m_Language가 설정되지 않았습니다.");
            return;
        }

        Set_Player();

        string fileName = FileNameUtils.RemoveSpaces(m_List_Contents.m_ListStory[m_List_Contents.m_StoryIndex].name);
        string videoPath = System.IO.Path.Combine(Path, "Story", fileName + ".mp4");
        string subtitlePath = System.IO.Path.Combine(Path, "Subtitles", "Story", m_LanguageManager.m_SelectedLanguage.ToString(), fileName + ".srt");
        string audioPath = System.IO.Path.Combine(audioBasePath, "Story", m_LanguageManager.m_SelectedLanguage.ToString(), fileName + ".mp3");
        string videoUrl = "file://" + videoPath;

        if (!File.Exists(videoPath))
        {
            Debug.LogError($"비디오 파일을 찾을 수 없습니다: {videoPath}");
            return;
        }

        // 오디오 로딩 시작
        StartCoroutine(LoadAudioClip(audioPath, (audioClip) => {
            if (audioClip != null && m_AudioSource != null)
            {
                m_AudioSource.clip = audioClip;
            }
        }));

        VideoPlay(videoUrl, subtitlePath);
    }

    public void Play_GreekMythology()
    {
        m_VideoType = "GreekMythology";

        if (m_List_Contents.m_GreekMythologyIndex + 1 < m_List_Contents.m_ListGreekMythology.Count)
        {
            m_NextVideo.gameObject.SetActive(true);
            m_TitleText.gameObject.SetActive(true);
            m_Message.gameObject.SetActive(false);

            m_NextVideo.sprite = m_List_Contents.m_ListGreekMythology[m_List_Contents.m_GreekMythologyIndex + 1];

            string fullName = m_List_Contents.m_ListGreekMythology[m_List_Contents.m_GreekMythologyIndex + 1].name;
            m_TitleText.text = fullName;
            TriggerTextEvent();
        }
        else
        {
            m_NextVideo.gameObject.SetActive(false);
            m_TitleText.gameObject.SetActive(false);
            m_Message.gameObject.SetActive(true);
        }

        if (m_LanguageManager == null || string.IsNullOrEmpty(m_LanguageManager.m_SelectedLanguage.ToString()))
        {
            Debug.LogError("LocalizationManager 또는 m_Language가 설정되지 않았습니다.");
            return;
        }

        Set_Player();

        string fileName = FileNameUtils.RemoveSpaces(m_List_Contents.m_ListGreekMythology[m_List_Contents.m_GreekMythologyIndex].name);
        string videoPath = System.IO.Path.Combine(Path, "GreekMythology", fileName + ".mp4");
        string subtitlePath = System.IO.Path.Combine(Path, "Subtitles", "GreekMythology", m_LanguageManager.m_SelectedLanguage.ToString(), fileName + ".srt");
        string audioPath = System.IO.Path.Combine(audioBasePath, "GreekMythology", m_LanguageManager.m_SelectedLanguage.ToString(), fileName + ".mp3");
        string videoUrl = "file://" + videoPath;

        if (!File.Exists(videoPath))
        {
            Debug.LogError($"비디오 파일을 찾을 수 없습니다: {videoPath}");
            return;
        }

        // 오디오 로딩 시작
        StartCoroutine(LoadAudioClip(audioPath, (audioClip) => {
            if (audioClip != null && m_AudioSource != null)
            {
                m_AudioSource.clip = audioClip;
            }
        }));

        VideoPlay(videoUrl, subtitlePath);
    }

    public void Play_MV()
    {
        m_VideoType = "MV";

        if (m_List_Contents.m_MVIndex + 1 < m_List_Contents.m_ListMV.Count)
        {
            m_NextVideo.gameObject.SetActive(true);
            m_TitleText.gameObject.SetActive(true);
            m_Message.gameObject.SetActive(false);

            m_NextVideo.sprite = m_List_Contents.m_ListMV[m_List_Contents.m_MVIndex + 1];

            string fullName = m_List_Contents.m_ListMV[m_List_Contents.m_MVIndex + 1].name;
            string[] name = fullName.Split('_');

            if (name.Length > 1)
                m_TitleText.text = name[1];
            else
                m_TitleText.text = fullName; // '_'가 없으면 전체 파일명 사용
            TriggerTextEvent();
        }
        else
        {
            m_NextVideo.gameObject.SetActive(false);
            m_TitleText.gameObject.SetActive(false);
            m_Message.gameObject.SetActive(true);
        }

        if (m_LanguageManager == null || string.IsNullOrEmpty(m_LanguageManager.m_SelectedLanguage.ToString()))
        {
            Debug.LogError("LocalizationManager 또는 m_Language가 설정되지 않았습니다.");
            return;
        }

        Set_Player();

        string fileName = FileNameUtils.RemoveSpaces(m_List_Contents.m_ListMV[m_List_Contents.m_MVIndex].name);
        string videoPath = System.IO.Path.Combine(Path, "MV", fileName + ".mp4");
        string subtitlePath = System.IO.Path.Combine(Path, "Subtitles", "MV", m_LanguageManager.m_SelectedLanguage.ToString(), fileName + ".srt");
        string audioPath = System.IO.Path.Combine(audioBasePath, "MV", m_LanguageManager.m_SelectedLanguage.ToString(), fileName + ".mp3");
        string videoUrl = "file://" + videoPath;

        if (!File.Exists(videoPath))
        {
            Debug.LogError($"비디오 파일을 찾을 수 없습니다: {videoPath}");
            return;
        }

        // 오디오 로딩 시작
        StartCoroutine(LoadAudioClip(audioPath, (audioClip) => {
            if (audioClip != null && m_AudioSource != null)
            {
                m_AudioSource.clip = audioClip;
            }
        }));

        VideoPlay(videoUrl, subtitlePath);
    }

    private IEnumerator AutoPlayNextVideoAfterDelay(float delay)
    {
        float timer = delay;
        while (timer > 0)
        {
            m_TextCount.text = $"{timer:F0}";
            yield return new WaitForSeconds(1f);
            timer -= 1f;
        }

        if (m_PlayerSetting != null)
        {
            m_PlayerSetting.SetActive(false);
        }

        bool hasNext = false;

        if (m_VideoType == "GreekMythology")
        {
            if (m_List_Contents.m_ListGreekMythology != null && m_List_Contents.m_ListGreekMythology.Count > 0 && m_List_Contents.m_GreekMythologyIndex < m_List_Contents.m_ListGreekMythology.Count - 1)
            {
                m_List_Contents.m_GreekMythologyIndex++;
                m_List_Contents.Set_Index();
                Play_GreekMythology();
                hasNext = true;
            }
        }
        if (m_VideoType == "Story")
        {
            if (m_List_Contents.m_ListStory != null && m_List_Contents.m_ListStory.Count > 0 && m_List_Contents.m_StoryIndex < m_List_Contents.m_ListStory.Count - 1)
            {
                m_List_Contents.m_StoryIndex++;
                m_List_Contents.Set_Index();
                Play_Story();
                hasNext = true;
            }
        }
        if (m_VideoType == "MV")
        {
            if (m_List_Contents.m_ListMV != null && m_List_Contents.m_ListMV.Count > 0 && m_List_Contents.m_MVIndex < m_List_Contents.m_ListMV.Count - 1)
            {
                m_List_Contents.m_MVIndex++;
                m_List_Contents.Set_Index();
                Play_MV();
                hasNext = true;
            }
        }

        if (!hasNext)
        {
            Close_Player();
        }
    }

    public void Set_Player()
    {
        if (m_Points == null)
        {
            Debug.LogError("m_Points가 설정되지 않았습니다.");
            return;
        }

        foreach (BoxCollider collider in m_Points.GetComponentsInChildren<BoxCollider>())
        {
            collider.enabled = false;
        }

        foreach (Image image in m_Points.GetComponentsInChildren<Image>())
        {
            if (m_SubTitle != null && image != m_SubTitle.GetComponent<Image>())
            {
                image.enabled = false;
            }
        }

        m_VideoPlayer.gameObject.SetActive(true);
        m_MP_Cavas.sortingOrder = -1;

        if (m_SubTitle != null)
        {
            m_SubTitle.gameObject.SetActive(true);
        }

        // 배경음악 페이드 아웃 후 일시정지
        if (m_List_Contents != null && m_List_Contents.m_Maneger_BGM != null)
        {
            m_List_Contents.m_Maneger_BGM.FadeOutAndPause();
        }
    }

    public void Close_Player()
    {
        if (m_Points == null)
        {
            Debug.LogError("m_Points가 설정되지 않았습니다.");
            return;
        }

        foreach (BoxCollider collider in m_Points.GetComponentsInChildren<BoxCollider>())
        {
            collider.enabled = true;
        }

        foreach (Image image in m_Points.GetComponentsInChildren<Image>())
        {
            image.enabled = true;
        }

        m_VideoPlayer.gameObject.SetActive(false);
        m_MP_Cavas.sortingOrder = 1;

        if (m_VideoPlayer != null)
        {
            m_VideoPlayer.Stop();
            m_IsVideoPlaying = false;
        }

        if (m_ProgressFillImage != null)
        {
            m_ProgressFillImage.fillAmount = 0f;
        }

        m_PlayerSetting.SetActive(false);

        m_Subtitles.Clear();
        m_CurrentSubtitleIndex = -1;
        if (m_SubTitle != null)
        {
            m_SubTitle.text = "";
            m_SubTitle.gameObject.SetActive(false);
        }
        m_VideoPlayer.url = null;

        // 오디오 정리
        if (m_AudioSource != null)
        {
            m_AudioSource.Stop();
            m_AudioSource.clip = null;
        }

        // 배경음악 페이드 인 후 재생
        if (m_List_Contents != null && m_List_Contents.m_Maneger_BGM != null)
        {
            m_List_Contents.m_Maneger_BGM.FadeInAndPlay();
        }
    }

    private void VideoPlay(string videoPath, string subtitlePath)
    {
        if (m_VideoPlayer == null)
        {
            Debug.LogError("m_VideoPlayer가 설정되지 않았습니다.");
            return;
        }

        m_VideoPlayer.source = VideoSource.Url;
        m_VideoPlayer.url = videoPath;
        m_VideoPlayer.prepareCompleted -= OnVideoPrepared;
        m_VideoPlayer.prepareCompleted += OnVideoPrepared;
        m_VideoPlayer.loopPointReached -= OnVideoEnd;
        m_VideoPlayer.loopPointReached += OnVideoEnd;

        m_VideoPlayer.Prepare();
        m_IsVideoPlaying = false;

        if (m_ProgressFillImage != null)
        {
            m_ProgressFillImage.fillAmount = 0f;
        }

        LoadSubtitles(subtitlePath);
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        m_VideoPlayer.Play();
        m_IsVideoPlaying = true;
        m_CurrentSubtitleIndex = -1;

        // 오디오 동기 재생
        if (m_AudioSource != null && m_AudioSource.clip != null)
        {
            m_AudioSource.Play();
        }

        if (m_SubTitle != null)
        {
            m_SubTitle.gameObject.SetActive(true);
            m_SubTitle.text = "";
        }

        // 첫 프레임에서도 자막 바로 표시
        if (m_Subtitles.Count > 0)
        {
            UpdateSubtitles(forceUpdate: true);
        }
        
        // 오버레이 초기화
        ResetOverlay();
    }

    private void UpdateSubtitles(bool forceUpdate = false)
    {
        if (m_VideoPlayer == null || m_Subtitles.Count == 0 || m_SubTitle == null)
            return;

        double currentTime = m_VideoPlayer.time;

        int newIndex = -1;
        for (int i = 0; i < m_Subtitles.Count; i++)
        {
            if (currentTime >= m_Subtitles[i].StartTime && currentTime <= m_Subtitles[i].EndTime)
            {
                newIndex = i;
                break;
            }
        }

        if (newIndex != m_CurrentSubtitleIndex || forceUpdate)
        {
            m_CurrentSubtitleIndex = newIndex;
            m_SubTitle.text = newIndex >= 0 ? m_Subtitles[newIndex].Text : "";
        }
    }

    private void OnVideoEnd(VideoPlayer source)
    {
        m_IsVideoPlaying = false;

        if (m_PlayerSetting != null)
        {
            m_PlayerSetting.SetActive(true);
        }

        m_VideoPlayer.Stop();

        m_Subtitles.Clear();
        m_CurrentSubtitleIndex = -1;
        if (m_SubTitle != null)
        {
            m_SubTitle.text = "";
            m_SubTitle.gameObject.SetActive(false);
        }

        if (m_PlayerToggle != null && m_PlayerToggle.isOn)
        {
            StartCoroutine(AutoPlayNextVideoAfterDelay(3f));
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SeekVideo(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        SeekVideo(eventData);
    }

    private void SeekVideo(PointerEventData eventData)
    {
        if (m_VideoPlayer == null || !m_VideoPlayer.canSetTime || m_VideoPlayer.length <= 0)
            return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            float width = rectTransform.rect.width;
            float normalized = Mathf.Clamp01((localPoint.x + width * 0.5f) / width);

            progressFillImage.fillAmount = normalized;
            m_VideoPlayer.time = normalized * m_VideoPlayer.length;

            // 오디오 정확한 시크
            if (m_AudioSource != null && m_AudioSource.clip != null)
            {
                m_AudioSource.time = (float)m_VideoPlayer.time;
            }

            UpdateSubtitles(forceUpdate: true);
        }
    }

    private void LoadSubtitles(string subtitlePath)
    {
        m_Subtitles.Clear();
        m_CurrentSubtitleIndex = -1;
        if (m_SubTitle != null)
        {
            m_SubTitle.text = "";
        }

        if (!File.Exists(subtitlePath))
        {
            Debug.LogWarning($"자막 파일을 찾을 수 없습니다: {subtitlePath}");
            return;
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(subtitlePath, System.Text.Encoding.UTF8);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"자막 파일 읽기 실패: {subtitlePath}, 오류: {e.Message}");
            return;
        }

        int index = 0;
        while (index < lines.Length)
        {
            if (!int.TryParse(lines[index].Trim(), out _))
            {
                index++;
                continue;
            }

            index++;
            if (index >= lines.Length) break;

            string timeLine = lines[index].Trim();
            var match = Regex.Match(timeLine, @"(\d{2}:\d{2}:\d{2},\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2},\d{3})");
            if (!match.Success)
            {
                index++;
                continue;
            }

            string startTimeStr = match.Groups[1].Value;
            string endTimeStr = match.Groups[2].Value;

            double startTime = ParseTime(startTimeStr);
            double endTime = ParseTime(endTimeStr);

            index++;
            string subtitleText = "";
            while (index < lines.Length && !string.IsNullOrWhiteSpace(lines[index]))
            {
                subtitleText += lines[index].Trim() + "\n";
                index++;
            }

            m_Subtitles.Add(new SubtitleEntry
            {
                StartTime = startTime,
                EndTime = endTime,
                Text = subtitleText.Trim()
            });

            index++;
        }
    }

    private double ParseTime(string timeStr)
    {
        string[] parts = timeStr.Split(':');
        string[] secondsParts = parts[2].Split(',');

        int hours = int.Parse(parts[0]);
        int minutes = int.Parse(parts[1]);
        int seconds = int.Parse(secondsParts[0]);
        int milliseconds = int.Parse(secondsParts[1]);

        return hours * 3600.0 + minutes * 60.0 + seconds + milliseconds / 1000.0;
    }

    public void TriggerTextEvent()
    {
        if (m_TitleText == null || string.IsNullOrEmpty(m_TitleText.text))
        {
            Debug.LogWarning("m_StoryText is null or empty.");
            return;
        }

        var localizeStringEvent = m_TitleText.GetComponent<LocalizeStringEvent>();
        if (localizeStringEvent == null)
        {
            Debug.LogWarning("LocalizeStringEvent component is missing on m_StoryText.");
            return;
        }

        LocalizedString localizedString = TitleLocalizationMap.GetLocalizedString(m_TitleText.text);
        if (localizedString != null)
        {
            localizeStringEvent.enabled = false;
            localizeStringEvent.StringReference = localizedString;
            localizeStringEvent.enabled = true;
        }
        else
        {
            Debug.LogWarning($"LocalizedString을 찾을 수 없습니다: {m_TitleText.text}");
        }
    }

    private void CheckOverlayTiming()
    {
        if (!m_IsVideoPlaying || m_VideoPlayer == null || m_HasShownOverlay)
            return;

        double currentTime = m_VideoPlayer.time;

        // 2초 시점을 지나갔는지 확인
        if (currentTime >= 2.0 && m_LastVideoTime < 2.0)
        {
            StartCoroutine(ShowOverlayWithFade());
            m_HasShownOverlay = true;
        }

        m_LastVideoTime = currentTime;
    }

    private void InitializeCanvasGroups()
    {
        // CanvasGroup 컴포넌트 가져오기 또는 추가
        if (m_OverlayTextImage != null)
        {
            m_OverlayTextImageCanvasGroup = m_OverlayTextImage.GetComponent<CanvasGroup>();
            if (m_OverlayTextImageCanvasGroup == null)
            {
                m_OverlayTextImageCanvasGroup = m_OverlayTextImage.gameObject.AddComponent<CanvasGroup>();
            }
            m_OverlayTextImageCanvasGroup.alpha = 0f;
            m_OverlayTextImage.gameObject.SetActive(false);
        }

        if (m_OverlayText != null)
        {
            m_OverlayTextCanvasGroup = m_OverlayText.GetComponent<CanvasGroup>();
            if (m_OverlayTextCanvasGroup == null)
            {
                m_OverlayTextCanvasGroup = m_OverlayText.gameObject.AddComponent<CanvasGroup>();
            }
            m_OverlayTextCanvasGroup.alpha = 0f;
            m_OverlayText.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeInOverlayTexts()
    {
        if (m_OverlayTextImageCanvasGroup != null && m_OverlayTextCanvasGroup != null)
        {
            m_OverlayTextImage.gameObject.SetActive(true);
            m_OverlayText.gameObject.SetActive(true);

            float elapsedTime = 0f;

            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);

                m_OverlayTextImageCanvasGroup.alpha = alpha;
                m_OverlayTextCanvasGroup.alpha = alpha;

                yield return null;
            }

            // 완전히 보이도록 설정
            m_OverlayTextImageCanvasGroup.alpha = 1f;
            m_OverlayTextCanvasGroup.alpha = 1f;
        }
    }

    private IEnumerator FadeOutOverlayTexts()
    {
        if (m_OverlayTextImageCanvasGroup != null && m_OverlayTextCanvasGroup != null)
        {
            float elapsedTime = 0f;
            float startAlpha = m_OverlayTextImageCanvasGroup.alpha;

            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutDuration);

                m_OverlayTextImageCanvasGroup.alpha = alpha;
                m_OverlayTextCanvasGroup.alpha = alpha;

                yield return null;
            }

            // 완전히 숨기고 비활성화
            m_OverlayTextImageCanvasGroup.alpha = 0f;
            m_OverlayTextCanvasGroup.alpha = 0f;
            m_OverlayTextImage.gameObject.SetActive(false);
            m_OverlayText.gameObject.SetActive(false);
        }
    }

    private IEnumerator ShowOverlayWithFade()
    {
        string currentFileName = GetCurrentVideoFileName();
        string cleanFileName = FileNameUtils.RemoveSpaces(currentFileName);
        string prefix = GetVideoTypePrefix();
        string customText = GetCustomTextByVideoType();

        m_OverlayTextImage.text = cleanFileName;
        m_OverlayText.text = customText;

        yield return StartCoroutine(FadeInOverlayTexts());

        yield return new WaitForSeconds(3f);

        yield return StartCoroutine(FadeOutOverlayTexts());
    }

    private string GetCustomTextByVideoType()
    {
        string currentTypeString = m_VideoType;
        VideoContentType currentType;

        // 문자열을 Enum으로 변환
        if (System.Enum.TryParse<VideoContentType>(currentTypeString, out currentType))
        {
            return overlayTextManager?.GetCustomText(currentType) ?? "";
        }

        return "";
    }

    private void ApplyLocalizationToOverlayText(string text)
    {
        if (m_OverlayTextImage == null) return;

        var localizeStringEvent = m_OverlayTextImage.GetComponent<LocalizeStringEvent>();
        if (localizeStringEvent == null) return;
        
        LocalizedString localizedString = TitleLocalizationMap.GetLocalizedString(text);
        if (localizedString != null)
        {
            localizeStringEvent.enabled = false;
            localizeStringEvent.StringReference = localizedString;
            localizeStringEvent.enabled = true;
        }
        else
        {
            // 번역 없으면 원본 텍스트 사용
            m_OverlayTextImage.text = text;
        }
    }
    
    private string GetCurrentVideoFileName()
    {
        switch (m_VideoType)
        {
            case "GreekMythology":
                return m_List_Contents.m_ListGreekMythology[m_List_Contents.m_GreekMythologyIndex].name;
            case "Story":
                return m_List_Contents.m_ListStory[m_List_Contents.m_StoryIndex].name;
            case "MV":
                return m_List_Contents.m_ListMV[m_List_Contents.m_MVIndex].name;
            default:
                return "";
        }
    }
    
    private string GetVideoTypePrefix()
    {
        switch (m_VideoType)
        {
            case "GreekMythology": return "그리스신화";
            case "Story": return "스토리";
            case "MV": return "뮤직비디오";
            default: return "";
        }
    }
    
    private IEnumerator HideOverlayAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (m_OverlayTextImage != null)
            m_OverlayTextImage.gameObject.SetActive(false);
        if (m_OverlayText != null)
            m_OverlayText.gameObject.SetActive(false);
    }
    
    private void ResetOverlay()
    {
        m_HasShownOverlay = false;
        m_LastVideoTime = 0.0;
        
        if (m_OverlayTextImage != null)
            m_OverlayTextImage.gameObject.SetActive(false);
        if (m_OverlayText != null)
            m_OverlayText.gameObject.SetActive(false);
    }
}