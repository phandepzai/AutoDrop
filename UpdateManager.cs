using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

#region THÔNG BÁO PHIÊN BẢN MỚI

public static class UpdateManager
{
    #region CẤU HÌNH
    // ⚙️ CẤU HÌNH CƠ BẢN (Có thể tùy chỉnh)
    private static int CHECK_INTERVAL_HOURS = 12;           // Kiểm tra mỗi 12 giờ
    private static readonly int HTTP_TIMEOUT_SECONDS = 8;            // Timeout khi tải file
    private static bool ENABLE_EPS_UNLOCK = false;          // Bật/tắt tính năng unlock EPS
    private static string[] ALLOWED_IP_PREFIXES = new[]     // Dải IP được phép unlock
    {
        "107.126.",
        "107.115."
    };
    private static string[] BLOCKED_IP_PREFIXES = new[]     // Dải IP bị chặn unlock
    {
        "107.125."
    };

    private static string UNLOCK_BAT_BASE_URL = null;
    #endregion

    #region BIẾN NỘI BỘ
    private static System.Windows.Forms.Timer _updateCheckTimer;
    private static DateTime _lastCheckTime = DateTime.MinValue;
    #endregion

    #region PUBLIC API
    /// <summary>
    /// Khởi tạo tự động kiểm tra cập nhật
    /// </summary>
    /// <param name="exeName">Tên file .exe (vd: "MyApp.exe")</param>
    /// <param name="httpServers">Danh sách HTTP servers</param>
    /// <param name="checkIntervalHours">Kiểm tra mỗi bao nhiêu giờ (mặc định 12)</param>
    /// <param name="enableEpsUnlock">Có bật unlock EPS không (mặc định false)</param>
    public static void InitializeAutoCheck(
        string exeName,
        string[] httpServers,
        int checkIntervalHours = 12,
        bool enableEpsUnlock = false,
        string unlockBatBaseUrl = null)
    {
        CHECK_INTERVAL_HOURS = checkIntervalHours;
        ENABLE_EPS_UNLOCK = enableEpsUnlock;

        // ✅ CHUẨN HÓA VÀ ENCODE URL
        if (!string.IsNullOrEmpty(unlockBatBaseUrl))
        {
            // Loại bỏ khoảng trắng đầu/cuối
            unlockBatBaseUrl = unlockBatBaseUrl.Trim();

            // ⭐ ENCODE CÁC PHẦN PATH (giữ nguyên protocol và domain)
            UNLOCK_BAT_BASE_URL = NormalizeHttpUrl(unlockBatBaseUrl);

            Debug.WriteLine($"[Init] Unlock URL: {UNLOCK_BAT_BASE_URL}");
        }
        StopAutoCheck();
        CheckForUpdates(exeName, httpServers);
        _lastCheckTime = DateTime.Now;

        _updateCheckTimer = new System.Windows.Forms.Timer
        {
            Interval = CHECK_INTERVAL_HOURS * 60 * 60 * 1000
        };

        _updateCheckTimer.Tick += (s, e) =>
        {
            try
            {
                TimeSpan timeSinceLastCheck = DateTime.Now - _lastCheckTime;
                if (timeSinceLastCheck.TotalHours >= CHECK_INTERVAL_HOURS)
                {
                    Debug.WriteLine($"[Auto Check] Đã {timeSinceLastCheck.TotalHours:F1} giờ, kiểm tra cập nhật...");
                    CheckForUpdates(exeName, httpServers);
                    _lastCheckTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Auto Check] Lỗi trong timer: {ex.Message}");
                InitializeAutoCheck(exeName, httpServers, checkIntervalHours, enableEpsUnlock);
            }
        };

        _updateCheckTimer.Start();
        Debug.WriteLine($"[Auto Check] Timer đã khởi động - kiểm tra mỗi {CHECK_INTERVAL_HOURS} giờ");
    }
    #region URL HELPERS
    /// <summary>
    /// Chuẩn hóa HTTP URL - Encode dấu cách và ký tự đặc biệt
    /// </summary>
    private static string NormalizeHttpUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        try
        {
            // Tách protocol, domain và path
            Uri uri = new Uri(url, UriKind.Absolute);

            // ⭐ ENCODE PATH (dấu cách → %20)
            string encodedPath = string.Join("/",
                uri.AbsolutePath.Split('/')
                    .Select(segment => Uri.EscapeDataString(segment))
            );

            // Ghép lại URL
            string normalizedUrl = $"{uri.Scheme}://{uri.Host}";

            if (uri.Port != 80 && uri.Port != 443)
                normalizedUrl += $":{uri.Port}";

            normalizedUrl += encodedPath;

            // Đảm bảo kết thúc bằng "/"
            if (!normalizedUrl.EndsWith("/"))
                normalizedUrl += "/";

            Debug.WriteLine($"[URL Normalize] {url} → {normalizedUrl}");
            return normalizedUrl;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[URL Normalize] Lỗi: {ex.Message}, giữ nguyên URL gốc");
            return url.TrimEnd('/') + "/";
        }
    }

    /// <summary>
    /// Ghép URL an toàn (tự động encode)
    /// </summary>
    private static string CombineUrl(string baseUrl, string relativePath)
    {
        if (string.IsNullOrEmpty(baseUrl))
            return relativePath;

        if (string.IsNullOrEmpty(relativePath))
            return baseUrl;

        // Loại bỏ "/" thừa
        baseUrl = baseUrl.TrimEnd('/');
        relativePath = relativePath.TrimStart('/');

        // ⭐ ENCODE phần relativePath
        string encodedPath = string.Join("/",
            relativePath.Split('/')
                .Select(segment => Uri.EscapeDataString(segment))
        );

        return $"{baseUrl}/{encodedPath}";
    }
    #endregion
    /// <summary>
    /// Cấu hình dải IP cho unlock EPS
    /// </summary>
    public static void ConfigureIPRanges(string[] allowedPrefixes, string[] blockedPrefixes = null)
    {
        ALLOWED_IP_PREFIXES = allowedPrefixes ?? new string[0];
        BLOCKED_IP_PREFIXES = blockedPrefixes ?? new string[0];
    }

    /// <summary>
    /// Khởi động lại timer nếu bị dừng
    /// </summary>
    public static void RestartTimerIfStopped(string exeName, string[] httpServers)
    {
        if (_updateCheckTimer == null || !_updateCheckTimer.Enabled)
        {
            Debug.WriteLine("[Auto Check] Timer bị dừng, khởi động lại...");
            InitializeAutoCheck(exeName, httpServers, CHECK_INTERVAL_HOURS, ENABLE_EPS_UNLOCK);
        }
    }

    /// <summary>
    /// Dừng kiểm tra tự động
    /// </summary>
    public static void StopAutoCheck()
    {
        if (_updateCheckTimer != null)
        {
            _updateCheckTimer.Stop();
            _updateCheckTimer.Dispose();
            _updateCheckTimer = null;
            Debug.WriteLine("[Auto Check] Timer đã dừng");
        }
    }
    #endregion

    #region KIỂM TRA CẬP NHẬT
    /// <summary>
    /// Kiểm tra phiên bản mới
    /// </summary>
    public static async void CheckForUpdates(string exeName, string[] httpServers)
    {
        try
        {
            string currentVersion = Application.ProductVersion;
            string latestVersion = null;
            string changelog = "";
            string workingServerUrl = null;

            Debug.WriteLine($"[Cập nhật] Phiên bản hiện tại: {currentVersion}");

            // Kiểm tra version qua HTTP
            var httpResult = await TryCheckVersionViaHTTP(httpServers);
            if (!httpResult.Success)
            {
                Debug.WriteLine("[Cập nhật] ❌ Không thể kết nối đến server cập nhật!");
                return;
            }

            latestVersion = httpResult.Version;
            workingServerUrl = httpResult.ServerUrl;
            Debug.WriteLine($"[Cập nhật] ✅ HTTP thành công! Phiên bản: {latestVersion}");

            // Lấy changelog
            changelog = await GetChangelogSafe(workingServerUrl);

            // So sánh version
            if (string.Compare(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0)
            {
                Debug.WriteLine($"[Cập nhật] Đã có phiên bản mới: {latestVersion} > {currentVersion}");
                ShowUpdatePrompt(latestVersion, changelog, workingServerUrl, exeName);
            }
            else
            {
                Debug.WriteLine($"[Cập nhật] Đã cập nhật: {currentVersion}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Cập nhật bị lỗi] {ex.Message}");
        }
    }

    private static async Task<(bool Success, string Version, string ServerUrl)> TryCheckVersionViaHTTP(string[] servers)
    {
        using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(HTTP_TIMEOUT_SECONDS) })
        {
            foreach (var server in servers)
            {
                try
                {
                    string url = server.TrimEnd('/') + "/version.txt";
                    Debug.WriteLine($"[HTTP] Đang thử: {url}");
                    string version = (await client.GetStringAsync(url)).Trim();
                    return (true, version, server.TrimEnd('/'));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HTTP] Lỗi {server}: {ex.Message}");
                }
            }
        }
        return (false, null, null);
    }

    private static async Task<string> GetChangelogSafe(string serverUrl)
    {
        try
        {
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(HTTP_TIMEOUT_SECONDS) })
            {
                return await client.GetStringAsync(serverUrl + "/changelog.txt");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Changelog] Lỗi: {ex.Message}");
        }
        return "(Không có thông tin thay đổi)";
    }
    #endregion

    #region EPS UNLOCK (OPTIONAL)
    // Tìm và tải tất cả file .bat unlock qua HTTP
    private static async Task<List<string>> FindAllUnlockBatAsync(IProgress<string> progress, string serverUrl = null)
    {
        var batFiles = new List<string>();

        // PHẦN 1: QUÉT QUA HTTP (ƯU TIÊN)
        string unlockBaseUrl = UNLOCK_BAT_BASE_URL;

        if (string.IsNullOrEmpty(unlockBaseUrl) && !string.IsNullOrEmpty(serverUrl))
        {
            unlockBaseUrl = serverUrl.TrimEnd('/') + "/unlock/";
        }

        if (!string.IsNullOrEmpty(unlockBaseUrl))
        {
            try
            {
                // ⭐ CHUẨN HÓA URL
                unlockBaseUrl = NormalizeHttpUrl(unlockBaseUrl);
                progress?.Report($"🌐 Đang quét HTTP: {unlockBaseUrl}");

                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(HTTP_TIMEOUT_SECONDS) })
                {
                    // ⭐ DÙNG HÀM CombineUrl THAY VÌ GHÉP TAY
                    string listUrl = CombineUrl(unlockBaseUrl, "list.txt");

                    try
                    {
                        string fileList = await client.GetStringAsync(listUrl);
                        var httpBatFiles = fileList
                            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(f => f.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
                            .Select(f => f.Trim())
                            .ToList();

                        if (httpBatFiles.Any())
                        {
                            progress?.Report($"✅ Tìm thấy {httpBatFiles.Count} file .bat trên HTTP");

                            foreach (var fileName in httpBatFiles)
                            {
                                // ⭐ DÙNG CombineUrl ĐỂ ENCODE AN TOÀN
                                string downloadUrl = CombineUrl(unlockBaseUrl, fileName);
                                string tempBatPath = Path.Combine(Path.GetTempPath(), fileName);

                                try
                                {
                                    progress?.Report($"⬇️ Đang tải: {fileName}");
                                    byte[] batContent = await client.GetByteArrayAsync(downloadUrl);
                                    File.WriteAllBytes(tempBatPath, batContent);
                                    batFiles.Add(tempBatPath);
                                    progress?.Report($"✅ Đã tải: {fileName}");
                                }
                                catch (Exception ex)
                                {
                                    progress?.Report($"⚠️ Lỗi tải {fileName}: {ex.Message}");
                                }
                            }
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        progress?.Report($"⚠️ Không tìm thấy list.txt: {ex.Message}");
                        progress?.Report("ℹ️ Thử tải file mặc định...");

                        var defaultBatFiles = new[] { "unlock_eps.bat", "disable_eps.bat", "unblock_printer.bat", "unlock.bat" };

                        foreach (var fileName in defaultBatFiles)
                        {
                            // ⭐ DÙNG CombineUrl
                            string downloadUrl = CombineUrl(unlockBaseUrl, fileName);
                            string tempBatPath = Path.Combine(Path.GetTempPath(), fileName);

                            try
                            {
                                byte[] batContent = await client.GetByteArrayAsync(downloadUrl);
                                File.WriteAllBytes(tempBatPath, batContent);
                                batFiles.Add(tempBatPath);
                                progress?.Report($"✅ Đã tải: {fileName}");
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"⚠️ Lỗi quét HTTP: {ex.Message}");
            }
        }

        // PHẦN 2: QUÉT NAS (NẾU HTTP THẤT BẠI)
        if (!batFiles.Any())
        {
            await Task.Run(() =>
            {
                progress?.Report("📂 HTTP không tìm thấy file, chuyển sang quét NAS...");

                // ✅ DÙNG BIẾN STATIC ĐÃ KHAI BÁO
                bool nasConnected = ConnectToNAS(NAS_PATH, NAS_USERNAME, NAS_PASSWORD, progress);

                if (nasConnected && Directory.Exists(NAS_PATH))
                {
                    try
                    {
                        progress?.Report($"🔍 Đang quét NAS: {NAS_PATH}");

                        var foundFiles = Directory.GetFiles(NAS_PATH, "*.bat", SearchOption.AllDirectories)
                            .Where(f =>
                            {
                                string name = Path.GetFileName(f).ToLower();
                                return name.Contains("unlock") || name.Contains("eps") ||
                                       name.Contains("disable") || name.Contains("unblock");
                            })
                            .ToList();

                        if (foundFiles.Any())
                        {
                            progress?.Report($"✅ Tìm thấy {foundFiles.Count} file .bat trên NAS");

                            foreach (var nasFile in foundFiles)
                            {
                                try
                                {
                                    string fileName = Path.GetFileName(nasFile);
                                    string tempPath = Path.Combine(Path.GetTempPath(), fileName);
                                    File.Copy(nasFile, tempPath, true);
                                    batFiles.Add(tempPath);
                                    progress?.Report($"✅ Đã sao chép: {fileName}");
                                }
                                catch (Exception ex)
                                {
                                    progress?.Report($"⚠️ Lỗi sao chép {Path.GetFileName(nasFile)}: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            progress?.Report("⚠️ Không tìm thấy file .bat trên NAS");
                        }
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"❌ Lỗi quét NAS: {ex.Message}");
                    }
                    finally
                    {
                        DisconnectNAS(NAS_PATH, progress); // ✅ DÙNG NAS_PATH
                    }
                }
                else
                {
                    progress?.Report("❌ Không thể kết nối hoặc truy cập NAS");
                }
            });
        }


        // PHẦN 3: QUÉT LOCAL (DỰ PHÒNG CUỐI CÙNG)
        if (!batFiles.Any())
        {
            await Task.Run(() =>
            {
                progress?.Report("📁 Quét thư mục local...");

                var localPaths = new List<string>
            {
                Application.StartupPath,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "EPS"),
            };

                foreach (var localPath in localPaths)
                {
                    if (!Directory.Exists(localPath)) continue;

                    try
                    {
                        progress?.Report($"🔍 Đang quét: {localPath}");
                        var foundFiles = Directory.GetFiles(localPath, "*.bat", SearchOption.AllDirectories)
                            .Where(f =>
                            {
                                string name = Path.GetFileName(f).ToLower();
                                return name.Contains("unlock") || name.Contains("eps") ||
                                       name.Contains("disable") || name.Contains("unblock");
                            })
                            .ToList();

                        if (foundFiles.Any())
                        {
                            progress?.Report($"✅ Tìm thấy {foundFiles.Count} file .bat local");
                            batFiles.AddRange(foundFiles);
                        }
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"⚠️ Lỗi quét {localPath}: {ex.Message}");
                    }
                }
            });
        }
        // PHẦN 4: SẮP XẾP KẾT QUẢ
        if (!batFiles.Any())
        {
            progress?.Report("❌ Không tìm thấy file .bat unlock ở bất kỳ nguồn nào");
            return batFiles;
        }

        batFiles = batFiles.OrderByDescending(f =>
        {
            string name = Path.GetFileName(f).ToLower();
            if (name.Contains("unlock") && name.Contains("eps")) return 2;
            if (name.Contains("unlock") || name.Contains("eps")) return 1;
            return 0;
        }).ToList();

        progress?.Report($"🎯 Tổng cộng tìm thấy {batFiles.Count} file .bat");
        return batFiles;
    }

    private static bool IsAllowedIPRange()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    string ipString = ip.ToString();

                    // Kiểm tra blocked trước
                    foreach (var blocked in BLOCKED_IP_PREFIXES)
                    {
                        if (ipString.StartsWith(blocked))
                        {
                            Debug.WriteLine($"[IP Check] ❌ IP bị chặn: {ipString}");
                            return false;
                        }
                    }

                    // Kiểm tra allowed
                    foreach (var allowed in ALLOWED_IP_PREFIXES)
                    {
                        if (ipString.StartsWith(allowed))
                        {
                            Debug.WriteLine($"[IP Check] ✅ IP được phép: {ipString}");
                            return true;
                        }
                    }
                }
            }

            Debug.WriteLine("[IP Check] ⚠️ IP không trong danh sách");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IP Check] ⚠️ Lỗi: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> RunBatFileAsync(string batFilePath, IProgress<string> progress)
    {
        try
        {
            progress?.Report($"▶️ Đang chạy: {Path.GetFileName(batFilePath)}");

            var processInfo = new ProcessStartInfo
            {
                FileName = batFilePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(batFilePath)
            };

            using (var process = new Process { StartInfo = processInfo })
            {
                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        progress?.Report($"  📄 {e.Data}");
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        progress?.Report($"  ⚠️ {e.Data}");
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                bool completed = await Task.Run(() => process.WaitForExit(30000));

                if (!completed)
                {
                    progress?.Report("⏱️ Timeout - Dừng process");
                    try { process.Kill(); } catch { }
                    return false;
                }

                bool success = process.ExitCode == 0;
                progress?.Report(success
                    ? "✅ Unlock thành công!"
                    : $"⚠️ Exit code: {process.ExitCode}");

                return success;
            }
        }
        catch (Exception ex)
        {
            progress?.Report($"❌ Lỗi chạy .bat: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region KẾT NỐI NAS
    // ⭐ CẤU HÌNH NAS (MÃ HÓA BASE64)
    private static readonly string NAS_PATH = @"\\107.126.41.111\nongvanphan";
    private static readonly string NAS_USERNAME = Encoding.UTF8.GetString(Convert.FromBase64String("YWRtaW4=")); // "admin"
    private static readonly string NAS_PASSWORD = Encoding.UTF8.GetString(Convert.FromBase64String("aW5zcDIwMTlA")); // "insp2019@"

    [DllImport("mpr.dll")]
    private static extern int WNetAddConnection2(ref NETRESOURCE netResource, string password, string username, int flags);

    [DllImport("mpr.dll")]
    private static extern int WNetCancelConnection2(string name, int flags, bool force);

    [StructLayout(LayoutKind.Sequential)]
    private struct NETRESOURCE
    {
        public int dwScope;
        public int dwType;
        public int dwDisplayType;
        public int dwUsage;
        public string lpLocalName;
        public string lpRemoteName;
        public string lpComment;
        public string lpProvider;
    }

    private static bool ValidateNASPath(string nasPath, IProgress<string> progress)
    {
        if (string.IsNullOrWhiteSpace(nasPath))
        {
            progress?.Report("❌ Đường dẫn NAS rỗng");
            return false;
        }

        if (!nasPath.StartsWith(@"\\"))
        {
            progress?.Report("❌ Đường dẫn NAS phải bắt đầu bằng \\\\");
            return false;
        }

        if (nasPath.Contains(" "))
        {
            progress?.Report("⚠️ Đường dẫn chứa dấu cách, có thể gây lỗi trên một số hệ thống");
        }

        return true;
    }

    private static string GetNetworkErrorMessage(int errorCode)
    {
        switch (errorCode)
        {
            case 0: return "Thành công";
            case 5: return "Truy cập bị từ chối - Kiểm tra username/password";
            case 53: return "Không tìm thấy đường dẫn mạng";
            case 67: return "Tên mạng không hợp lệ";
            case 85: return "Ổ đĩa mạng đang được sử dụng";
            case 86: return "Mật khẩu không đúng";
            case 1203: return "Không tìm thấy máy chủ";
            case 1219: return "Đã có kết nối với thông tin đăng nhập khác";
            case 1326: return "Username hoặc password không đúng";
            case 2250: return "Ổ đĩa mạng không được map";
            default: return $"Lỗi không xác định (Code: {errorCode})";
        }
    }

    private static bool ConnectToNAS(string nasPath, string username, string password, IProgress<string> progress)
    {
        try
        {
            if (!ValidateNASPath(nasPath, progress))
                return false;

            string cleanPath = nasPath.Trim();

            try
            {
                WNetCancelConnection2(cleanPath, 0, true);
            }
            catch { }

            progress?.Report($"🔗 Đang kết nối NAS: {cleanPath}");

            var netResource = new NETRESOURCE
            {
                dwType = 1,
                lpRemoteName = cleanPath
            };

            int result = WNetAddConnection2(ref netResource, password, username, 0);

            if (result == 0)
            {
                progress?.Report($"✅ Kết nối NAS thành công!");
                return true;
            }
            else
            {
                string errorMessage = GetNetworkErrorMessage(result);
                progress?.Report($"⚠️ Lỗi kết nối NAS (Code: {result}) - {errorMessage}");
                return false;
            }
        }
        catch (Exception ex)
        {
            progress?.Report($"❌ Lỗi kết nối NAS: {ex.Message}");
            return false;
        }
    }

    private static void DisconnectNAS(string nasPath, IProgress<string> progress)
    {
        try
        {
            WNetCancelConnection2(nasPath.Trim(), 0, true);
            progress?.Report($"🔌 Đã ngắt kết nối NAS");
        }
        catch (Exception ex)
        {
            progress?.Report($"⚠️ Lỗi ngắt kết nối NAS: {ex.Message}");
        }
    }
    #endregion

    #region GIAO DIỆN THÔNG BÁO
    private static void ShowUpdatePrompt(string latestVersion, string changelog, string serverUrl, string exeName)
    {
        int cornerRadius = 20;
        var updateForm = new Form
        {
            Text = "Cập nhật phần mềm",
            Size = new Size(450, 300),
            StartPosition = FormStartPosition.Manual,
            FormBorderStyle = FormBorderStyle.None,
            TopMost = true,
            BackColor = System.Drawing.Color.White,
            Icon = Application.OpenForms.Count > 0 ? Application.OpenForms[0].Icon : SystemIcons.Application
        };

        updateForm.Location = new Point(
            Screen.PrimaryScreen.WorkingArea.Right - updateForm.Width - 20,
            Screen.PrimaryScreen.WorkingArea.Bottom - updateForm.Height - 20
        );

        IntPtr hRgn = CreateRoundRectRgn(0, 0, updateForm.Width, updateForm.Height, cornerRadius, cornerRadius);
        updateForm.Region = Region.FromHrgn(hRgn);

        int val = 2;
        DwmSetWindowAttribute(updateForm.Handle, 2, ref val, 4);
        MARGINS margins = new MARGINS() { cxLeftWidth = 1, cxRightWidth = 1, cyTopHeight = 1, cyBottomHeight = 1 };
        DwmExtendFrameIntoClientArea(updateForm.Handle, ref margins);

        updateForm.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(System.Drawing.Color.FromArgb(128, System.Drawing.Color.LightGray)))
            {
                pen.Width = 1f;
                e.Graphics.DrawRectangle(pen, 0.5f, 0.5f, updateForm.Width - 1, updateForm.Height - 1);
            }
        };

        // Icon
        var assembly = Assembly.GetExecutingAssembly();
        string resourceName = "AutoDrop.src.update_icon.png";
        Image iconImage = SystemIcons.Shield.ToBitmap();

        try
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null && stream.Length > 0)
                {
                    using (var tempImage = new Bitmap(stream))
                    {
                        iconImage = new Bitmap(tempImage);
                    }
                }
            }
        }
        catch { }

        var picIcon = new PictureBox
        {
            Size = new Size(40, 40),
            Location = new Point(20, 20),
            Image = iconImage,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = System.Drawing.Color.Transparent
        };

        string appName = exeName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? exeName.Substring(0, exeName.Length - 4)
            : exeName;

        string currentVersion = Application.ProductVersion;
        var lblVersion = new Label
        {
            Text = $"{appName} đã có phiên bản mới: {latestVersion}\nPhiên bản hiện tại: {currentVersion}",
            Location = new Point(70, 15),
            Width = updateForm.Width - 90,
            Height = 40,
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        var rtbChangelog = new RichTextBox
        {
            Text = changelog,
            Location = new Point(50, 60),
            Width = updateForm.Width - 65,
            Height = 170,
            BorderStyle = BorderStyle.None,
            BackColor = System.Drawing.Color.White,
            Font = new Font("Segoe UI", 9),
            ScrollBars = RichTextBoxScrollBars.Vertical,
            ReadOnly = true,
            WordWrap = true,
        };

        var txtLog = new TextBox
        {
            Location = new Point(20, 80),
            Width = updateForm.Width - 40,
            Height = 150,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Consolas", 8),
            BackColor = System.Drawing.Color.FromArgb(240, 240, 240),
            Visible = false
        };

        var panelButtons = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
            BackColor = System.Drawing.Color.White,
            Padding = new Padding(0, 0, 0, 10)
        };

        var btnUpdate = new Button
        {
            Text = "Cập nhật ngay",
            Width = 120,
            Height = 35,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 10F),
            BackColor = System.Drawing.Color.FromArgb(60, 179, 113),
            ForeColor = System.Drawing.Color.White,
            Cursor = Cursors.Hand
        };
        btnUpdate.FlatAppearance.BorderSize = 0;
        btnUpdate.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(0, 139, 139);
        btnUpdate.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnUpdate.Width, btnUpdate.Height, 15, 15));

        var btnSkip = new Button
        {
            Text = "Để sau",
            Width = 120,
            Height = 35,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 10F),
            BackColor = System.Drawing.Color.FromArgb(200, 200, 200),
            ForeColor = System.Drawing.Color.Black,
            Cursor = Cursors.Hand
        };
        var lblWarning = new Label
        {
            Text = "Nếu báo lỗi không tự động cập nhật\r\nHãy Unlock EPS trước khi bấm cập nhật ứng dụng",
            ForeColor = System.Drawing.Color.Red,
            Font = new Font("Segoe UI", 8, FontStyle.Italic),
            Width = panelButtons.Width,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(panelButtons.Width - 70, 40) // đặt dưới 2 nút (10 + 35 = 45)
        };
        btnSkip.FlatAppearance.BorderSize = 0;
        btnSkip.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(120, 120, 120);
        btnSkip.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnSkip.Width, btnSkip.Height, 15, 15));

        btnUpdate.Location = new Point(70, 5);
        btnSkip.Location = new Point(panelButtons.Width - btnSkip.Width - 70, 5);
        btnSkip.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        btnSkip.Click += (s, e) => updateForm.Close();

        btnUpdate.Click += async (s, e) =>
        {
            btnUpdate.Enabled = false;
            btnSkip.Enabled = false;
            txtLog.Visible = true;

            var progress = new Progress<string>(msg =>
            {
                if (updateForm.InvokeRequired)
                {
                    updateForm.Invoke(new Action(() =>
                    {
                        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
                        txtLog.SelectionStart = txtLog.Text.Length;
                        txtLog.ScrollToCaret();
                    }));
                }
                else
                {
                    txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                }
            });

            await DownloadAndUpdateAsync(serverUrl, exeName, latestVersion, btnUpdate, updateForm, progress);
        };

        panelButtons.Controls.Add(btnUpdate);
        panelButtons.Controls.Add(btnSkip);
        updateForm.Controls.Add(picIcon);
        updateForm.Controls.Add(lblVersion);
        updateForm.Controls.Add(txtLog);
        updateForm.Controls.Add(rtbChangelog);
        panelButtons.Controls.Add(lblWarning);
        updateForm.Controls.Add(panelButtons);
        updateForm.Show();
    }
    #endregion

    #region TẢI VÀ CẬP NHẬT
    private static async Task DownloadAndUpdateAsync(
    string serverUrl, string exeName, string latestVersion,
    Button btnUpdate, Form updateForm, IProgress<string> progress)
    {
        string currentExe = Application.ExecutablePath;
        string exeDir = Path.GetDirectoryName(currentExe);
        string baseName = Path.GetFileNameWithoutExtension(exeName);

        // Dùng thư mục con "update_temp" trong app để tránh bị quét temp
        string updateTempDir = Path.Combine(exeDir, "update_temp");
        Directory.CreateDirectory(updateTempDir);
        string tempNewExe = Path.Combine(updateTempDir, exeName);

        // Backup phiên bản cũ (giữ nguyên logic đẹp của bạn)
        string currentVersion = Application.ProductVersion.Replace(".", "_");
        string backupFileName = $"{baseName}_v{currentVersion}.exe";
        string backupDir = Path.Combine(exeDir, "backup");
        Directory.CreateDirectory(backupDir);
        string backupExePath = Path.Combine(backupDir, backupFileName);

        try
        {
            // UNLOCK EPS GIỮ NGUYÊN 
            if (ENABLE_EPS_UNLOCK && IsAllowedIPRange())
            {
                progress?.Report("Đang unlock EPS...");
                var bats = await FindAllUnlockBatAsync(progress, serverUrl);
                foreach (var bat in bats)
                {
                    if (await RunBatFileAsync(bat, progress))
                    {
                        progress?.Report("Unlock EPS thành công!");
                        await Task.Delay(2500);
                        break;
                    }
                }
            }

            // TẢI FILE MỚI
            progress?.Report("Đang tải bản cập nhật...");
            if (!await DownloadUpdateViaHTTP(serverUrl, exeName, tempNewExe, btnUpdate, progress))
                throw new Exception("Tải file thất bại");

            // BACKUP PHIÊN BẢN CŨ
            if (File.Exists(currentExe))
            {
                File.Copy(currentExe, backupExePath, true);
                progress?.Report($"Đã backup: {backupFileName}");
            }

            // Dọn backup cũ (giữ 3 phiên bản gần đây nhất)
            try
            {
                var oldBackups = Directory.GetFiles(backupDir, $"{baseName}_v*.exe")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Skip(3);
                foreach (var f in oldBackups)
                {
                    File.Delete(f.FullName);
                    progress?.Report($"Xóa backup cũ: {f.Name}");
                }
            }
            catch { }

            //TẠO FILE UPDATE.CMD ĐẸP + KHÔNG BAO GIỜ LỖI FONT TIẾNG VIỆT
            string batPath = Path.Combine(updateTempDir, "update.cmd");

            // Lấy tên app để hiển thị đẹp
            string appName = Path.GetFileNameWithoutExtension(exeName);

            string batContent = $@"@echo off
            chcp 65001 >nul
            mode con: cols=90 lines=28
            color 0b
            cls

            echo.
            echo  ╔═══════════════════════════════════════════════════════════════════════════════════╗
            echo  ║                                                                                   ║
            echo  ║                                   AUTO UPDATE                                     ║
            echo  ║                                                                                   ║
            echo  ╚═══════════════════════════════════════════════════════════════════════════════════╝
            echo.
            echo                        Đang cập nhật {appName}
            echo                 Phiên bản mới → {latestVersion}
            echo                 Phiên bản hiện tại → {currentVersion}
            echo.
            echo  Đang thực hiện cập nhật tự động...
            echo.
            echo  ┌───────────────────────────────────────────────────────────────────────────────┐
            echo  │                                                                               │

            echo  │  Đang chờ ứng dụng cũ đóng hoàn toàn...
            :wait
            timeout /t 3 /nobreak >nul
            tasklist /fi ""imagename eq {Path.GetFileName(currentExe)}"" 2>nul | find /i ""{Path.GetFileName(currentExe)}"" >nul
            if not errorlevel 1 goto wait

            echo  │  Ứng dụng đã đóng.
            echo  │
            echo  │  Đang thay thế file mới...
            if exist ""{tempNewExe}"" (
                copy /y ""{tempNewExe}"" ""{currentExe}"" >nul
                if not errorlevel 1 (
                    echo  │  Cập nhật thành công!
                    echo  │
                    echo  │  Đang khởi động lại ứng dụng...
                    start """" ""{currentExe}""
                    timeout /t 3 >nul
                    echo  │  Đang dọn dẹp file tạm...
                    rd /s /q ""{updateTempDir}"" 2>nul
                    echo  │
                    echo  │  Hoàn tất! Cảm ơn bạn đã sử dụng {appName}!
                    echo  │
                    echo  └───────────────────────────────────────────────────────────────────────────────┘
                    timeout /t 5 >nul
                    exit
                )
            )

            echo  │  LỖI: Không thể cập nhật! Đang khôi phục bản cũ...
            if exist ""{backupExePath}"" copy /y ""{backupExePath}"" ""{currentExe}"" >nul
            echo  │  Đã khôi phục thành công.
            echo  │
            echo  └───────────────────────────────────────────────────────────────────────────────┘
            timeout /t 8 >nul
            exit
            ";

            File.WriteAllText(batPath, batContent, new UTF8Encoding(false));

            //CHẠY CMD HIỂN THỊ ĐẸP
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{batPath}\"\"",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            };

            try
            {
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                progress?.Report($"Không thể khởi động updater: {ex.Message}");
                MessageBox.Show("Cần chạy với quyền Administrator để cập nhật ứng dụng!",
                                "Yêu cầu quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnUpdate.Enabled = true;
                return;
            }

            await Task.Delay(1200);
            updateForm.Close();
            Application.Exit();
        }
        catch (Exception ex)
        {
            progress?.Report($"LỖI: {ex.Message}");
            MessageBox.Show($"Cập nhật thất bại:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnUpdate.Text = "Thử lại";
            btnUpdate.Enabled = true;
        }
    }

    private static async Task<bool> DownloadUpdateViaHTTP(string serverUrl, string exeName, string tempFile, Button btnUpdate, IProgress<string> progress)
    {
        try
        {
            using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
            using (var response = await client.GetAsync(serverUrl + "/" + exeName, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength ?? -1L;

                using (var input = await response.Content.ReadAsStreamAsync())
                using (var output = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int read;
                    int lastPercent = 0;

                    do
                    {
                        read = await input.ReadAsync(buffer, 0, buffer.Length);
                        if (read > 0)
                        {
                            await output.WriteAsync(buffer, 0, read);
                            totalRead += read;

                            if (total != -1)
                            {
                                int percent = (int)(totalRead * 100 / total);

                                if (btnUpdate.InvokeRequired)
                                    btnUpdate.Invoke(new Action(() => btnUpdate.Text = $"Đang tải... {percent}%"));
                                else
                                    btnUpdate.Text = $"Đang tải... {percent}%";

                                if (percent != lastPercent && percent % 10 == 0)
                                {
                                    progress?.Report($"📊 Tiến độ: {percent}%");
                                    lastPercent = percent;
                                }
                            }
                        }
                    } while (read > 0);
                }
            }

            progress?.Report("✅ Tải xuống hoàn tất!");
            return true;
        }
        catch (Exception ex)
        {
            progress?.Report($"❌ Lỗi tải: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region WIN32 API
    [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
    private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
        int nWidthEllipse, int nHeightEllipse);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }
    #endregion
}
#endregion