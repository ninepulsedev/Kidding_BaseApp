using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.IO;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))] // 필수 컴포넌트
public class Manager_BGM : MonoBehaviour
{
    // 게임 설정 필드
    [FoldoutGroup("Setting")] public AudioSource m_AudioSource;
    [FoldoutGroup("Setting")][Range(0, 1)] public float m_Volume = 0.2f; // 기본 볼륨
    [FoldoutGroup("Setting")][Range(0.1f, 5f)] public float m_FadeDuration = 1f; // 페이드 시간 (초)
    
    // 외부 음악 폴더 설정
    [FoldoutGroup("Setting"), PropertyOrder(10)] 
    [FolderPath(AbsolutePath = true)]
    private readonly string m_ExternalMusicPath = Paths.ExternalMusicPath;  // 외부 폴더 경로
    
    [FoldoutGroup("Setting"), PropertyOrder(11)] 
    public List<AudioClip> m_ListAudio;
    
    [FoldoutGroup("Setting"), PropertyOrder(20)] 
    [Range(0.01f, 0.1f)]
    public float m_LoadDelayPerFile = 0.05f;  // 파일당 로드 간격 (프레임 드롭 방지)

    // 내부 상태 변수
    private List<AudioClip> availableClips; // 사용 가능한 클립 리스트
    private AudioClip currentClip; // 현재 재생 중인 클립
    private float clipTimer; // 현재 클립의 경과 시간 추적
    private bool isPlaying; // 재생 상태
    private bool isLoadingAudio = false;  // 로딩 중 플래그

    // === [디버그 설정] ===
    [FoldoutGroup("Debug"), ToggleLeft, PropertyOrder(2)] public bool m_EnableDebugLogs = false;

    // 디버그 로그 헬퍼 메서드
    private void DebugLog(string message)
    {
        if (m_EnableDebugLogs)
            return;
    }

    private void DebugLogWarning(string message)
    {
        if (m_EnableDebugLogs)
            return;
    }

    // 외부 폴더에서 오디오 로드
    private IEnumerator LoadAudioFromExternalFolder()
    {
        if (string.IsNullOrEmpty(m_ExternalMusicPath))
        {
            yield break;
        }

        if (!Directory.Exists(m_ExternalMusicPath))
        {
            yield break;
        }

        isLoadingAudio = true;
        m_ListAudio.Clear();

        // 지원하는 오디오 포맷
        string[] extensions = { "*.mp3", "*.wav", "*.ogg" };
        List<string> audioFiles = new List<string>();

        foreach (string ext in extensions)
        {
            audioFiles.AddRange(Directory.GetFiles(m_ExternalMusicPath, ext, SearchOption.TopDirectoryOnly));
        }

        if (audioFiles.Count == 0)
        {
            isLoadingAudio = false;
            yield break;
        }

        DebugLog($"Found {audioFiles.Count} audio files. Loading...");

        // 프레임 드롭 방지를 위해 파일당 간격을 두고 로드
        int loadedCount = 0;
        foreach (string filePath in audioFiles)
        {
            yield return StartCoroutine(LoadAudioClipFromFile(filePath));
            loadedCount++;
            
            // 프레임 드롭 방지: 일정 간격마다 프레임 양보
            if (loadedCount % 5 == 0)
            {
                yield return new WaitForSeconds(m_LoadDelayPerFile);
            }
        }

        isLoadingAudio = false;
        DebugLog($"Audio loading complete. Total clips: {m_ListAudio.Count}");

        // 로드 완료 후 자동 재생 (기존 동작 유지)
        if (m_ListAudio.Count > 0)
        {
            Play();
        }
    }

    private IEnumerator LoadAudioClipFromFile(string filePath)
    {
        string url = "file://" + filePath;
        AudioType audioType = AudioType.MPEG;  // 기본값

        // 파일 확장자에 따라 AudioType 결정
        string extension = Path.GetExtension(filePath).ToLower();
        switch (extension)
        {
            case ".mp3":
                audioType = AudioType.MPEG;
                break;
            case ".wav":
                audioType = AudioType.WAV;
                break;
            case ".ogg":
                audioType = AudioType.OGGVORBIS;
                break;
        }

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            // 스트리밍 모드로 메모리 효율성 향상
            DownloadHandlerAudioClip downloadHandler = www.downloadHandler as DownloadHandlerAudioClip;
            if (downloadHandler != null)
            {
                downloadHandler.streamAudio = true;  // 스트리밍 모드 활성화
            }
            
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    clip.name = Path.GetFileNameWithoutExtension(filePath);
                    m_ListAudio.Add(clip);
                    DebugLog($"Loaded: {clip.name}");
                }
            }
            else
            {
                // Failed to load audio
            }
        }
        
        // 파일 로드 후 프레임 양보 (버벅임 방지)
        yield return null;
    }

    [HorizontalGroup("Group00")]
    [Button("Stop", ButtonSizes.Large), GUIColor(0, 1, 1)]
    public void Stop()
    {
        if (m_AudioSource != null)
        {
            m_AudioSource.Stop();
            isPlaying = false;
            clipTimer = 0f;
            currentClip = null;
        }
        else
        {
            // AudioSource is not assigned or failed to initialize
        }
    }

    [HorizontalGroup("Group00")]
    [Button("Pause", ButtonSizes.Large), GUIColor(0, 1, 1)]
    public void Pause()
    {
        if (m_AudioSource != null)
        {
            if (m_AudioSource.isPlaying)
            {
                m_AudioSource.Pause();
                isPlaying = false;
            }
            else
            {
                DebugLogWarning("BGM is not playing, cannot pause");
            }
        }
        else
        {
            // AudioSource is not assigned or failed to initialize
        }
    }

    [HorizontalGroup("Group00")]
    [Button("Play", ButtonSizes.Large), GUIColor(0, 1, 1)]
    public void Play()
    {
        if (m_AudioSource == null)
        {
            return;
        }

        // 외부 오디오 로딩 중이면 대기
        if (isLoadingAudio)
        {
            DebugLogWarning("Audio files are still loading. Please wait...");
            return;
        }

        if (m_ListAudio == null || m_ListAudio.Count == 0)
        {
            return;
        }

        // 이미 재생 중일 때 Play 버튼 클릭: 새 클립 재생
        if (m_AudioSource.isPlaying)
        {
            m_AudioSource.Stop();
            isPlaying = false;
            clipTimer = 0f;
            PlayRandomClip();
            return;
        }

        // 일시정지 상태: 재개 재생
        if (!m_AudioSource.isPlaying && m_AudioSource.clip != null && currentClip != null)
        {
            m_AudioSource.UnPause();
            isPlaying = true;
            return;
        }

        // availableClips 초기화
        if (availableClips == null || availableClips.Count == 0)
        {
            availableClips = new List<AudioClip>(m_ListAudio);
        }

        // 랜덤 클립 재생
        PlayRandomClip();
    }

    // 페이드 아웃 후 일시정지
    public void FadeOutAndPause()
    {
        if (m_AudioSource != null && m_AudioSource.isPlaying)
        {
            StartCoroutine(FadeOutCoroutine());
        }
    }

    // 페이드 인 후 재생
    public void FadeInAndPlay()
    {
        if (m_AudioSource != null)
        {
            StartCoroutine(FadeInCoroutine());
        }
    }

    private IEnumerator FadeOutCoroutine()
    {
        if (m_AudioSource == null) yield break;

        float startVolume = m_AudioSource.volume;
        float fadeDuration = m_FadeDuration;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            m_AudioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        m_AudioSource.volume = 0f;
        m_AudioSource.Pause();
        isPlaying = false;
    }

    private IEnumerator FadeInCoroutine()
    {
        if (m_AudioSource == null) yield break;

        // 일시정지 상태에서 재개
        if (!m_AudioSource.isPlaying && m_AudioSource.clip != null && currentClip != null)
        {
            m_AudioSource.UnPause();
            isPlaying = true;
        }
        else
        {
            // 재생 중이 아니면 Play 호출
            Play();
            yield return new WaitForSeconds(0.1f); // 재생 시작 대기
        }

        if (m_AudioSource.isPlaying)
        {
            float targetVolume = m_Volume;
            float fadeDuration = m_FadeDuration;
            float elapsed = 0f;
            m_AudioSource.volume = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                m_AudioSource.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }

            m_AudioSource.volume = targetVolume;
        }
    }

    // 초기화
    private void Start()
    {
        // m_AudioSource 초기화
        m_AudioSource = GetComponent<AudioSource>();
        if (m_AudioSource == null)
        {
            m_AudioSource = gameObject.AddComponent<AudioSource>();
        }
        // AudioSource 설정
        m_AudioSource.playOnAwake = false;
        m_AudioSource.volume = 1f;
        m_AudioSource.mute = false;

        availableClips = new List<AudioClip>();
        isPlaying = false;
        clipTimer = 0f;

        // 외부 폴더에서 오디오 로드
        StartCoroutine(LoadAudioFromExternalFolder());
    }

    // 매 프레임 업데이트
    private void Update()
    {
        if (isPlaying && m_AudioSource != null && currentClip != null)
        {
            clipTimer += Time.deltaTime;
            // 클립이 끝났는지 확인
            if (clipTimer >= currentClip.length)
            {
                PlayRandomClip();
            }
        }
    }

    // 랜덤 클립 재생
    private void PlayRandomClip()
    {
        if (availableClips.Count == 0)
        {
            availableClips = new List<AudioClip>(m_ListAudio);
        }

        int randomIndex = Random.Range(0, availableClips.Count);
        currentClip = availableClips[randomIndex];
        availableClips.RemoveAt(randomIndex);

        if (currentClip == null)
        {
            return;
        }

        m_AudioSource.clip = currentClip;
        m_AudioSource.volume = m_Volume;
        m_AudioSource.Play();
        isPlaying = true;
        clipTimer = 0f;
    }
}
