using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

// NuGet: Install-Package TagLibSharp
using TagLib;

namespace MP3Player
{
    public class MainForm : Form
    {
        private WebView2 webView;
        private string downloadPath;
        private bool webViewReady = false;

        public MainForm()
        {
            Text = "Music";
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1100, 750);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.Black;

            webView = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(webView);
            Load += MainForm_Load;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                await webView.EnsureCoreWebView2Async();

                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

                webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;

                string exeDir = AppDomain.CurrentDomain.BaseDirectory;

                downloadPath = Path.Combine(exeDir, "Downloads", "Spotify");
                Directory.CreateDirectory(downloadPath);


                string downloadsRoot = Path.Combine(exeDir, "Downloads");
                Directory.CreateDirectory(downloadsRoot);

                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "music.local",
                    downloadsRoot,
                    Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);


                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = "MP3Player.music-player.html";

                using Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    MessageBox.Show($"Embedded resource not found:\n{resourceName}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using StreamReader reader = new StreamReader(stream);
                string html = reader.ReadToEnd();

                string htmlPath = Path.Combine(exeDir, "music-player.html");
                System.IO.File.WriteAllText(htmlPath, html, System.Text.Encoding.UTF8);

                webView.CoreWebView2.NavigationCompleted += async (s2, e2) =>
                {
                    if (!e2.IsSuccess) return;
                    await LoadSongsFromFolder(downloadPath);

                    string downloadsRootCheck = Path.Combine(exeDir, "Downloads");
                    if (Directory.Exists(downloadsRootCheck))
                    {
                        foreach (string subDir in Directory.GetDirectories(downloadsRootCheck))
                        {
                            if (!subDir.Equals(downloadPath, StringComparison.OrdinalIgnoreCase))
                                await LoadSongsFromFolder(subDir);
                        }
                    }
                };

                webView.CoreWebView2.Navigate("file:///" + htmlPath.Replace('\\', '/'));

                webViewReady = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void WebView_WebMessageReceived(object sender,
            Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var msg = JsonSerializer.Deserialize<WebMessage>(e.WebMessageAsJson);
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;

                if (msg.type == "loadDownloads")
                {
                    await LoadSongsFromFolder(downloadPath);

                    string downloadsRoot = Path.Combine(exeDir, "Downloads");
                    if (Directory.Exists(downloadsRoot))
                    {
                        foreach (string subDir in Directory.GetDirectories(downloadsRoot))
                        {
                            if (!subDir.Equals(downloadPath, StringComparison.OrdinalIgnoreCase))
                                await LoadSongsFromFolder(subDir);
                        }
                    }
                }
                else if (msg.type == "importSpotify")
                {
                    await RunSpotifyDownload(msg.url, exeDir);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("WebMessage error: " + ex.Message);
            }
        }

        private async Task RunSpotifyDownload(string url, string exeDir)
        {
            string logPath = Path.Combine(exeDir, "spotify-download.log");
            string ffmpegBin = Path.Combine(exeDir, "ffmpeg", "bin");

            if (!System.IO.File.Exists(logPath))
                System.IO.File.WriteAllText(logPath, "=== Spotify Downloader Log ===\n");

            System.IO.File.AppendAllText(logPath, $"\n[{DateTime.Now}] Starting: {url}\n");

            string fullCmd = $"/C cd \"{exeDir}\" && set \"PATH=%PATH%;{ffmpegBin}\" " +
                             $"&& spotify-dl.exe -o \"{downloadPath}\" \"{url}\" >> \"{logPath}\" 2>&1";

            var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = fullCmd,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            });

            if (proc != null)
            {
                while (!proc.HasExited)
                {
                    await Task.Delay(5000);
                    await LoadSongsFromFolder(downloadPath);
                }
                await Task.Delay(1000);
                await LoadSongsFromFolder(downloadPath);
            }
        }

        private async Task LoadSongsFromFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;

            try
            {
                var extensions = new[] { "*.mp3", "*.flac", "*.wav", "*.ogg", "*.m4a", "*.aac" };
                var songList = new List<object>();
                int id = 0;

                foreach (var ext in extensions)
                {
                    foreach (var filePath in Directory.GetFiles(folderPath, ext, SearchOption.AllDirectories))
                    {
                        string title = Path.GetFileNameWithoutExtension(filePath);
                        string artist = "Unknown Artist";
                        string album = "Unknown Album";
                        string coverUrl = null;

                        try
                        {
                            using var tagFile = TagLib.File.Create(filePath);
                            var tag = tagFile.Tag;

                            if (!string.IsNullOrWhiteSpace(tag.Title))
                                title = tag.Title;

                            if (tag.Performers?.Length > 0 && !string.IsNullOrWhiteSpace(tag.Performers[0]))
                                artist = string.Join(", ", tag.Performers);
                            else if (tag.AlbumArtists?.Length > 0 && !string.IsNullOrWhiteSpace(tag.AlbumArtists[0]))
                                artist = string.Join(", ", tag.AlbumArtists);

                            if (!string.IsNullOrWhiteSpace(tag.Album))
                                album = tag.Album;

                            if (tag.Pictures?.Length > 0)
                            {
                                var pic = tag.Pictures[0];
                                string mimeType = pic.MimeType;

                                if (string.IsNullOrWhiteSpace(mimeType) || !mimeType.StartsWith("image/"))
                                {
                                    mimeType = DetectImageMime(pic.Data.Data);
                                }

                                string b64 = Convert.ToBase64String(pic.Data.Data);
                                coverUrl = $"data:{mimeType};base64,{b64}";
                            }
                        }
                        catch
                        {
                            string fileName = Path.GetFileNameWithoutExtension(filePath);
                            int dashIdx = fileName.IndexOf(" - ");
                            if (dashIdx > 0)
                            {
                                artist = fileName.Substring(0, dashIdx).Trim();
                                title = fileName.Substring(dashIdx + 3).Trim();
                            }
                        }

                        string downloadsRoot2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads");
                        string relativePath = filePath.Substring(downloadsRoot2.Length).TrimStart('\\', '/');
                        string fileUrl = "https://music.local/" + relativePath.Replace('\\', '/');

                        songList.Add(new
                        {
                            id = id++,
                            title,
                            artist,
                            album,
                            url = fileUrl,
                            coverUrl  
                        });
                    }
                }

                string jsonSongs = JsonSerializer.Serialize(songList);

                string script = $"if(typeof loadSongsFromBackend==='function') loadSongsFromBackend({jsonSongs});";
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                await webView.CoreWebView2.ExecuteScriptAsync(
                    $"console.error('LoadSongs error: {EscapeJs(ex.Message)}');");
            }
        }

 
        private static string DetectImageMime(byte[] data)
        {
            if (data == null || data.Length < 4) return "image/jpeg";

            if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                return "image/jpeg";

            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return "image/png";

            if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
                return "image/gif";

            if (data.Length >= 12 &&
                data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
                data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
                return "image/webp";

            return "image/jpeg"; // safe default
        }

        private static string EscapeJs(string s)
            => (s ?? "").Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", " ").Replace("\r", "");

        public class WebMessage
        {
            public string type { get; set; }
            public string url { get; set; }
        }
    }

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
}