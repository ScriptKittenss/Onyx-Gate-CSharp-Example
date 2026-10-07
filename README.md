<div align="center">

# ⚡ Onyx Gate — C# WinForms Example

[![Language](https://img.shields.io/badge/Language-C%23%2012-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://dotnet.microsoft.com)
[![Framework](https://img.shields.io/badge/Framework-.NET%208.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download)
[![UI](https://img.shields.io/badge/UI-Windows%20Forms-0078D6?style=for-the-badge&logo=windows&logoColor=white)](https://learn.microsoft.com/dotnet/desktop/winforms)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)

<br/>

**A modern, asynchronous C# Windows Forms authentication client and SDK for [Onyx Gate](https://auth.script-kittens.com).**

</div>

---

## 🚀 Overview

This example provides a complete GUI client implementing asynchronous login, registration, key redemption, and session validation using the modern .NET 8 SDK and Windows Forms.

### ✨ Features
- ⚡ **Asynchronous Flow** — Non-blocking UI operations (`async` / `await` with `HttpClient`).
- 🛡️ **HWID Hardware Fingerprinting** — Built-in Windows WMI / registry hardware profiling.
- 📦 **Zero External Dependencies** — Built with native .NET 8 runtime features without heavy third-party packages.
- 🎨 **Clean WinForms UI** — Pre-built `LoginForm` and feature dashboard `MainForm`.

---

## 📂 Project Structure

```text
├── SKAuth.cs               # Drop-in Onyx Gate SDK for C#
├── LoginForm.cs            # Login & Register UI Form
├── MainForm.cs             # Authenticated Dashboard / Feature Menu
├── Program.cs              # Application Entry Point
├── OnyxGate-CSharp.sln     # Visual Studio Solution
└── OnyxGate-CSharp.csproj  # .NET 8 Project File
```

---

## ⚡ Quick Start

### 1. Requirements
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (with *.NET desktop development* workload)

### 2. Setup
1. Clone the repository:
   ```bash
   git clone https://github.com/fegrial/Onyx-Gate-CSharp-Example.git
   ```
2. Open `OnyxGate-CSharp.sln` in Visual Studio 2022.
3. In `LoginForm.cs`, configure your `APP_ID`:
   ```csharp
   private readonly string APP_ID = "YOUR_APP_ID_HERE";
   ```
4. Build in **Release | Any CPU** and launch!

---

## 💻 Code Integration

Add Onyx Gate to any existing C# application in 3 steps:

### 1. Initialize Auth Client
```csharp
using OnyxGate;

public class App {
    private readonly SKAuth auth = new SKAuth("YOUR_APP_ID", "1.0");
}
```

### 2. Authenticate User
```csharp
var response = await auth.Login("username", "password");

if (response.GetProperty("ok").GetBoolean()) {
    string plan = response.GetProperty("data").GetProperty("plan").GetString();
    Console.WriteLine($"Welcome! Active plan: {plan}");
    
    // Check subscription gate
    if (auth.IsPaid()) {
        ShowPaidFeatures();
    }
} else {
    string error = response.GetProperty("message").GetString();
    MessageBox.Show(error, "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
```

### 3. Key Activation
```csharp
var result = await auth.RedeemKey("ONYX-XXXX-XXXX-XXXX");
if (result.GetProperty("ok").GetBoolean()) {
    MessageBox.Show("License key successfully redeemed!");
}
```

---

## 🛡️ API Reference

| Method | Description |
| :--- | :--- |
| `SKAuth(appId, version)` | Creates an auth client instance. |
| `Task<JsonElement> Login(user, pass)` | Logs in user and validates HWID. |
| `Task<JsonElement> Register(user, pass, email, key)` | Creates a new user profile. |
| `Task<JsonElement> RedeemKey(key)` | Redeems license key to activate plan. |
| `bool IsPaid()` | Returns `true` if current user has an active paid plan. |
| `string GetHWID()` | Computes machine hardware ID. |

---

<div align="center">
<sub>Powered by <b>Onyx Gate</b> • Maintained by <a href="https://github.com/fegrial">fegrial</a></sub>
</div>
