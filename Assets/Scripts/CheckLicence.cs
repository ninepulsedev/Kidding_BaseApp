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
                            found = true;
                            isAuthenticated = doc?.fields?.isAuthenticated?.booleanValue ?? false;
                            break;
                        }
                    }
                }

                if (found && !isAuthenticated)
                {
                    m_DemoText.SetActive(true);
                }
                else if (found && isAuthenticated)
                {
                    m_DemoText.SetActive(false);
                }
                else
                {
                    m_DemoText.SetActive(true);
                }
            }
            else if (request.responseCode == 404)
            {
                m_DemoText.SetActive(true);
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