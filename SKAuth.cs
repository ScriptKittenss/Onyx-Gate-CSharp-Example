// ============================================================
//  Onyx Gate SDK  —  SKAuth.cs
//  Drop into any C# .NET 8 Windows project.
//  Requires NuGet: System.Management  (already in .csproj)
// ============================================================

using System;
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
        public JsonElement? User      { get; private set; }
        public string?      SessionId { get; private set; }

        // ── Constructor ──────────────────────────────────────────────────────
        public SKAuth(string appId, string version = "1.0")
        {
            _appId   = appId;
            _version = version;
            _hwid    = GetHWID();
        }

        // ── HWID — reads disk serial, hashes it ─────────────────────────────
        private static string GetHWID()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT SerialNumber FROM Win32_DiskDrive");
                foreach (ManagementBaseObject obj in searcher.Get())
                {
                    var serial = obj["SerialNumber"]?.ToString() ?? "";
                    var bytes  = SHA256.HashData(Encoding.UTF8.GetBytes(serial));
                    return Convert.ToHexString(bytes).ToLower()[..32];
                }
            }
            catch { /* fallback below */ }
            return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(Environment.MachineName))
            ).ToLower()[..32];
        }

        // ── Internal POST helper ─────────────────────────────────────────────
        private async Task<JsonElement> Post(string endpoint, object body)
        {
            try
            {
                var json    = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var res     = await _http.PostAsync(BASE + endpoint, content);
                var raw     = await res.Content.ReadAsStringAsync();
                return JsonDocument.Parse(raw).RootElement;
            }
            catch (Exception ex)
            {
                var err = $"{{\"ok\":false,\"message\":\"Connection error: {ex.Message}\"}}";
                return JsonDocument.Parse(err).RootElement;
            }
        }

        // ── Login ─────────────────────────────────────────────────────────────
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

            if (r.GetProperty("ok").GetBoolean())
            {
                User      = r.GetProperty("user");
                SessionId = r.TryGetProperty("sessionId", out var s)
                    ? s.GetString() : null;
            }
            return r;
        }

        // ── Register (new customer with license key) ──────────────────────────
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

        // ── Redeem a key for an existing account ──────────────────────────────
        public async Task<JsonElement> Redeem(string username, string licenseKey)
            => await Post("/sdk/redeem", new { appId = _appId, username, licenseKey });

        // ── GetVar — fetch a server-side variable by name ─────────────────────
        public async Task<string?> GetVar(string name)
        {
            try
            {
                var res = await _http.GetAsync(
                    $"{BASE}/sdk/variable?appId={_appId}&name={name}");
                var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                return doc.RootElement.TryGetProperty("value", out var v)
                    ? v.GetString() : null;
            }
            catch { return null; }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        public string GetUsername()
            => User?.GetProperty("username").GetString() ?? "";

        public string GetPlan()
            => User?.GetProperty("plan").GetString() ?? "free";

        public bool IsPaid()
        {
            var p = GetPlan();
            return p == "paid" || p == "vip" || p == "lifetime";
        }
    }
}
