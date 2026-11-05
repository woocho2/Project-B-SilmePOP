using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Firebase를 SDK 없이 REST(HTTP 요청)로 씀. Windows용 SDK가 베타라서 (기획 [3]).
// 하는 일: 이메일 회원가입·로그인과 토큰 갱신, 읽기·쓰기, 조건부 쓰기(트랜잭션), 실시간 수신(SSE), 서버 시각 맞추기.
// UnityWebRequest는 메인 스레드에서 끝나므로 모든 콜백도 메인 스레드에서 옴.
//
// 읽는 순서: SignUpWithEmail·SignInWithEmail(로그인) → Get/Put/Patch/Post(쓰기·읽기) → Transaction(동시에 써도 안전한 쓰기)
//           → Listen(실시간 수신) → SyncServerTime(서버 시각)
public sealed class FirebaseRestClient
{
    // Transaction의 update가 이것을 돌려주면 쓰지 않고 멈춤
    public static readonly object Abort = new object();

    private const int RequestTimeout = 15;              // 초
    private const double RefreshBeforeExpiry = 300;     // 토큰(1시간)이 끝나기 5분 전에 새로 받음

    private readonly string databaseUrl;
    private readonly string apiKey;
    private string idToken;
    private string refreshToken;
    private DateTime tokenExpiresAt;
    private long serverOffset;

    public string Uid { get; private set; }
    public bool IsSignedIn => idToken != null;
    public bool NeedsTokenRefresh => IsSignedIn && DateTime.UtcNow >= tokenExpiresAt;
    public long ServerNow => LocalNow + serverOffset;

    private static long LocalNow => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public FirebaseRestClient(string databaseUrl, string apiKey)
    {
        this.databaseUrl = databaseUrl.Trim().TrimEnd('/');
        this.apiKey = apiKey.Trim();
    }

    // ── 연결 확인과 로그인 ───────────────────────────────────────

    // 서버에 닿는지만 봄. 로그인 전이라 권한 오류(401)가 와도 닿은 것이므로 true.
    public void CheckConnection(Action<bool> onDone)
    {
        UnityWebRequest request = NewRequest($"{databaseUrl}/.json?shallow=true", "GET", null);
        Send(request, r => onDone?.Invoke(r.result != UnityWebRequest.Result.ConnectionError
                                       && r.result != UnityWebRequest.Result.DataProcessingError));
    }

    // 이메일 회원가입. 성공하면 Uid와 토큰(1시간짜리)을 받음. 실패하면 onDone(false, 사유 코드). 코드 예: EMAIL_EXISTS, INVALID_EMAIL, WEAK_PASSWORD
    public void SignUpWithEmail(string email, string password, Action<bool, string> onDone)
    {
        Authenticate("accounts:signUp", EmailBody(email, password), onDone);
    }

    // 이메일 로그인. 코드 예: INVALID_LOGIN_CREDENTIALS(이메일·비밀번호가 틀림), USER_DISABLED
    public void SignInWithEmail(string email, string password, Action<bool, string> onDone)
    {
        Authenticate("accounts:signInWithPassword", EmailBody(email, password), onDone);
    }

    private void Authenticate(string action, string body, Action<bool, string> onDone)
    {
        string url = $"https://identitytoolkit.googleapis.com/v1/{action}?key={apiKey}";
        Send(NewRequest(url, "POST", body), r =>
        {
            if (r.result != UnityWebRequest.Result.Success)
            {
                string code = AuthErrorCode(r);
                Debug.LogWarning($"Firebase 로그인 실패 ({r.responseCode}): {code}");
                onDone?.Invoke(false, code);
                return;
            }

            object response = Json.Parse(r.downloadHandler.text);
            Uid = FirebaseData.Str(response, "localId");
            SetTokens(FirebaseData.Str(response, "idToken"), FirebaseData.Str(response, "refreshToken"), FirebaseData.Str(response, "expiresIn"));
            onDone?.Invoke(true, null);
        });
    }

    private static string EmailBody(string email, string password)
    {
        return Json.Serialize(new Dictionary<string, object>
        {
            { "email", email },
            { "password", password },
            { "returnSecureToken", true }
        });
    }

    // 응답 {"error":{"message":"WEAK_PASSWORD : Password should be ..."}} 에서 앞의 코드만
    private static string AuthErrorCode(UnityWebRequest request)
    {
        if (request.result == UnityWebRequest.Result.ConnectionError) return "NETWORK_ERROR";
        try
        {
            string message = FirebaseData.Str(FirebaseData.Get(Json.Parse(request.downloadHandler.text), "error"), "message");
            int space = message.IndexOf(' ');
            return space > 0 ? message.Substring(0, space) : message;
        }
        catch (FormatException)
        {
            return "UNKNOWN";
        }
    }

    // 토큰이 끝나기 전에 새 토큰을 받음. 실시간 수신은 새 토큰으로 다시 연결해야 함.
    public void RefreshToken(Action<bool> onDone)
    {
        string url = $"https://securetoken.googleapis.com/v1/token?key={apiKey}";
        UnityWebRequest request = new UnityWebRequest(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("grant_type=refresh_token&refresh_token=" + refreshToken)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        Send(request, r =>
        {
            if (!Succeeded(r, "토큰 갱신")) { onDone?.Invoke(false); return; }

            object body = Json.Parse(r.downloadHandler.text);
            SetTokens(FirebaseData.Str(body, "id_token"), FirebaseData.Str(body, "refresh_token"), FirebaseData.Str(body, "expires_in"));
            onDone?.Invoke(true);
        });
    }

    private void SetTokens(string newIdToken, string newRefreshToken, string expiresInSeconds)
    {
        idToken = newIdToken;
        refreshToken = newRefreshToken;
        double seconds = double.TryParse(expiresInSeconds, out double parsed) ? parsed : 3600;
        tokenExpiresAt = DateTime.UtcNow.AddSeconds(Math.Max(60, seconds - RefreshBeforeExpiry));
    }

    // ── 읽기와 쓰기 ─────────────────────────────────────────────
    // path는 "rooms/abc"처럼 데이터베이스 안의 위치. onDone의 object는 Json.Parse의 결과.

    public void Get(string path, Action<bool, object> onDone)
    {
        Send(NewRequest(DataUrl(path), "GET", null), r =>
        {
            bool ok = Succeeded(r, "읽기 " + path);
            onDone?.Invoke(ok, ok ? Json.Parse(r.downloadHandler.text) : null);
        });
    }

    // 그 위치를 value로 통째로 바꿈. 응답은 실제로 쓰인 값 (서버 시각은 숫자로 바뀌어 옴).
    public void Put(string path, object value, Action<bool, object> onDone = null)
    {
        Send(NewRequest(DataUrl(path), "PUT", Json.Serialize(value)), r =>
        {
            bool ok = Succeeded(r, "쓰기 " + path);
            onDone?.Invoke(ok, ok ? Json.Parse(r.downloadHandler.text) : null);
        });
    }

    // 적힌 자식만 바꿈. 키에 "slots/1/ready"처럼 경로를 써서 여러 곳을 한 번에 바꿀 수 있음. null은 지움.
    public void Patch(string path, Dictionary<string, object> children, Action<bool> onDone = null)
    {
        Send(NewRequest(DataUrl(path), "PATCH", Json.Serialize(children)), r => onDone?.Invoke(Succeeded(r, "고치기 " + path)));
    }

    // 목록에 새 항목을 붙임. 서버가 시간 순서로 정렬되는 키를 만들어 돌려줌 (실패하면 null).
    public void Post(string path, object value, Action<string> onDone = null)
    {
        Send(NewRequest(DataUrl(path), "POST", Json.Serialize(value)), r =>
        {
            if (!Succeeded(r, "추가 " + path)) { onDone?.Invoke(null); return; }
            onDone?.Invoke(FirebaseData.Str(Json.Parse(r.downloadHandler.text), "name", null));
        });
    }

    public void Delete(string path, Action<bool> onDone = null)
    {
        Send(NewRequest(DataUrl(path), "DELETE", null), r => onDone?.Invoke(Succeeded(r, "지우기 " + path)));
    }

    // 동시에 여러 명이 써도 안전한 쓰기. 읽은 뒤 그 사이에 누가 바꿨으면(412) 다시 읽어서 update를 다시 함.
    // update: 지금 값 → 새 값 (null이면 지움, Abort면 멈춤). onDone: 성공 여부, 쓰인 값(멈췄으면 그때의 값).
    public void Transaction(string path, Func<object, object> update, Action<bool, object> onDone, int attemptsLeft = 8)
    {
        UnityWebRequest get = NewRequest(DataUrl(path), "GET", null);
        get.SetRequestHeader("X-Firebase-ETag", "true");
        Send(get, r =>
        {
            if (!Succeeded(r, "트랜잭션 읽기 " + path)) { onDone?.Invoke(false, null); return; }

            string etag = r.GetResponseHeader("ETag");
            object current = Json.Parse(r.downloadHandler.text);
            object next = update(current);
            if (ReferenceEquals(next, Abort)) { onDone?.Invoke(false, current); return; }

            UnityWebRequest put = NewRequest(DataUrl(path), "PUT", Json.Serialize(next));
            put.SetRequestHeader("if-match", etag);
            Send(put, p =>
            {
                if (p.responseCode == 412 && attemptsLeft > 1)
                {
                    Transaction(path, update, onDone, attemptsLeft - 1);
                    return;
                }
                bool ok = Succeeded(p, "트랜잭션 쓰기 " + path);
                onDone?.Invoke(ok, ok ? Json.Parse(p.downloadHandler.text) : null);
            });
        });
    }

    // 서버 시각을 path에 써 보고, 돌아온 값으로 내 시계와의 차이를 구함 (요청 왕복의 가운데를 기준으로).
    public void SyncServerTime(string path, Action onDone)
    {
        long sentAt = LocalNow;
        Put(path, FirebaseData.ServerTimestamp, (ok, written) =>
        {
            long receivedAt = LocalNow;
            if (ok && written is long serverTime) serverOffset = serverTime - (sentAt + receivedAt) / 2;
            onDone?.Invoke();
        });
    }

    // ── 실시간 수신 ─────────────────────────────────────────────

    // path 아래가 바뀔 때마다 onEvent("put" 또는 "patch", 바뀐 위치, 값). 처음엔 path 전체가 put으로 옴.
    // 연결이 끊기거나 토큰이 끝나면 onEnded (다시 연결은 부른 쪽이 함). query는 "limitToLast=30" 같은 추가 조건.
    public Stream Listen(string path, string query, Action<string, string, object> onEvent, Action onEnded)
    {
        string url = DataUrl(path) + (string.IsNullOrEmpty(query) ? "" : "&" + query);
        return new Stream(url, onEvent, onEnded);
    }

    public sealed class Stream
    {
        private readonly UnityWebRequest request;
        private bool closed;

        public Stream(string url, Action<string, string, object> onEvent, Action onEnded)
        {
            request = new UnityWebRequest(url, "GET") { downloadHandler = new EventStreamHandler(OnMessage), timeout = 0 };
            request.SetRequestHeader("Accept", "text/event-stream");
            request.SendWebRequest().completed += _ =>
            {
                if (!closed && request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Firebase 실시간 수신이 끊겼습니다: {request.error}");
                }
                request.Dispose();
                if (!closed) onEnded?.Invoke();
            };

            void OnMessage(string eventName, string data)
            {
                if (closed) return;
                switch (eventName)
                {
                    case "put":
                    case "patch":
                        object body = Json.Parse(data);
                        onEvent?.Invoke(eventName, FirebaseData.Str(body, "path", "/"), FirebaseData.Get(body, "data"));
                        break;
                    case "cancel":        // 권한이 없어짐
                    case "auth_revoked":  // 토큰이 끝남
                        Close();
                        onEnded?.Invoke();
                        break;
                    // keep-alive는 무시
                }
            }
        }

        public void Close()
        {
            if (closed) return;
            closed = true;
            request.Abort();
        }
    }

    // text/event-stream을 받는 대로 "event: 이름\ndata: JSON\n\n" 단위로 잘라 넘김.
    private sealed class EventStreamHandler : DownloadHandlerScript
    {
        private readonly List<byte> pending = new List<byte>();
        private readonly Action<string, string> onMessage;

        public EventStreamHandler(Action<string, string> onMessage) : base(new byte[8192])
        {
            this.onMessage = onMessage;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            for (int i = 0; i < dataLength; i++) pending.Add(data[i]);

            int end;
            while ((end = FindBlankLine()) >= 0)
            {
                // 글자가 여러 조각에 걸쳐 올 수 있어 한 사건이 다 모인 뒤에 UTF-8로 바꿈
                string block = Encoding.UTF8.GetString(pending.GetRange(0, end).ToArray());
                pending.RemoveRange(0, end + 2);
                Dispatch(block);
            }
            return true;
        }

        private int FindBlankLine()
        {
            for (int i = 0; i + 1 < pending.Count; i++)
            {
                if (pending[i] == '\n' && pending[i + 1] == '\n') return i;
            }
            return -1;
        }

        private void Dispatch(string block)
        {
            string eventName = null;
            StringBuilder data = new StringBuilder();
            foreach (string rawLine in block.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (line.StartsWith("event:")) eventName = line.Substring(6).Trim();
                else if (line.StartsWith("data:")) data.Append(line.Substring(5).TrimStart());
            }
            if (eventName != null) onMessage(eventName, data.ToString());
        }
    }

    // ── 공통 ────────────────────────────────────────────────────

    private string DataUrl(string path)
    {
        return $"{databaseUrl}/{path.Trim('/')}.json?auth={Uri.EscapeDataString(idToken ?? "")}";
    }

    private static UnityWebRequest NewRequest(string url, string method, string jsonBody)
    {
        UnityWebRequest request = new UnityWebRequest(url, method) { downloadHandler = new DownloadHandlerBuffer() };
        if (jsonBody != null)
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            request.SetRequestHeader("Content-Type", "application/json");
        }
        return request;
    }

    private static void Send(UnityWebRequest request, Action<UnityWebRequest> onDone)
    {
        request.timeout = RequestTimeout;
        request.SendWebRequest().completed += _ =>
        {
            try { onDone(request); }
            finally { request.Dispose(); }
        };
    }

    private static bool Succeeded(UnityWebRequest request, string what)
    {
        if (request.result == UnityWebRequest.Result.Success) return true;

        string body = request.downloadHandler != null ? request.downloadHandler.text : "";
        Debug.LogWarning($"Firebase {what} 실패 ({request.responseCode}): {request.error} {body}");
        return false;
    }
}
