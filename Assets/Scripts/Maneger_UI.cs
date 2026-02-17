using UnityEngine;
using Sirenix.OdinInspector;
using System.IO;
using System;

public class Maneger_UI : MonoBehaviour
{
    [FoldoutGroup("Setting")] public List_Contents m_List_Contents;
    [FoldoutGroup("Setting")] public GameObject m_PointsPos;
    [FoldoutGroup("Setting")] public GameObject m_Main;
    [FoldoutGroup("Setting")] public GameObject m_GreekMythology;
    [FoldoutGroup("Setting")] public GameObject m_Story;
    [FoldoutGroup("Setting")] public GameObject m_MV;
    [FoldoutGroup("Setting")] public GameObject m_Setting;
    [FoldoutGroup("Setting")] public GameObject m_GameTouch;
    [FoldoutGroup("Setting")] public GameObject m_GameMotion;
    [FoldoutGroup("Setting")] public GameObject m_Category;
    [FoldoutGroup("Setting")] public GameObject m_Player;
    [FoldoutGroup("Setting")] public GameObject m_BlankImage;
    [FoldoutGroup("Setting")] public float m_ChangeDelay = 0.3f;
    [FoldoutGroup("Setting")] public int m_PageIndex = 0;
    [FoldoutGroup("Audio")] public GameObject m_AudioClick;

    private readonly string pageInfoPath = Paths.PageInfoPath;
    private VideoPlayerManager m_PlayerManager;

    [Serializable]
    private struct PageData
    {
        public int pageIndex;
        public int greekMythologyIndex;
        public int storyIndex;
        public int mvIndex;
        public int gameTouchIndex;
        public int gameMotionIndex;
    }
    private void Awake()
    {
        m_Setting.SetActive(false);
        m_Player.SetActive(false); 
        if (m_Player != null)
            m_PlayerManager = m_Player.GetComponent<VideoPlayerManager>();
    }
    void Start()
    {
        // 프로그램 시작 시 저장 파일 삭제
        DeletePageInfoFile();
        
        // 기본값 적용
        ApplyDefaultValues();
        
        // 기본 페이지로 이동
        Invoke(nameof(Main), 1f);
    }
    private void DeletePageInfoFile()
    {
        try
        {
            if (File.Exists(pageInfoPath))
            {
                File.Delete(pageInfoPath);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"���� ���� �� ���� �߻�: {e.Message}");
        }
    }
    public void Set_Main()
    {
        Invoke(nameof(Main), m_ChangeDelay);
    }
    public void Set_GreekMythology()
    {
        Invoke(nameof(GreekMythology), m_ChangeDelay);
    }

    public void Set_Story()
    {
        Invoke(nameof(Story), m_ChangeDelay);
    }

    public void Set_MV()
    {
        Invoke(nameof(MV), m_ChangeDelay);
    }

    public void Set_GameTouch()
    {
        Invoke(nameof(GameTouch), m_ChangeDelay);
    }

    public void Set_GameMotion()
    {
        Invoke(nameof(GameMotion), m_ChangeDelay);
    }

    public void Set_All()
    {
        m_Main.SetActive(true);
        m_MV.SetActive(true);
        m_Story.SetActive(true);
        m_GameTouch.SetActive(true);
        m_GameMotion.SetActive(true);
        m_Category.SetActive(true);
        m_Player.SetActive(true);
        m_BlankImage.SetActive(true);
        SavePageIndexToFile(); // m_PageIndex �� �ε��� ����
    }

    void Main()
    {
        m_PageIndex = 0;
        m_Main.SetActive(true);
        m_MV.SetActive(false);
        m_GreekMythology.SetActive(false);
        m_Story.SetActive(false);
        m_GameTouch.SetActive(false);
        m_GameMotion.SetActive(false);
        m_Category.SetActive(false);
        m_Player.SetActive(false);
        m_BlankImage.SetActive(false);
        SavePageIndexToFile(); // m_PageIndex �� �ε��� ����
    }

    void GreekMythology()
    {
        m_PageIndex = 1;
        m_Main.SetActive(false);
        m_MV.SetActive(false);
        m_GreekMythology.SetActive(true);
        m_Story.SetActive(false);
        m_GameTouch.SetActive(false);
        m_GameMotion.SetActive(false);
        m_Category.SetActive(true);
        m_Player.SetActive(false);
        m_BlankImage.SetActive(false);
        SavePageIndexToFile(); // m_PageIndex �� �ε��� ����
        if (m_PlayerManager != null)
            m_PlayerManager.m_VideoType = "GreekMythology";
    }


    void Story()
    {
        m_PageIndex = 1;
        m_Main.SetActive(false);
        m_MV.SetActive(false);
        m_GreekMythology.SetActive(false);
        m_Story.SetActive(true);
        m_GameTouch.SetActive(false);
        m_GameMotion.SetActive(false);
        m_Category.SetActive(true);
        m_Player.SetActive(false);
        m_BlankImage.SetActive(false);
        SavePageIndexToFile(); // m_PageIndex �� �ε��� ����
        if (m_PlayerManager != null)
            m_PlayerManager.m_VideoType = "Story";
    }

    void MV()
    {
        m_PageIndex = 2;
        m_Main.SetActive(false);
        m_MV.SetActive(true);
        m_GreekMythology.SetActive(false);
        m_Story.SetActive(false);
        m_GameTouch.SetActive(false);
        m_GameMotion.SetActive(false);
        m_Category.SetActive(true);
        m_Player.SetActive(false);
        m_BlankImage.SetActive(false);
        SavePageIndexToFile(); // m_PageIndex �� �ε��� ����
        if (m_PlayerManager != null)
            m_PlayerManager.m_VideoType = "MV";
    }

    void GameTouch()
    {
        m_PageIndex = 3;
        m_Main.SetActive(false);
        m_MV.SetActive(false);
        m_GreekMythology.SetActive(false);
        m_Story.SetActive(false);
        m_GameTouch.SetActive(true);
        m_GameMotion.SetActive(false);
        m_Category.SetActive(true);
        m_Player.SetActive(false);
        m_BlankImage.SetActive(false);
        SavePageIndexToFile(); // m_PageIndex �� �ε��� ����
    }

    void GameMotion()
    {
        m_PageIndex = 4;
        m_Main.SetActive(false);
        m_MV.SetActive(false);
        m_GreekMythology.SetActive(false);
        m_Story.SetActive(false);
        m_GameTouch.SetActive(false);
        m_GameMotion.SetActive(true);
        m_Category.SetActive(true);
        m_Player.SetActive(false);
        m_BlankImage.SetActive(false);
        SavePageIndexToFile(); // m_PageIndex �� �ε��� ����
    }

    private void ApplyDefaultValues()
    {
        m_PageIndex = 0;
        if (m_List_Contents != null)
        {
            m_List_Contents.m_GreekMythologyIndex = 0;
            m_List_Contents.m_StoryIndex = 0;
            m_List_Contents.m_MVIndex = 0;
            m_List_Contents.m_GameTouchIndex = 0;
            m_List_Contents.m_GameMotionIndex = 0;
            m_List_Contents.Set_AllIndex();
        }
    }

    private void LoadPageIndexFromFile()
    {
#if UNITY_STANDALONE_WIN
        try
        {
            // ���丮 Ȯ�� �� ����
            string directory = Path.GetDirectoryName(pageInfoPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(pageInfoPath))
            {
                string jsonContent = File.ReadAllText(pageInfoPath);
                PageData data = JsonUtility.FromJson<PageData>(jsonContent);

                m_PageIndex = data.pageIndex;

                // m_List_Contents�� null�� �ƴ� ��� �ε��� ����
                if (m_List_Contents != null)
                {
                    m_List_Contents.m_GreekMythologyIndex = data.greekMythologyIndex;
                    m_List_Contents.m_StoryIndex = data.storyIndex;
                    m_List_Contents.m_MVIndex = data.mvIndex;
                    m_List_Contents.m_GameTouchIndex = data.gameTouchIndex;
                    m_List_Contents.m_GameMotionIndex = data.gameMotionIndex;
                    m_List_Contents.Set_AllIndex(); // UI ����
                }
            }
            else
            {
                // PageInfo.txt not found. Using default values.
                // 기본값 적용
                m_PageIndex = 0;
                if (m_List_Contents != null)
                {
                    m_List_Contents.m_GreekMythologyIndex = 0;
                    m_List_Contents.m_StoryIndex = 0;
                    m_List_Contents.m_MVIndex = 0;
                    m_List_Contents.m_GameTouchIndex = 0;
                    m_List_Contents.m_GameMotionIndex = 0;
                    m_List_Contents.Set_AllIndex(); // UI ����
                }
                SavePageIndexToFile(); // 기본값으로 파일 생성������ ������ �����ϰ� �⺻�� ����
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load data from {pageInfoPath}: {e.Message}");
            m_PageIndex = 0; // ���� �� �⺻��
            if (m_List_Contents != null)
            {
                m_List_Contents.m_GreekMythologyIndex = 0;
                m_List_Contents.m_StoryIndex = 0;
                m_List_Contents.m_MVIndex = 0;
                m_List_Contents.m_GameTouchIndex = 0;
                m_List_Contents.m_GameMotionIndex = 0;
                m_List_Contents.Set_AllIndex(); // UI ����
            }
            SavePageIndexToFile(); // ���� �� �⺻�� ����
        }
#else
        Debug.LogWarning("File I/O is only supported on Windows Standalone platform!");
#endif
    }

    public void SavePageIndexToFile()
    {
#if UNITY_STANDALONE_WIN
        try
        {
            // ���丮 Ȯ�� �� ����
            string directory = Path.GetDirectoryName(pageInfoPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // ������ �غ�
            PageData data = new PageData
            {
                pageIndex = m_PageIndex,
                greekMythologyIndex = m_List_Contents != null ? m_List_Contents.m_GreekMythologyIndex : 0,
                storyIndex = m_List_Contents != null ? m_List_Contents.m_StoryIndex : 0,
                mvIndex = m_List_Contents != null ? m_List_Contents.m_MVIndex : 0,
                gameTouchIndex = m_List_Contents != null ? m_List_Contents.m_GameTouchIndex : 0,
                gameMotionIndex = m_List_Contents != null ? m_List_Contents.m_GameMotionIndex : 0
            };

            // JSON���� ����ȭ
            string jsonContent = JsonUtility.ToJson(data, true);
            File.WriteAllText(pageInfoPath, jsonContent);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save data to {pageInfoPath}: {e.Message}");
        }
#else
        Debug.LogWarning("File I/O is only supported on Windows Standalone platform!");
#endif
    }
}