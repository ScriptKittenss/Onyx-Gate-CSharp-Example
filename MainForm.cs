// ============================================================
//  MainForm.cs  —  Opens after successful login
//  _auth.GetUsername()  → logged-in username
//  _auth.GetPlan()      → "free" / "starter" / "pro" (any non-free = paid)
//  _auth.IsPaid()       → true if paid plan
// ============================================================

using System;
using System.Windows.Forms;

namespace OnyxGateExample
{
    public partial class MainForm : Form
    {
        private readonly SKAuth _auth;

        public MainForm(SKAuth auth)
        {
            _auth = auth;
            InitializeComponent();

            // Fill header info
            lblUser.Text = $"{_auth.GetUsername()}  ·  {_auth.GetPlan().ToUpper()}";

            // Load default section
            LoadSection("Aimbot");
        }

        // ── Sidebar button clicks ─────────────────────────────────────────────
        private void btnAimbot_Click  (object s, EventArgs e) => LoadSection("Aimbot");
        private void btnESP_Click     (object s, EventArgs e) => LoadSection("ESP");
        private void btnVisuals_Click (object s, EventArgs e) => LoadSection("Visuals");
        private void btnMisc_Click    (object s, EventArgs e) => LoadSection("Misc");
        private void btnSettings_Click(object s, EventArgs e) => LoadSection("Settings");

        // ── Section loader ────────────────────────────────────────────────────
        private void LoadSection(string name)
        {
            panelContent.Controls.Clear();

            bool needsPaid = name == "Aimbot" || name == "ESP";

            if (needsPaid && !_auth.IsPaid())
            {
                var lbl = new Label
                {
                    Text      = $"⚠  {name} requires a paid plan.\n\n" +
                                "Buy a key at discord.gg/tWwUSPh5GT",
                    ForeColor = System.Drawing.Color.FromArgb(200, 155, 55),
                    Font      = new System.Drawing.Font("Segoe UI", 10.5f),
                    Location  = new System.Drawing.Point(12, 16),
                    AutoSize  = true,
                };
                panelContent.Controls.Add(lbl);
                return;
            }

            var title = new Label
            {
                Text      = name,
                ForeColor = System.Drawing.Color.FromArgb(215, 195, 165),
                Font      = new System.Drawing.Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold),
                Location  = new System.Drawing.Point(12, 8),
                AutoSize  = true,
            };
            panelContent.Controls.Add(title);

            var features = name switch
            {
                "Aimbot"   => new[] { "Enable Aimbot", "Aim at Head", "Aim at Neck", "FOV Circle", "Smoothing" },
                "ESP"      => new[] { "Player Box", "Health Bar", "Distance", "Name Tags", "Skeleton" },
                "Visuals"  => new[] { "No Recoil", "No Spread", "Bright Mode", "Custom FOV" },
                "Misc"     => new[] { "Bunny Hop", "Speed Hack", "Auto-reload", "Panic Key (END)" },
                "Settings" => new[] { "Show FPS", "Watermark", "Auto-update" },
                _          => System.Array.Empty<string>(),
            };

            int y = 46;
            foreach (var f in features)
            {
                var cb = new CheckBox
                {
                    Text      = f,
                    ForeColor = System.Drawing.Color.FromArgb(195, 180, 155),
                    Location  = new System.Drawing.Point(12, y),
                    AutoSize  = true,
                    Font      = new System.Drawing.Font("Segoe UI", 9.5f),
                };
                panelContent.Controls.Add(cb);
                y += 32;
            }
        }
    }
}
