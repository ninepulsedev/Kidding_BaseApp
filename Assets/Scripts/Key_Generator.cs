using SFB;
using Sirenix.OdinInspector;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Key_Generator : MonoBehaviour
{
    public InputField m_InputField;
    public string m_Hwid;
    public string m_LicenseKey;
    private const string LicenseFileName = "license.dat";
    private string path = Path.Combine("C:\\KidingKiding_App", LicenseFileName);

    [HorizontalGroup("LicenseKey")]
    [Button("Create LicenseKey", ButtonSizes.Large), GUIColor(0, 1, 1)]
    public void Create_LicenseKey()
    {
        string hwid = m_Hwid;
        string licenseKey = GenerateLicenseKey(hwid);
        m_LicenseKey = licenseKey;

        // 컴퓨터 이름 가져오기
        string computerName = System.Environment.MachineName;
        string fileName = $"{computerName}_LicenseKey.dat";

        // 파일 확장자 필터 설정 (.dat 파일)
        var extensions = new[] {
            new ExtensionFilter("License Files", "dat"),
            new ExtensionFilter("All Files", "*"),
        };

        // 파일 저장 브라우저 열기
        var savePath = StandaloneFileBrowser.SaveFilePanel("Save License Key", "", fileName, extensions);

        // 선택된 경로에 파일 저장
        if (!string.IsNullOrEmpty(savePath))
        {
            try
            {
                File.WriteAllText(savePath, licenseKey);
            }
            catch (IOException e)
            {
                Debug.LogError($"Failed to save license key to {savePath}. Error: {e.Message}");
            }
        }
        else
        {
            // Save file selection canceled or invalid path.
        }
    }

    private void SaveLicenseKey(string key)
    {
        File.WriteAllText(path, key);
    }

    private string GetHardwareID()
    {
        return SystemInfo.deviceUniqueIdentifier;
    }

    private string GenerateLicenseKey(string input)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            string base64 = Convert.ToBase64String(hash);
            string cleaned = new string(base64.Where(char.IsLetterOrDigit).ToArray()).ToUpper();

            StringBuilder rawKey = new StringBuilder();
            int[] indexPattern = { 3, 11, 7, 19, 2, 5, 13, 23, 17, 0, 9, 29 };

            foreach (int i in indexPattern)
            {
                if (i < cleaned.Length)
                    rawKey.Append(cleaned[i]);
                else
                    rawKey.Append('X');
            }

            return string.Format("{0}-{1}-{2}",
                rawKey.ToString(0, 4),
                rawKey.ToString(4, 4),
                rawKey.ToString(8, 4));
        }
    }

    public void OpenFileBrowser()
    {
        // 파일 확장자 필터 설정 (.txt 파일)
        var extensions = new[] {
            new ExtensionFilter("Hwid Files", "txt"),
            new ExtensionFilter("All Files", "*"),
        };

        // 파일 브라우저 열기
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Hwid File", "", extensions, false);

        // 선택된 파일 처리
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            LoadDatFile(paths[0]);
        }
        else
        {
            // File selection canceled or invalid path.
        }
    }

    void LoadDatFile(string filePath)
    {
        try
        {
            // 1. 바이너리 데이터 읽기 (필요시)
            byte[] fileData = File.ReadAllBytes(filePath);

            // 2. 텍스트 읽기 (텍스트 파일 .dat 가정)
            string textContent = File.ReadAllText(filePath);

            // 여기서 fileData 또는 textContent를 사용해 추가 처리
            // 예: 라이선스 데이터 파싱, 검증 등
            ProcessLicenseData(textContent);
        }
        catch (IOException e)
        {
            Debug.LogError($"Failed to load file: {filePath}. Error: {e.Message}");
        }
    }

    void ProcessLicenseData(string content)
    {
        // .dat 파일 내용을 기반으로 라이선스 처리 로직
        // 예: 데이터 처리
        if (!string.IsNullOrEmpty(content))
        {
            // License file content processed successfully.
            // 여기서 JSON, XML, 또는 특정 형식 파싱 가능
            // 예: if (content.Contains("valid_license_key")) { ... }
            m_Hwid = content;
        }
        else
        {
            // License file is empty or invalid.
        }
    }
}