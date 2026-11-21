using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace HullBreachLauncher
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    class MainForm : Form
    {
        // Repositorio de GitHub donde se publican las versiones (Releases).
        const string Repo = "miguelpedraza-sketch/HullBreach";
        const string GameExe = "HullBreach.exe";

        static readonly Color Bg = Color.FromArgb(17, 17, 19), Card = Color.FromArgb(32, 32, 35), Orange = Color.FromArgb(255, 128, 26),
                              Dim = Color.FromArgb(150, 150, 158), TextC = Color.FromArgb(235, 235, 240);

        readonly string _root, _gameDir, _versionFile;
        Button _play, _update;
        Label _status, _versions;
        ProgressBar _bar;
        TextBox _notes;
        string _latestTag, _notesText, _zipUrl, _installed;
        bool _busy;

        public MainForm()
        {
            _root = AppDomain.CurrentDomain.BaseDirectory;
            _gameDir = Path.Combine(_root, "Game");
            _versionFile = Path.Combine(_gameDir, "version.txt");

            Text = "Hull Breach";
            ClientSize = new Size(840, 540);
            FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Bg; ForeColor = TextC; Font = new Font("Segoe UI", 10f);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            // Logo
            var pic = new PictureBox { Left = 30, Top = 20, Width = 270, Height = 270, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Bg };
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("logo.png"))
                if (s != null) pic.Image = Image.FromStream(s);
            Controls.Add(pic);

            _versions = new Label { Left = 30, Top = 296, Width = 270, Height = 44, ForeColor = Dim, Text = "Comprobando versión…" };
            Controls.Add(_versions);

            _play = MakeButton("PLAY", 30, 346, 270, 52, true);
            _play.Click += delegate { Play(); };
            _update = MakeButton("ACTUALIZAR", 30, 408, 270, 44, false);
            _update.Click += delegate { StartUpdate(); };
            Controls.Add(_play); Controls.Add(_update);

            // Cambios
            var head = new Label { Left = 330, Top = 24, Width = 480, Height = 30, Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = Orange, Text = "NOVEDADES" };
            Controls.Add(head);
            _notes = new TextBox
            {
                Left = 330, Top = 60, Width = 480, Height = 392, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                BackColor = Card, ForeColor = TextC, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10f), Text = ""
            };
            Controls.Add(_notes);

            _bar = new ProgressBar { Left = 30, Top = 474, Width = 780, Height = 10, Style = ProgressBarStyle.Continuous, Visible = false };
            Controls.Add(_bar);
            _status = new Label { Left = 30, Top = 492, Width = 780, Height = 30, ForeColor = Dim, Text = "" };
            Controls.Add(_status);

            _installed = ReadInstalled();
            SetButtons();
            Shown += delegate { CheckLatest(); };
        }

        Button MakeButton(string text, int x, int y, int w, int h, bool primary)
        {
            var b = new Button
            {
                Text = text, Left = x, Top = y, Width = w, Height = h, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", primary ? 14f : 11f, FontStyle.Bold), UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = primary ? 0 : 2;
            Style(b, primary);
            return b;
        }

        static void Style(Button b, bool primary)
        {
            bool on = b.Enabled;
            if (primary) { b.BackColor = on ? Orange : Color.FromArgb(52, 52, 56); b.ForeColor = on ? Color.FromArgb(20, 20, 22) : Color.FromArgb(110, 110, 118); }
            else { b.BackColor = Bg; b.ForeColor = on ? Color.White : Color.FromArgb(110, 110, 118); b.FlatAppearance.BorderColor = on ? Color.White : Color.FromArgb(70, 70, 76); }
        }

        string ReadInstalled()
        {
            try { if (File.Exists(_versionFile) && File.Exists(Path.Combine(_gameDir, GameExe))) return File.ReadAllText(_versionFile).Trim(); } catch { }
            return null;
        }

        static string Norm(string v) { return (v ?? "").Trim().TrimStart('v', 'V'); }

        bool UpToDate { get { return _installed != null && _latestTag != null && Norm(_installed) == Norm(_latestTag); } }

        void SetButtons()
        {
            bool offline = _latestTag == null;
            // Play: solo con la última versión instalada (si no hay conexión, deja jugar lo que haya instalado).
            _play.Enabled = !_busy && _installed != null && (UpToDate || offline);
            bool needs = _latestTag != null && !UpToDate;
            _update.Enabled = !_busy && needs;
            _update.Text = _installed == null ? "INSTALAR" : "ACTUALIZAR";
            Style(_play, true); Style(_update, false);
        }

        void Ui(Action a) { if (IsHandleCreated) BeginInvoke(a); }

        void Status(string s) { Ui(delegate { _status.Text = s; }); }

        // ------------------------------------------------------------------ Comprobar última versión
        void CheckLatest()
        {
            _status.Text = "Buscando actualizaciones…";
            var t = new Thread(delegate ()
            {
                try
                {
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;   // TLS 1.2
                    try { FetchFromApi(); }
                    catch { FetchWithoutApi(); }   // la API de GitHub limita a 60 consultas/hora por IP (error 403): se usa la vía sin límite
                    Ui(delegate
                    {
                        _notes.Text = (_notesText ?? "").Replace("\r\n", "\n").Replace("\n", "\r\n");
                        ShowVersions();
                        SetButtons();
                        _status.Text = UpToDate ? "Todo al día. ¡A jugar!" : (_installed == null ? "Instala el juego para poder jugar." : "Hay una versión nueva: actualiza para jugar.");
                    });
                }
                catch (Exception ex)
                {
                    _latestTag = null;
                    string msg = ex.Message;
                    Ui(delegate
                    {
                        ShowVersions(); SetButtons();
                        _notes.Text = "No se pudieron cargar las novedades.";
                        _status.Text = "Sin conexión con el servidor de actualizaciones (" + msg + ")";
                    });
                }
            });
            t.IsBackground = true; t.Start();
        }

        void FetchFromApi()
        {
            var req = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/" + Repo + "/releases/latest");
            req.UserAgent = "HullBreachLauncher"; req.Accept = "application/vnd.github+json"; req.Timeout = 15000;
            string json;
            using (var resp = req.GetResponse())
            using (var sr = new StreamReader(resp.GetResponseStream())) json = sr.ReadToEnd();
            var ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var d = (Dictionary<string, object>)ser.DeserializeObject(json);
            string tag = Convert.ToString(d["tag_name"]);
            string notes = d.ContainsKey("body") && d["body"] != null ? Convert.ToString(d["body"]) : "";
            string zipUrl = null;
            foreach (var a in (object[])d["assets"])
            {
                var ad = (Dictionary<string, object>)a;
                string name = Convert.ToString(ad["name"]);
                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) { zipUrl = Convert.ToString(ad["browser_download_url"]); break; }
            }
            _latestTag = tag; _notesText = notes; _zipUrl = zipUrl;
        }

        // Sin API: la página /releases/latest redirige a /releases/tag/<etiqueta>; el zip se llama HullBreach-X.Y.zip y las
        // notas están en RELEASE_NOTES_X.Y.md del repositorio. Ninguna de estas direcciones cuenta para el límite de la API.
        void FetchWithoutApi()
        {
            var req = (HttpWebRequest)WebRequest.Create("https://github.com/" + Repo + "/releases/latest");
            req.UserAgent = "HullBreachLauncher"; req.AllowAutoRedirect = false; req.Method = "HEAD"; req.Timeout = 15000;
            string loc;
            using (var resp = (HttpWebResponse)req.GetResponse()) loc = resp.Headers["Location"];
            if (string.IsNullOrEmpty(loc) || loc.IndexOf("/tag/", StringComparison.Ordinal) < 0) throw new Exception("no se pudo leer la última versión");
            string tag = Uri.UnescapeDataString(loc.Substring(loc.LastIndexOf("/tag/", StringComparison.Ordinal) + 5)).Trim();
            string ver = Norm(tag);
            string notes = "";
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Headers[HttpRequestHeader.UserAgent] = "HullBreachLauncher"; wc.Encoding = System.Text.Encoding.UTF8;
                    notes = wc.DownloadString("https://raw.githubusercontent.com/" + Repo + "/main/RELEASE_NOTES_" + ver + ".md");
                }
            }
            catch { }
            _latestTag = tag; _notesText = notes;
            _zipUrl = "https://github.com/" + Repo + "/releases/download/" + tag + "/HullBreach-" + ver + ".zip";
        }

        void ShowVersions()
        {
            _versions.Text = "Instalada: " + (_installed == null ? "—" : Norm(_installed)) + "\nÚltima: " + (_latestTag == null ? "?" : Norm(_latestTag));
        }

        // ------------------------------------------------------------------ Actualizar
        void StartUpdate()
        {
            if (_busy) return;
            if (_zipUrl == null) { _status.Text = "La versión publicada no tiene archivo de juego (.zip)."; return; }
            _busy = true; SetButtons();
            _bar.Visible = true; _bar.Value = 0;
            var t = new Thread(DoUpdate);
            t.IsBackground = true; t.Start();
        }

        void DoUpdate()
        {
            string zip = Path.Combine(Path.GetTempPath(), "HullBreach_update.zip");
            string tmp = Path.Combine(_root, "Game.tmp");
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                Status("Descargando " + Norm(_latestTag) + "…");
                var done = new ManualResetEvent(false); Exception derr = null;
                using (var wc = new WebClient())
                {
                    wc.Headers[HttpRequestHeader.UserAgent] = "HullBreachLauncher";
                    wc.DownloadProgressChanged += delegate (object s, DownloadProgressChangedEventArgs e)
                    {
                        int pc = e.ProgressPercentage; long got = e.BytesReceived / 1048576, tot = e.TotalBytesToReceive / 1048576;
                        Ui(delegate { _bar.Value = Math.Min(100, pc); _status.Text = "Descargando… " + got + " / " + tot + " MB"; });
                    };
                    wc.DownloadFileCompleted += delegate (object s, System.ComponentModel.AsyncCompletedEventArgs e) { derr = e.Error; done.Set(); };
                    wc.DownloadFileAsync(new Uri(_zipUrl), zip);
                    done.WaitOne();
                }
                if (derr != null) throw derr;

                Status("Instalando…");
                if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
                Directory.CreateDirectory(tmp);
                string tmpFull = Path.GetFullPath(tmp);
                using (var za = ZipFile.OpenRead(zip))
                {
                    int n = za.Entries.Count, i = 0;
                    foreach (var en in za.Entries)
                    {
                        i++;
                        string dest = Path.GetFullPath(Path.Combine(tmp, en.FullName));
                        if (!dest.StartsWith(tmpFull, StringComparison.OrdinalIgnoreCase)) continue;   // protección ante rutas raras
                        if (en.FullName.EndsWith("/")) { Directory.CreateDirectory(dest); continue; }
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        en.ExtractToFile(dest, true);
                        if (i % 20 == 0) { int p = i * 100 / n; Ui(delegate { _bar.Value = p; _status.Text = "Instalando… " + p + "%"; }); }
                    }
                }
                File.WriteAllText(Path.Combine(tmp, "version.txt"), _latestTag);
                if (Directory.Exists(_gameDir)) Directory.Delete(_gameDir, true);
                Directory.Move(tmp, _gameDir);
                try { File.Delete(zip); } catch { }

                Ui(delegate
                {
                    _installed = ReadInstalled(); _busy = false;
                    _bar.Visible = false; ShowVersions(); SetButtons();
                    _status.Text = "Listo: Hull Breach " + Norm(_latestTag) + " instalado.";
                });
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                try { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); } catch { }
                Ui(delegate { _busy = false; _bar.Visible = false; SetButtons(); _status.Text = "Error al actualizar: " + msg; });
            }
        }

        // ------------------------------------------------------------------ Jugar
        void Play()
        {
            try
            {
                string exe = Path.Combine(_gameDir, GameExe);
                Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = _gameDir, UseShellExecute = true });
                Close();
            }
            catch (Exception ex) { _status.Text = "No se pudo iniciar el juego: " + ex.Message; }
        }
    }
}
