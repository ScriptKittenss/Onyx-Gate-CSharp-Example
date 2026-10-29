// ============================================================
//  Onyx Gate SDK  —  SKAuth.cs
//  Drop into any C# .NET 8 Windows project.
//  Requires NuGet: System.Management
//  Docs: https://auth.script-kittens.com  →  Documentation → SDKs
// ============================================================

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Management;
using System.Security.Cryptography;

namespace OnyxGateExample
{
    public class SKAuth
    {
        // ── Server ──────────────────────────────────────────────────────────
        private const string BASE = "https://auth.script-kittens.com";

        // ── Fields ──────────────────────────────────────────────────────────
        private readonly string      _appId;
        private readonly string      _version;
        private readonly string      _hwid;
        private static readonly HttpClient _http = new();

        // ── Public info (set after login) ────────────────────────────────────
        // user holds: username, plan, expires, email, apiKey
        public JsonElement? User { get; private set; }
        public string?      Hwid => _hwid;

        // ── Constructor ──────────────────────────────────────────────────────
        public SKAuth(string appId, string version = "1.0")
        {
            _appId   = appId;
            _version = version;
            _hwid    = GetHWID();
        }

        // ── HWID — disk serial hashed with SHA256 (first 32 hex chars) ───────
        // Matches the Python and C++ SDKs: same machine => same HWID on all SDKs.
        public static string GetHWID()
        {
            string raw = "";
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT SerialNumber FROM Win32_DiskDrive");
                foreach (ManagementBaseObject obj in searcher.Get())
                {
                    raw = (obj["SerialNumber"]?.ToString() ?? "").Trim();
                    if (!string.IsNullOrEmpty(raw)) break;
                }
            }
            catch { /* fallback below */ }
            if (string.IsNullOrEmpty(raw))
                raw = Environment.MachineName;
            return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(raw.Trim()))
            ).ToLower()[..32];
        }

        // ── Internal POST helper ─────────────────────────────────────────────
        private async Task<JsonElement> Post(string endpoint, object body)
        {
            try
            {
                var json    = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
                var res     = await _http.PostAsync(BASE + endpoint, content, cts.Token);
                var raw     = await res.Content.ReadAsStringAsync();
                return JsonDocument.Parse(raw).RootElement;
            }
            catch (Exception ex)
            {
                var err = $"{{\"ok\":false,\"message\":\"Connection error: {ex.Message}\"}}";
                return JsonDocument.Parse(err).RootElement;
            }
        }

        private static string Prop(JsonElement el, string name, string fallback = "")
            => el.ValueKind == JsonValueKind.Object
               && el.TryGetProperty(name, out var v)
               && v.ValueKind == JsonValueKind.String
               ? v.GetString() ?? fallback : fallback;

        // ── Login ─────────────────────────────────────────────────────────────
        // On success, User is populated. On failure, show the server's message
        // (HWID mismatch, banned, expired, update required, offline...) as-is.
        public async Task<JsonElement> Login(string username, string password)
        {
            var r = await Post("/sdk/login", new
            {
                appId    = _appId,
                username,
                password,
                hwid     = _hwid,
                version  = _version
            });

            if (r.ValueKind == JsonValueKind.Object
                && r.TryGetProperty("ok", out var ok) && ok.GetBoolean()
                && r.TryGetProperty("user", out var user))
            {
                User = user;
            }
            return r;
        }

        // ── Register (new account — license key optional) ──────────────────────
        public async Task<JsonElement> Register(
            string username, string password,
            string email = "", string licenseKey = "")
        {
            return await Post("/sdk/register", new
            {
                appId    = _appId,
                username,
                password,
                email,
                licenseKey
            });
        }

        // ── Redeem a license key on an existing account ─────────────────────────
        public async Task<JsonElement> Redeem(string username, string licenseKey)
            => await Post("/sdk/redeem", new { appId = _appId, username, licenseKey });

        // ── Validate — heartbeat ────────────────────────────────────────────────
        // Call every few minutes while your tool runs. Kicks banned / expired
        // users and syncs plan changes without re-login. Needs a login first.
        public async Task<JsonElement> Validate()
        {
            var apiKey = User is JsonElement u ? Prop(u, "apiKey") : "";
            if (string.IsNullOrEmpty(apiKey))
            {
                var err = "{\"ok\":false,\"message\":\"Not logged in — call Login() first.\"}";
                return JsonDocument.Parse(err).RootElement;
            }
            var r = await Post("/sdk/validate", new
            {
                appId  = _appId,
                apiKey,
                hwid   = _hwid
            });
            if (r.ValueKind == JsonValueKind.Object
                && r.TryGetProperty("ok", out var ok) && ok.GetBoolean()
                && User is JsonElement user
                && user.ValueKind == JsonValueKind.Object)
            {
                // sync plan/expires into a refreshed user object
                using var doc = JsonDocument.Parse(user.GetRawText());
                var dict = new System.Collections.Generic.Dictionary<string, JsonElement>();
                foreach (var p in doc.RootElement.EnumerateObject())
                    dict[p.Name] = p.Value.Clone();
                if (r.TryGetProperty("plan", out var plan))       dict["plan"] = plan.Clone();
                if (r.TryGetProperty("expires", out var expires)) dict["expires"] = expires.Clone();
                var rebuilt = "{" + string.Join(",",
                    System.Linq.Enumerable.Select(dict,
                        kv => $"\"{kv.Key}\":{kv.Value.GetRawText()}")) + "}";
                User = JsonDocument.Parse(rebuilt).RootElement;
            }
            return r;
        }

        // ── GetVar — fetch a server-side runtime variable by name ────────────────
        // Set in dashboard → Settings → Variables. No recompile needed to change
        // values (version checks, announcements, download URLs...).
        public async Task<string?> GetVar(string name)
        {
            var r = await Post("/sdk/variable", new { appId = _appId, name });
            return Prop(r, "value", "") is string v && v != "" ? v : null;
        }

        // ── DownloadFile — fetch a file from the Runtime Vault ────────────────────
        // fileId comes from dashboard → Files. Needs a logged-in user.
        // Returns true when the file was saved to savePath.
        public async Task<bool> DownloadFile(string fileId, string savePath)
        {
            try
            {
                var apiKey = User is JsonElement u ? Prop(u, "apiKey") : "";
                if (string.IsNullOrEmpty(apiKey)) return false;
                var url = $"{BASE}/sdk/file?appId={Uri.EscapeDataString(_appId)}"
                        + $"&fileId={Uri.EscapeDataString(fileId)}"
                        + $"&apiKey={Uri.EscapeDataString(apiKey)}";
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                var bytes = await _http.GetByteArrayAsync(url, cts.Token);
                if (bytes.Length == 0) return false;
                await File.WriteAllBytesAsync(savePath, bytes, cts.Token);
                return true;
            }
            catch { return false; }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        public string GetUsername()
            => User is JsonElement u ? Prop(u, "username") : "";

        public string GetPlan()
            => User is JsonElement u ? Prop(u, "plan", "free") : "free";

        public string GetExpires()
            => User is JsonElement u ? Prop(u, "expires") : "";

        // Any plan that isn't free counts as paid (starter, pro, ...).
        public bool IsPaid()
            => !string.Equals(GetPlan(), "free", StringComparison.OrdinalIgnoreCase);
    }
}
