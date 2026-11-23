// XRealityLabClient.cs - minimal Unity client for the XRealityLab API.
//
// Drop this file into the Assets folder of a Unity project (2019.4 or newer),
// add the component to a GameObject and fill in the fields in the Inspector.
//
// Flow:
//   1. Login(email, password)           -> stores the auth token of the researcher (PI or Research Assistant)
//   2. SendResult(time, mistakes, notes) -> saves one VR/AR result for the participant
//
// The three "pass tokens" are copied from the web platform:
//   My Projects > project > data collection > participant name > Results page
//   buttons "Copy Participant Pass Token", "Copy Session Pass Token", "Copy Research Pass Token".

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class XRealityLabClient : MonoBehaviour
{
    [Header("API address, without a trailing slash (e.g. https://xrealitylab-api.azurewebsites.net)")]
    public string apiBaseUrl = "http://localhost:8081";

    [Header("Researcher account (Principal Investigator or Research Assistant of the project)")]
    public string email;
    public string password;

    [Header("Pass tokens copied from the Results page of the participant")]
    public string participantPassToken;
    public string sessionPassToken;
    public string researchPassToken;

    public string AuthToken { get; private set; }

    [Serializable] private class UserResponse { public int userId; public string firstName; public string lastName; public string authToken; public int roleId; }
    [Serializable] private class ApiResponse { public string responseGUID; public string message; public string result; }
    [Serializable] private class ParticipantResult
    {
        public string participantMD5;
        public string sessionMD5;
        public string researchMD5;
        public string time;          // "hh:mm:ss"
        public int noOfMistakes;
        public string notes;         // optional, max 250 characters
    }

    /// <summary>Logs the researcher in. Every login creates a new token and invalidates the previous one.</summary>
    public IEnumerator Login(Action<bool, string> done = null)
    {
        string url = apiBaseUrl + "/api/Users/email/" + UnityWebRequest.EscapeURL(email) + "/" + UnityWebRequest.EscapeURL(password);
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            string body = request.downloadHandler.text;
            if (request.responseCode == 200)
            {
                AuthToken = JsonUtility.FromJson<UserResponse>(body).authToken;
                done?.Invoke(true, "Logged in");
            }
            else
            {
                done?.Invoke(false, ReadMessage(body, request.error));
            }
        }
    }

    /// <summary>Saves one Virtual / Augmented Reality result (duration and number of mistakes) for the participant.</summary>
    public IEnumerator SendResult(TimeSpan duration, int mistakes, string notes = "", Action<bool, string> done = null)
    {
        if (string.IsNullOrEmpty(AuthToken))
        {
            done?.Invoke(false, "Call Login first");
            yield break;
        }

        var payload = new ParticipantResult
        {
            participantMD5 = participantPassToken.Trim(),
            sessionMD5 = sessionPassToken.Trim(),
            researchMD5 = researchPassToken.Trim(),
            time = string.Format("{0:00}:{1:00}:{2:00}", (int)duration.TotalHours, duration.Minutes, duration.Seconds),
            noOfMistakes = mistakes,
            notes = notes
        };

        // The token contains special characters, so it must be URL-encoded.
        string url = apiBaseUrl + "/api/Project/ParticipantResult?_authToken=" + UnityWebRequest.EscapeURL(AuthToken);
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            string body = request.downloadHandler.text;
            done?.Invoke(request.responseCode == 200, ReadMessage(body, request.error));
        }
    }

    private static string ReadMessage(string body, string fallback)
    {
        try { return JsonUtility.FromJson<ApiResponse>(body).message ?? fallback; }
        catch { return string.IsNullOrEmpty(body) ? fallback : body; }
    }

    // Example: log in when the scene starts and send a test result.
    // Remove this and call Login / SendResult from your own game logic.
    private IEnumerator Start()
    {
        yield return Login((ok, message) => Debug.Log("XRealityLab login: " + message));
        if (!string.IsNullOrEmpty(AuthToken) && !string.IsNullOrEmpty(participantPassToken))
            yield return SendResult(TimeSpan.FromSeconds(95), 2, "test from Unity", (ok, message) => Debug.Log("XRealityLab result: " + message));
    }
}
