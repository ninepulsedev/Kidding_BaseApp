using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine.UI;
using MPUIKIT;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class List_Contents : MonoBehaviour
{
    [FoldoutGroup("Setting")] public LanguageManager m_LanguageManager;
    [FoldoutGroup("Setting")] public Maneger_UI m_Maneger_UI;
    [FoldoutGroup("Setting")] public Manager_BGM m_Maneger_BGM;
    [FoldoutGroup("Setting")] public GameObject m_Cover;

    [FoldoutGroup("GreekMythology")] public Text m_GreekMythologyText;
    [FoldoutGroup("GreekMythology")] public Text m_GreekMythologyCountText;
    [FoldoutGroup("GreekMythology")] public int m_GreekMythologyIndex = 0;
    [FoldoutGroup("GreekMythology")] public MPImage m_ThumnailGreekMythology;
    [FoldoutGroup("GreekMythology")] public List<Sprite> m_ListGreekMythology;

    [FoldoutGroup("Story")] public Text m_StoryText;
    [FoldoutGroup("Story")] public Text m_StoryPartsText;
    [FoldoutGroup("Story")] public Text m_StoryCountText;
    [FoldoutGroup("Story")] public int m_StoryIndex = 0;
    [FoldoutGroup("Story")] public MPImage m_ThumnailStory;
    [FoldoutGroup("Story")] public List<Sprite> m_ListStory;

    [FoldoutGroup("MV")] public Text m_MVText;
    [FoldoutGroup("MV")] public Text m_MVCountText;
    [FoldoutGroup("MV")] public int m_MVIndex = 0;
    [FoldoutGroup("MV")] public MPImage m_ThumnailMV;
    [FoldoutGroup("MV")] public List<Sprite> m_ListMV;

    [FoldoutGroup("GameTouch")] public Text m_GameTouchCountText;
    [FoldoutGroup("GameTouch")] public int m_GameTouchIndex = 0;
    [FoldoutGroup("GameTouch")] public MPImage m_ThumnailGameTouch;
    [FoldoutGroup("GameTouch")] public List<Sprite> m_ListGameTouch;

    [FoldoutGroup("GameMotion")] public Text m_GameMotionCountText;
    [FoldoutGroup("GameMotion")] public int m_GameMotionIndex = 0;
    [FoldoutGroup("GameMotion")] public MPImage m_ThumnailGameMotion;
    [FoldoutGroup("GameMotion")] public List<Sprite> m_ListGameMotion;

    private readonly string Path_Touch = Paths.GameTouchPath;
    private readonly string Path_Motion = Paths.GameMotionPath;

    private readonly string Path_GreekMythologyImage = Paths.ImageGreekMythologyPath;
    private readonly string Path_MVImage = Paths.ImageMVPath;
    private readonly string Path_StoryImage = Paths.ImageStoryPath;
    private readonly string Path_TouchImage = Paths.ImageTouchPath;
    private readonly string Path_MotionImage = Paths.ImageMotionPath;

    private List<Sprite> LoadImagesFromDirectory(string directoryPath)
    {
        List<Sprite> spriteList = new List<Sprite>();

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Debug.LogWarning($"이미지 디렉토리가 존재하지 않습니다: {directoryPath}");
                return spriteList;
            }

            string[] supportedExtensions = { ".png", ".jpg", ".jpeg", ".PNG", ".JPG", ".JPEG" };
            List<string> imageFiles = new List<string>();

            foreach (string extension in supportedExtensions)
            {
                string[] files = Directory.GetFiles(directoryPath, $"*{extension}", SearchOption.TopDirectoryOnly);
                imageFiles.AddRange(files);
            }

            // 중복 제거 후 파일명으로 정렬
            imageFiles = imageFiles.Distinct().OrderBy(f => Path.GetFileName(f)).ToList();

            foreach (string filePath in imageFiles)
            {
                try
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    Texture2D texture = new Texture2D(2, 2);
                    
                    if (texture.LoadImage(fileData))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                        sprite.name = fileName;
                        spriteList.Add(sprite);
                    }
                    else
                    {
                        Debug.LogWarning($"이미지 로드 실패: {filePath}");
                        UnityEngine.Object.Destroy(texture);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"이미지 파일 로드 중 오류 발생 ({filePath}): {e.Message}");
                }
            }

            Debug.Log($"이미지 로드 완료: {directoryPath} ({spriteList.Count}개)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"디렉토리 처리 중 오류 발생 ({directoryPath}): {e.Message}");
        }

        return spriteList;
    }

    private void InitializeImageLists()
    {
        // 리스트가 null이면 초기화
        if (m_ListGreekMythology == null)
            m_ListGreekMythology = new List<Sprite>();
        if (m_ListStory == null)
            m_ListStory = new List<Sprite>();
        if (m_ListMV == null)
            m_ListMV = new List<Sprite>();
        if (m_ListGameTouch == null)
            m_ListGameTouch = new List<Sprite>();
        if (m_ListGameMotion == null)
            m_ListGameMotion = new List<Sprite>();

        // 기존 리스트 비우기
        m_ListGreekMythology.Clear();
        m_ListStory.Clear();
        m_ListMV.Clear();
        m_ListGameTouch.Clear();
        m_ListGameMotion.Clear();

        // 각 디렉토리에서 이미지 로드
        m_ListGreekMythology = LoadImagesFromDirectory(Path_GreekMythologyImage);
        m_ListStory = LoadImagesFromDirectory(Path_StoryImage);
        m_ListMV = LoadImagesFromDirectory(Path_MVImage);
        m_ListGameTouch = LoadImagesFromDirectory(Path_TouchImage);
        m_ListGameMotion = LoadImagesFromDirectory(Path_MotionImage);
    }

    void Awake()
    {
        InitializeImageLists();
    }

    void Start()
    {
        Set_AllIndex();
        m_LanguageManager.TriggerTextEvent();
    }
    public void Set_AllIndex()
    {
        Set_Index();
        Set_StoryCount();
        Set_MVCount();
        Set_GameTouchCount();
        Set_GameMotionCount();
    }
    public void Set_Index()
    {
        if (m_ListGreekMythology != null && m_ListGreekMythology.Count > 0)
        {
            m_GreekMythologyIndex = Mathf.Clamp(m_GreekMythologyIndex, 0, m_ListGreekMythology.Count - 1);
            if (m_ThumnailGreekMythology != null)
            {
                m_ThumnailGreekMythology.sprite = m_ListGreekMythology[m_GreekMythologyIndex];
            }
            m_GreekMythologyText.text = m_ListGreekMythology[m_GreekMythologyIndex].name;
        }

        if (m_ListStory != null && m_ListStory.Count > 0)
        {
            m_StoryIndex = Mathf.Clamp(m_StoryIndex, 0, m_ListStory.Count - 1);
            if (m_ThumnailStory != null)
            {
                m_ThumnailStory.sprite = m_ListStory[m_StoryIndex];
            }
            string[] result = m_ListStory[m_StoryIndex].name.Split('_');
            if (result.Length > 1)
                m_StoryText.text = result[1];
            if (result.Length > 2)
                m_StoryPartsText.text = result[2];
        }

        if (m_ListMV != null && m_ListMV.Count > 0)
        {
            m_MVIndex = Mathf.Clamp(m_MVIndex, 0, m_ListMV.Count - 1);
            if (m_ThumnailMV != null)
            {
                m_ThumnailMV.sprite = m_ListMV[m_MVIndex];
            }
            string[] result = m_ListMV[m_MVIndex].name.Split('_');
            if (result.Length > 1)
                m_MVText.text = result[1];
        }

        if (m_ListGameTouch != null && m_ListGameTouch.Count > 0)
        {
            m_GameTouchIndex = Mathf.Clamp(m_GameTouchIndex, 0, m_ListGameTouch.Count - 1);
            if (m_ThumnailGameTouch != null)
            {
                m_ThumnailGameTouch.sprite = m_ListGameTouch[m_GameTouchIndex];
            }
        }

        if (m_ListGameMotion != null && m_ListGameMotion.Count > 0)
        {
            m_GameMotionIndex = Mathf.Clamp(m_GameMotionIndex, 0, m_ListGameMotion.Count - 1);
            if (m_ThumnailGameMotion != null)
            {
                m_ThumnailGameMotion.sprite = m_ListGameMotion[m_GameMotionIndex];
            }
        }

        // 모든 텍스트 업데이트 후 한 번만 언어 변경 적용
        if (m_LanguageManager != null)
        {
            m_LanguageManager.TriggerTextEvent();
        }
    }

    public void DecreaseIndex_GreekMythology()
    {
        if (m_ListGreekMythology == null || m_ListGreekMythology.Count == 0) return;
        m_GreekMythologyIndex--;
        if (m_GreekMythologyIndex < 0) m_GreekMythologyIndex = m_ListGreekMythology.Count - 1;
        Set_Index();
        Set_GreekMythologyCount();
        m_Maneger_UI.SavePageIndexToFile();
    }

    public void IncreaseIndex_GreekMythology()
    {
        if (m_ListGreekMythology == null || m_ListGreekMythology.Count == 0) return;
        m_GreekMythologyIndex++;
        if (m_GreekMythologyIndex >= m_ListGreekMythology.Count) m_GreekMythologyIndex = 0;
        Set_Index();
        Set_GreekMythologyCount();
        m_Maneger_UI.SavePageIndexToFile();
    }

    public void DecreaseIndex_Story()
    {
        if (m_ListStory == null || m_ListStory.Count == 0) return;
        m_StoryIndex--;
        if (m_StoryIndex < 0) m_StoryIndex = m_ListStory.Count - 1;
        Set_Index();
        Set_StoryCount();
        m_Maneger_UI.SavePageIndexToFile(); 
    }

    public void IncreaseIndex_Story()
    {
        if (m_ListStory == null || m_ListStory.Count == 0) return;
        m_StoryIndex++;
        if (m_StoryIndex >= m_ListStory.Count) m_StoryIndex = 0;
        Set_Index();
        Set_StoryCount();
        m_Maneger_UI.SavePageIndexToFile();
    }

    public void DecreaseIndex_MV()
    {
        if (m_ListMV == null || m_ListMV.Count == 0) return;
        m_MVIndex--;
        if (m_MVIndex < 0) m_MVIndex = m_ListMV.Count - 1;
        Set_Index();
        Set_MVCount();
        m_Maneger_UI.SavePageIndexToFile();
    }

    public void IncreaseIndex_MV()
    {
        if (m_ListMV == null || m_ListMV.Count == 0) return;
        m_MVIndex++;
        if (m_MVIndex >= m_ListMV.Count) m_MVIndex = 0;
        Set_Index();
        Set_MVCount();
        m_Maneger_UI.SavePageIndexToFile();
    }

    public void DecreaseIndex_GameTouch()
    {
        if (m_ListGameTouch == null || m_ListGameTouch.Count == 0) return;
        m_GameTouchIndex--;
        if (m_GameTouchIndex < 0) m_GameTouchIndex = m_ListGameTouch.Count - 1;
        Set_Index();
        Set_GameTouchCount();
        m_Maneger_UI.SavePageIndexToFile();
    }

    public void IncreaseIndex_GameTouch()
    {
        if (m_ListGameTouch == null || m_ListGameTouch.Count == 0) return;
        m_GameTouchIndex++;
        if (m_GameTouchIndex >= m_ListGameTouch.Count) m_GameTouchIndex = 0;
        Set_Index();
        Set_GameTouchCount();
        m_Maneger_UI.SavePageIndexToFile();
    }

    public void DecreaseIndex_GameMotion()
    {
        if (m_ListGameMotion == null || m_ListGameMotion.Count == 0) return;
        m_GameMotionIndex--;
        if (m_GameMotionIndex < 0) m_GameMotionIndex = m_ListGameMotion.Count - 1;
        Set_Index();
        Set_GameMotionCount();
        m_Maneger_UI.SavePageIndexToFile();
    }

    public void IncreaseIndex_GameMotion()
    {
        if (m_ListGameMotion == null || m_ListGameMotion.Count == 0) return;
        m_GameMotionIndex++;
        if (m_GameMotionIndex >= m_ListGameMotion.Count) m_GameMotionIndex = 0;
        Set_Index();
        Set_GameMotionCount();
        m_Maneger_UI.SavePageIndexToFile();
    }

    public void Set_GreekMythologyCount()
    {
        m_GreekMythologyCountText.text = (m_GreekMythologyIndex + 1) + "/" + m_ListGreekMythology.Count;
    }
    public void Set_StoryCount()
    {
        m_StoryCountText.text = (m_StoryIndex + 1) + "/" + m_ListStory.Count;
    }

    public void Set_MVCount()
    {
        m_MVCountText.text = (m_MVIndex + 1) + "/" + m_ListMV.Count;
    }

    public void Set_GameTouchCount()
    {
        m_GameTouchCountText.text = (m_GameTouchIndex + 1) + "/" + m_ListGameTouch.Count;
    }

    public void Set_GameMotionCount()
    {
        m_GameMotionCountText.text = (m_GameMotionIndex + 1) + "/" + m_ListGameMotion.Count;
    }

    public void Play_GameTouch()
    {
        Time.timeScale = 0;
        m_Cover.SetActive(true);
        RunExternalExe("T");
    }

    public void Play_GameMotion()
    {
        Time.timeScale = 0;
        m_Cover.SetActive(true);
        RunExternalExe("M");
    }

    public void RunExternalExe(string type)
    {
#if UNITY_STANDALONE_WIN
        try
        {
            // 리스트 유효성 검사
            List<Sprite> targetList = type == "M" ? m_ListGameMotion : m_ListGameTouch;
            int targetIndex = type == "M" ? m_GameMotionIndex : m_GameTouchIndex;
            string basePath = type == "M" ? Path_Motion : Path_Touch;

            if (targetList == null || targetList.Count == 0)
            {
                Debug.LogError($"m_ListGame{(type == "M" ? "Motion" : "Touch")}가 null이거나 비어 있습니다!");
                return;
            }

            // 파일명 설정
            string filename = targetList[targetIndex].name;

            // filename이 포함된 폴더 검색
            string[] folders = Directory.GetDirectories(basePath, $"*{filename}*", SearchOption.TopDirectoryOnly);
            if (folders.Length == 0)
            {
                Debug.LogError($"filename '{filename}'이 포함된 폴더를 찾을 수 없습니다: {basePath}");
                return;
            }

            // 첫 번째 매칭 폴더 선택 (필요 시 다른 기준 적용 가능)
            string selectedFolder = folders[0];
            Debug.Log($"선택된 폴더: {selectedFolder}");

            // 선택된 폴더 내 filename이 포함된 .exe 파일 검색
            string[] exeFiles = Directory.GetFiles(selectedFolder, $"*{filename}*.exe", SearchOption.TopDirectoryOnly);
            if (exeFiles.Length == 0)
            {
                Debug.LogError($"filename '{filename}'이 포함된 .exe 파일을 찾을 수 없습니다: {selectedFolder}");
                return;
            }

            // 첫 번째 매칭 파일 선택 (필요 시 다른 기준 적용 가능)
            string exePath = exeFiles[0];
            string directory = Path.GetDirectoryName(exePath);

            // 파일 존재 여부 확인
            if (!File.Exists(exePath))
            {
                Debug.LogError($"실행 파일을 찾을 수 없습니다: {exePath}");
                return;
            }

            // ProcessStartInfo 설정
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = directory,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            // 프로세스 실행
            Process.Start(startInfo);
            Debug.Log($"실행 시작: {exePath}");

            // BGM 일시정지
            if (m_Maneger_BGM != null)
            {
                m_Maneger_BGM.Pause();
                Debug.Log("BGM 일시정지됨");
            }
            else
            {
                Debug.LogWarning("m_Maneger_BGM이 null입니다!");
            }

            // 애플리케이션 종료
            if (Application.isEditor)
            {
#if UNITY_EDITOR
                Debug.Log("에디터에서 실행 중: 플레이 모드 종료");
                EditorApplication.isPlaying = false;
#endif
            }
            else
            {
                Debug.Log("빌드에서 실행 중: 애플리케이션 종료");
                Application.Quit();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"실행 중 오류: {e.Message}");
        }
#else
    Debug.LogWarning("외부 실행 파일은 Windows Standalone 플랫폼에서만 지원됩니다!");
#endif
    }
}