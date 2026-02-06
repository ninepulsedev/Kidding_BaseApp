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
            Debug.Log("[AUTH_DEBUG] CPU ID: " + identifier);
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
                Debug.Log("[AUTH_DEBUG] CPU ID: " + hardwareCpuId);
                Debug.Log("Firestore 응답: " + jsonResponse);

                var collection = JsonUtility.FromJson<FirestoreCollection>(jsonResponse);
                bool found = false;
                bool isAuthenticated = false; // 기본값 설정 (인증되지 않은 상태)
                if (collection.documents != null)
                {
                    foreach (var doc in collection.documents)
                    {
                        string firestoreDeviceId = doc?.fields?.deviceId?.stringValue ?? "N/A";
                        if (firestoreDeviceId == hardwareCpuId)
                        {
                            Debug.Log("결과: CPU ID가 Firestore에 있음");
                            found = true;
                            isAuthenticated = doc?.fields?.isAuthenticated?.booleanValue ?? false; // isAuthenticated 확인
                            Debug.Log($"isAuthenticated 값: {isAuthenticated}");
                            break;
                        }
                    }
                }

                if (found && !isAuthenticated)
                {
                    Debug.Log("[AUTH_DEBUG] Found: True, Auth: False -> Demo Mode ACTIVE");
                    m_DemoText.SetActive(true); // 데모 모드 활성화
                }
                else if (found && isAuthenticated)
                {
                    Debug.Log("[AUTH_DEBUG] Found: True, Auth: True -> Demo Mode HIDDEN");
                    m_DemoText.SetActive(false); // 데모 모드 비활성화
                }
                else
                {
                    Debug.Log("[AUTH_DEBUG] Found: False -> Demo Mode ACTIVE");
                    m_DemoText.SetActive(true); // CPU ID 없으면 데모 모드 활성화
                }
            }
            else if (request.responseCode == 404)
            {
                Debug.Log("Unity에서 추출한 CPU ID: " + hardwareCpuId);
                Debug.Log("결과: CPU ID가 Firestore에 없음 (경로 또는 데이터 없음) -> 데모 모드 활성화");
                m_DemoText.SetActive(true); // 데모 모드 활성화
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
    public class Fields
    {
        public DeviceId deviceId;
        public AuthenticationAllowed isAuthenticated;
    }

    [Serializable]
    public class DeviceId
    {
        public string stringValue;
    }

    [Serializable]
    public class AuthenticationAllowed
    {
        public bool booleanValue;
    }
}