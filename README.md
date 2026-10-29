<div align="center">

# Onyx Gate — C# SDK Example

**A C# .NET 8 SDK for [Onyx Gate](https://auth.script-kittens.com). Async, no heavy dependencies.**

</div>

---

## Files

| File | What it is |
| :--- | :--- |
| `SKAuth.cs` | Drop-in SDK class — add it to any .NET 8 Windows project |

Requires the `System.Management` NuGet package (for the HWID).

## Quick start

```bash
git clone https://github.com/ScriptKittenss/Onyx-Gate-CSharp-Example.git
```

Add `SKAuth.cs` to your project, install `System.Management`, then:

```csharp
var auth = new SKAuth("YOUR_APP_ID", "1.0");

var res = await auth.Login("username", "password");
if (!res.GetProperty("ok").GetBoolean())
{
    MessageBox.Show(res.GetProperty("message").GetString());
    return;
}

MessageBox.Show($"Welcome {auth.GetUsername()}! Plan: {auth.GetPlan()}");

if (auth.IsPaid())
{
    // unlock premium features
}
```

## API reference

| Method | Description |
| :--- | :--- |
| `new SKAuth(appId, version = "1.0")` | Create the client. Computes the machine HWID. |
| `Login(username, password)` | Authenticate. On success `User` holds `username`, `plan`, `expires`, `email`, `apiKey`. On failure show the `message` as-is. |
| `Register(username, password, email = "", licenseKey = "")` | Create an account. `licenseKey` is optional. |
| `Redeem(username, licenseKey)` | Apply a license key to an existing account. |
| `Validate()` | Heartbeat — call every few minutes. Kicks banned/expired users, syncs plan changes. Needs a login first. |
| `GetVar(name)` | Read a runtime variable (dashboard → Settings → Variables). Returns the value or `null`. |
| `DownloadFile(fileId, savePath)` | Download a file from the Runtime Vault (dashboard → Files). Returns `true` on success. Needs a login first. |
| `IsPaid()` | `true` when the plan isn't `free` (starter, pro, …). |
| `GetHWID()` | The machine HWID (SHA256, 32 hex chars). Same machine gives the same HWID in the Python and C++ SDKs. |
| `GetUsername()` / `GetPlan()` / `GetExpires()` | Shortcuts into `User`. |

## Notes

- The App ID is public. Never put your app secret, a password, or a license key in source code.
- `version` must match the version set in your dashboard, or login returns "Update required".
- Logins are rate limited (15/min per IP), registrations 5 per 5 minutes per IP.
- Full endpoint docs: https://auth.script-kittens.com → Documentation.

---

<div align="center">
<sub>Powered by <b>Onyx Gate</b> • <a href="https://github.com/ScriptKittenss">ScriptKittenss</a></sub>
</div>
