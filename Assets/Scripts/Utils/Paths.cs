using UnityEngine;
using System.IO;

public static class Paths
{
    // 하드코딩 경로 유지 (사용자 요구사항)
    private static readonly string HardcodedBasePath = @"C:\KidingKiding_App";
    
    // 동적 경로 (개발/배포 유연성)
    private static readonly string DynamicBasePath = Application.dataPath.Replace("/Assets", "");
    
    // 선택적 사용 - 기본값: 하드코딩 경로 사용
    public static bool UseHardcodedPath { get; set; } = true;
    
    // 실제 사용 경로
    public static readonly string BasePath = UseHardcodedPath ? HardcodedBasePath : DynamicBasePath;
    public static readonly string MoviePath = Path.Combine(BasePath, "Movie");
    public static readonly string AudioPath = Path.Combine(BasePath, "Resource", "Audio", "Movie");
    public static readonly string SubtitlesPath = Path.Combine(BasePath, "Movie", "Subtitles");
    public static readonly string GameTouchPath = Path.Combine(BasePath, "Game", "Touch");
    public static readonly string GameMotionPath = Path.Combine(BasePath, "Game", "Motion");
    public static readonly string ImageGreekMythologyPath = Path.Combine(BasePath, "Resource", "Image", "GreekMythology");
    public static readonly string ImageMVPath = Path.Combine(BasePath, "Resource", "Image", "Music");
    public static readonly string ImageStoryPath = Path.Combine(BasePath, "Resource", "Image", "Story");
    public static readonly string ImageTouchPath = Path.Combine(BasePath, "Resource", "Image", "Touch");
    public static readonly string ImageMotionPath = Path.Combine(BasePath, "Resource", "Image", "Motion");
    public static readonly string PageInfoPath = Path.Combine(BasePath, "PageInfo.txt");
    public static readonly string LanguagePath = Path.Combine(BasePath, "Language.json");
    public static readonly string ExternalMusicPath = Path.Combine(BasePath, "Resource", "Audio", "Music");
    
    // 런타임에 경로 초기화 및 로깅
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        Debug.Log($"Paths initialized with UseHardcodedPath = {UseHardcodedPath}");
        Debug.Log($"BasePath = {BasePath}");
    }
}
