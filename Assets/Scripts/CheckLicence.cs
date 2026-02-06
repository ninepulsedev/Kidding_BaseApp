using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;

public class CheckLicense : MonoBehaviour
{
    public GameObject m_DemoText;
    private string firebaseUrl = "https://firestore.googleapis.com/v1/projects/kiosk-manager-bb4dc/databases/(default)/documents/devices";
    private string installUrlBase = "https://kiosk-manager-bb4dc.web.app/install.html?cpuId=";
    [SerializeField] private string accessToken = ""; // Firebase 인증 토큰 (인스펙터에서 설정하거나 코드로 주입)

    void Start()
    {
        m_DemoText.GetComponent<Text>().raycastTarget = false;
        string hardwareCpuId = GetHardwareIdentifier();
        StartCoroutine(CheckCpuIdInFirestore(hardwareCpuId));
    }

    private string GetHardwareIdentifier()
    {
        string identifier = SystemInfo.deviceUniqueIdentifier;
        if (string.IsNullOrEmpty(identifier))
        {
            identifier = System.Guid.NewGuid().ToString();
            Debug.LogWarning("deviceUniqueIdentifier가 비어 있어 GUID 사용: " + identifier);
        }
        else
        {
            Debug.Log("deviceUniqueIdentifier 사용: " + identifier);
        }
        return identifier;
    }

    private IEnumerator CheckCpuIdInFirestore(string hardwareCpuId)
    {
        string fullUrl = firebaseUrl + "?pageSize=100"; // 최대 100개 문서 조회
        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            // 인증 토큰 추가 (토큰이 필요한 경우)
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success && request.responseCode == 200)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Firestore 응답: " + jsonResponse);

                var collection = JsonUtility.FromJson<FirestoreCollection>(jsonResponse);
                bool found = false;
                bool isAuthenticated = true; // 기본값 설정
                if (collection.documents != null)
                {
                    foreach (var doc in collection.documents)
                    {
                        string firestoreCpuId = doc?.fields?.cpuId?.stringValue ?? "N/A";
                        if (firestoreCpuId == hardwareCpuId)
                        {
                            Debug.Log("결과: CPU ID가 Firestore에 있음");
                            found = true;
                            isAuthenticated = doc?.fields?.isAuthenticated?.booleanValue ?? true; // isAuthenticated 확인
                            Debug.Log($"isAuthenticated 값: {isAuthenticated}");
                            break;
                        }
                    }
                }

                if (found && !isAuthenticated)
                {
                    Debug.Log("결과: CPU ID 존재, isAuthenticated=false -> 데모 모드 활성화");
                    m_DemoText.SetActive(true); // 데모 모드 활성화
                }
                else if (found && isAuthenticated)
                {
                    Debug.Log("결과: CPU ID 존재, isAuthenticated=true -> 데모 모드 비활성화");
                    m_DemoText.SetActive(false); // 데모 모드 비활성화
                }
                else
                {
                    Debug.Log("결과: CPU ID가 Firestore에 없음 -> 설치 URL로 이동");
                    string installUrl = installUrlBase + UnityWebRequest.EscapeURL(hardwareCpuId);
                    m_DemoText.SetActive(true); // CPU ID 없으면 데모 모드 활성화
                    Application.OpenURL(installUrl); // 브라우저 열기
                }
            }
            else if (request.responseCode == 404)
            {
                string installUrl = installUrlBase + UnityWebRequest.EscapeURL(hardwareCpuId);
                Debug.Log("Unity에서 추출한 CPU ID: " + hardwareCpuId);
                Debug.Log("결과: CPU ID가 Firestore에 없음 (경로 또는 데이터 없음). 설치 URL로 이동합니다: " + installUrl);
                m_DemoText.SetActive(true); // 데모 모드 활성화
                Application.OpenURL(installUrl); // 브라우저 열기
            }
            else
            {
                Debug.LogError($"Firestore 요청 실패 (코드: {request.responseCode}): {request.error}");
                m_DemoText.SetActive(true); // 오류 시 데모 모드 활성화
            }
        }
    }

    [Serializable]
    private class FirestoreCollection
    {
        public List<FirestoreDocument> documents;
    }

    [Serializable]
    private class FirestoreDocument
    {
        public string name;
        public Fields fields;
    }

    [Serializable]
    private class Fields
    {
        public CpuId cpuId;
        public AuthenticationAllowed isAuthenticated; // 필드 이름 변경
    }

    [Serializable]
    private class CpuId
    {
        public string stringValue;
    }

    [Serializable]
    private class AuthenticationAllowed
    {
        public bool booleanValue;
    }
}