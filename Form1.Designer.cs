#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
// 明確引用 WinForms專用的 WebView2命名空間
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace AuspiciousBrowser
{
    public partial class Form1 : Form
    {
        private Button btnBack = null!;
        private Button btnForward = null!;
        private Button btnRefresh = null!;
        private TextBox textBoxUrl = null!;
        private Button btnGo = null!;
        private Button btnHistory = null!;
        private WebView2 webView21 = null!;

        // 定義歷史記錄的數據結構
        public class HistoryItem
        {
            public string Time { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;

            public override string ToString()
            {
                return $"{Time} - {Url}";
            }
        }

        // 用於全局保存歷史記錄的列表
        private static List<HistoryItem> historyList = new List<HistoryItem>();

        public Form1()
        {
            InitializeCustomControls();
        }

        private async void InitializeCustomControls()
        {
            // 1. 設置窗體基本屬性
            this.Text = "AuspiciousExplorer - 檸檬瀏覽器";
            this.Width = 1200;
            this.Height = 800;

            // 2. 創建“後退”按鈕
            btnBack = new Button();
            btnBack.Text = "後退";
            btnBack.Location = new Point(12, 12);
            btnBack.Width = 55;
            btnBack.Enabled = false;
            btnBack.Click += (s, e) => { if (webView21.CoreWebView2 != null && webView21.CoreWebView2.CanGoBack) webView21.CoreWebView2.GoBack(); };
            this.Controls.Add(btnBack);

            // 3. 創建“前進”按鈕
            btnForward = new Button();
            btnForward.Text = "前進";
            btnForward.Location = new Point(73, 12);
            btnForward.Width = 55;
            btnForward.Enabled = false;
            btnForward.Click += (s, e) => { if (webView21.CoreWebView2 != null && webView21.CoreWebView2.CanGoForward) webView21.CoreWebView2.GoForward(); };
            this.Controls.Add(btnForward);

            // 4. 創建“刷新”按鈕
            btnRefresh = new Button();
            btnRefresh.Text = "刷新";
            btnRefresh.Location = new Point(134, 12);
            btnRefresh.Width = 55;
            btnRefresh.Click += (s, e) => { webView21.CoreWebView2?.Reload(); };
            this.Controls.Add(btnRefresh);

            // 5. 創建“歷史”按鈕
            btnHistory = new Button();
            btnHistory.Text = "歷史";
            btnHistory.Location = new Point(195, 12);
            btnHistory.Width = 55;
            btnHistory.Click += BtnHistory_Click;
            this.Controls.Add(btnHistory);

            // 6. 創建頂部地址欄文本框
            textBoxUrl = new TextBox();
            textBoxUrl.Location = new Point(260, 14);
            textBoxUrl.Width = 735;
            textBoxUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxUrl.KeyDown += TextBoxUrl_KeyDown;
            this.Controls.Add(textBoxUrl);

            // 7. 創建“訪問”按鈕
            btnGo = new Button();
            btnGo.Text = "訪問";
            btnGo.Location = new Point(1005, 12);
            btnGo.Width = 160;
            btnGo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnGo.Click += BtnGo_Click;
            this.Controls.Add(btnGo);

            // 8. 創建 WebView2 內核控件
            webView21 = new WebView2();
            webView21.Location = new Point(12, 50);
            webView21.Size = new Size(1160, 700);
            webView21.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(webView21);

            // 9. 初始化 WebView2 運行環境（加入 GPU 硬體加速優化參數）
            try
            {
                var options = new CoreWebView2EnvironmentOptions("--enable-hardware-accelerated-video-decode --ignore-gpu-blocklist --enable-gpu-rasterization");
                var env = await CoreWebView2Environment.CreateAsync(null, null, options);
                await webView21.EnsureCoreWebView2Async(env);

                if (webView21.CoreWebView2 != null)
                {
                    // 監聽導航完成
                    webView21.CoreWebView2.NavigationCompleted += (s, e) =>
                    {
                        string? currentUrl = webView21.CoreWebView2?.Source;
                        if (!string.IsNullOrEmpty(currentUrl) && !currentUrl.StartsWith("about:"))
                        {
                            textBoxUrl.Text = currentUrl;
                        }
                        UpdateNavigationButtons();

                        // 自動記錄歷史
                        if (!string.IsNullOrEmpty(currentUrl) && !currentUrl.StartsWith("about:"))
                        {
                            string currentTime = DateTime.Now.ToString("HH:mm:ss");
                            if (historyList.Count == 0 || historyList[historyList.Count - 1].Url != currentUrl)
                            {
                                historyList.Add(new HistoryItem { Time = currentTime, Url = currentUrl });
                            }
                        }
                    };

                    // 監聽歷史狀態變化
                    webView21.CoreWebView2.HistoryChanged += (s, e) =>
                    {
                        UpdateNavigationButtons();
                    };

                    // 攔截網頁中的新窗口打開請求（例如 target="_blank"）
                    webView21.CoreWebView2.NewWindowRequested += (s, e) =>
                    {
                        e.Handled = true;
                        string targetUri = e.Uri;

                        this.Invoke(new Action(() => {
                            CreateBrowserSubForm(targetUri);
                        }));
                    };

                    // 下載管理器實現
                    webView21.CoreWebView2.DownloadStarting += (s, e) =>
                    {
                        using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                        {
                            saveFileDialog.FileName = Path.GetFileName(e.ResultFilePath);
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                e.ResultFilePath = saveFileDialog.FileName;

                                e.DownloadOperation.BytesReceivedChanged += (sender, args) =>
                                {
                                    long totalBytes = (long)(e.DownloadOperation.TotalBytesToReceive ?? 0);
                                    long receivedBytes = (long)e.DownloadOperation.BytesReceived;
                                    if (totalBytes > 0)
                                    {
                                        int progress = (int)((double)receivedBytes / totalBytes * 100);
                                        this.Invoke(new Action(() => {
                                            this.Text = $"AuspiciousExplorer - 正在下載... {progress}%";
                                        }));
                                    }
                                };

                                e.DownloadOperation.StateChanged += (sender, args) =>
                                {
                                    if (e.DownloadOperation.State == CoreWebView2DownloadState.Completed)
                                    {
                                        this.Invoke(new Action(() => {
                                            this.Text = "AuspiciousExplorer - 檸檬瀏覽器";
                                            MessageBox.Show("文件下載成功！", "下載完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }));
                                    }
                                };
                            }
                            else
                            {
                                e.Cancel = true;
                            }
                        }
                    };

                    // 加載內置首頁並設置內部協議網址
                    webView21.CoreWebView2.NavigateToString(GetInternalHomePageHtml());
                    textBoxUrl.Text = "about:lemon-home";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 初始化失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 獨立封裝：創建內部首頁 HTML 字符串
        private string GetInternalHomePageHtml()
        {
            return @"
            <!DOCTYPE html>
            <html lang='zh-CN'>
            <head>
                <meta charset='UTF-8'>
                <title>AuspiciousExplorer - 檸檬瀏覽器內部主頁</title>
                <style>
                    body { background-color: #1e1e1e; color: #d4d4d4; font-family: 'Segoe UI', Tahoma, sans-serif; text-align: center; padding-top: 100px; margin: 0; }
                    h1 { color: #4ec9b0; font-size: 36px; margin-bottom: 10px; }
                    .card { background: #252526; border: 1px solid #3c3c3c; display: inline-block; padding: 50px; border-radius: 10px; box-shadow: 0 8px 24px rgba(0,0,0,0.6); width: 650px; }
                    .tag { background: #0e639c; padding: 6px 12px; border-radius: 4px; font-size: 14px; color: #ffffff; font-weight: bold; }
                    hr { border: 0; border-top: 1px solid #3c3c3c; margin: 25px 0; }
                    .links { margin-top: 30px; }
                    a { color: #569cd6; text-decoration: none; margin: 0 15px; font-size: 16px; font-weight: 500; }
                    a:hover { text-decoration: underline; color: #4ec9b0; }
                    .footer { margin-top: 40px; font-size: 12px; color: #858585; }
                </style>
            </head>
            <body>
                <div class='card'>
                    <h1>🐾 AuspiciousExplorer</h1>
                    <p><span class='tag'>Lemon Studio 內部專屬版</span></p>
                    <hr>
                    <p style='text-align: left; line-height: 1.8; color: #9cdcfe;'>
                        &nbsp;&nbsp;&nbsp;&nbsp;這是一款專為極客與獨立創作打造的高性能瀏覽器。底層基於 Chromium 內核與 WebView2 構建，完美解鎖 GPU 硬體加速，帶來絲滑流暢的網頁與直播渲染體驗。
                    </p>
                    <div class='links'>
                        <a href='https://www.bilibili.com'>📺 Bilibili 影音</a>
                        <a href='https://cn.bing.com'>🔍 Bing 搜索</a>
                    </div>
                    <div class='footer'>
                        AuspiciousExplorer Internal Home &bull; Powered by Lemon Studio
                    </div>
                </div>
            </body>
            </html>";
        }

        // 獨立封裝：創建一個標準的帶導航工具欄與歷史功能的子瀏覽器分頁視窗
        private async void CreateBrowserSubForm(string initialUrl)
        {
            Form subForm = new Form();
            subForm.Text = "AuspiciousExplorer - 子分頁視窗";
            subForm.Width = 950;
            subForm.Height = 650;
            subForm.StartPosition = FormStartPosition.CenterParent;

            Button subBtnBack = new Button { Text = "後退", Location = new Point(12, 12), Width = 50, Enabled = false };
            Button subBtnForward = new Button { Text = "前進", Location = new Point(68, 12), Width = 50, Enabled = false };
            Button subBtnRefresh = new Button { Text = "刷新", Location = new Point(124, 12), Width = 50 };

            TextBox subTextBoxUrl = new TextBox { Location = new Point(185, 14), Width = 635, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            Button subBtnGo = new Button { Text = "訪問", Location = new Point(830, 12), Width = 95, Anchor = AnchorStyles.Top | AnchorStyles.Right };

            subForm.Controls.Add(subBtnBack);
            subForm.Controls.Add(subBtnForward);
            subForm.Controls.Add(subBtnRefresh);
            subForm.Controls.Add(subTextBoxUrl);
            subForm.Controls.Add(subBtnGo);

            WebView2 subWebView = new WebView2();
            subWebView.Location = new Point(12, 50);
            subWebView.Size = new Size(913, 545);
            subWebView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            subForm.Controls.Add(subWebView);

            try
            {
                var options = new CoreWebView2EnvironmentOptions("--enable-hardware-accelerated-video-decode --ignore-gpu-blocklist --enable-gpu-rasterization");
                var env = await CoreWebView2Environment.CreateAsync(null, null, options);
                await subWebView.EnsureCoreWebView2Async(env);

                if (subWebView.CoreWebView2 != null)
                {
                    subWebView.CoreWebView2.NavigationCompleted += (sender, ev) =>
                    {
                        var core = subWebView.CoreWebView2;
                        if (core != null)
                        {
                            string? currentUrl = core.Source;
                            if (!string.IsNullOrEmpty(currentUrl))
                            {
                                subTextBoxUrl.Text = currentUrl;
                                subBtnBack.Enabled = core.CanGoBack;
                                subBtnForward.Enabled = core.CanGoForward;

                                string currentTime = DateTime.Now.ToString("HH:mm:ss");
                                if (historyList.Count == 0 || historyList[historyList.Count - 1].Url != currentUrl)
                                {
                                    historyList.Add(new HistoryItem { Time = currentTime, Url = currentUrl });
                                }
                            }
                        }
                    };

                    subWebView.CoreWebView2.HistoryChanged += (sender, ev) =>
                    {
                        var core = subWebView.CoreWebView2;
                        if (core != null)
                        {
                            subBtnBack.Enabled = core.CanGoBack;
                            subBtnForward.Enabled = core.CanGoForward;
                        }
                    };

                    if (!string.IsNullOrEmpty(initialUrl))
                    {
                        subWebView.CoreWebView2.Navigate(initialUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(subForm, $"子內核初始化失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            subBtnBack.Click += (s, args) => { if (subWebView.CoreWebView2 != null && subWebView.CoreWebView2.CanGoBack) subWebView.CoreWebView2.GoBack(); };
            subBtnForward.Click += (s, args) => { if (subWebView.CoreWebView2 != null && subWebView.CoreWebView2.CanGoForward) subWebView.CoreWebView2.GoForward(); };
            subBtnRefresh.Click += (s, args) => { subWebView.CoreWebView2?.Reload(); };

            subBtnGo.Click += (s, args) =>
            {
                if (subWebView.CoreWebView2 != null && !string.IsNullOrWhiteSpace(subTextBoxUrl.Text))
                {
                    string targetUrl = subTextBoxUrl.Text.Trim();
                    if (!targetUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !targetUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                        !targetUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                    {
                        targetUrl = "https://" + targetUrl;
                    }
                    subWebView.CoreWebView2.Navigate(targetUrl);
                }
            };

            subTextBoxUrl.KeyDown += (s, args) =>
            {
                if (args.KeyCode == Keys.Enter)
                {
                    subBtnGo.PerformClick();
                    args.SuppressKeyPress = true;
                }
            };

            subForm.Show(this);
        }

        // 點擊歷史按鈕：彈出帶有歷史列表與右側子瀏覽器的管理中心
        private void BtnHistory_Click(object? sender, EventArgs e)
        {
            if (historyList.Count == 0)
            {
                MessageBox.Show("目前暫無瀏覽歷史記錄。", "歷史記錄", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (Form historyForm = new Form())
            {
                historyForm.Text = "AuspiciousExplorer - 歷史與分頁導航中心";
                historyForm.Width = 900;
                historyForm.Height = 650;
                historyForm.StartPosition = FormStartPosition.CenterParent;

                ListBox listBoxHistory = new ListBox();
                listBoxHistory.Location = new Point(12, 12);
                listBoxHistory.Size = new Size(280, 580);
                listBoxHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

                foreach (var item in historyList)
                {
                    listBoxHistory.Items.Add(item);
                }
                historyForm.Controls.Add(listBoxHistory);

                Button subBtnBack = new Button { Text = "後退", Location = new Point(305, 12), Width = 50, Enabled = false };
                Button subBtnForward = new Button { Text = "前進", Location = new Point(360, 12), Width = 50, Enabled = false };
                Button subBtnRefresh = new Button { Text = "刷新", Location = new Point(415, 12), Width = 50 };

                TextBox subTextBoxUrl = new TextBox { Location = new Point(475, 14), Width = 280, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
                Button subBtnGo = new Button { Text = "訪問", Location = new Point(765, 12), Width = 105, Anchor = AnchorStyles.Top | AnchorStyles.Right };

                historyForm.Controls.Add(subBtnBack);
                historyForm.Controls.Add(subBtnForward);
                historyForm.Controls.Add(subBtnRefresh);
                historyForm.Controls.Add(subTextBoxUrl);
                historyForm.Controls.Add(subBtnGo);

                WebView2 subWebView = new WebView2();
                subWebView.Location = new Point(305, 50);
                subWebView.Size = new Size(565, 542);
                subWebView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                historyForm.Controls.Add(subWebView);

                historyForm.Shown += async (s, args) =>
                {
                    try
                    {
                        var options = new CoreWebView2EnvironmentOptions("--enable-hardware-accelerated-video-decode --ignore-gpu-blocklist --enable-gpu-rasterization");
                        var env = await CoreWebView2Environment.CreateAsync(null, null, options);
                        await subWebView.EnsureCoreWebView2Async(env);

                        if (subWebView.CoreWebView2 != null)
                        {
                            subWebView.CoreWebView2.NavigationCompleted += (sender, ev) =>
                            {
                                var core = subWebView.CoreWebView2;
                                if (core != null)
                                {
                                    string? currentUrl = core.Source;
                                    if (!string.IsNullOrEmpty(currentUrl))
                                    {
                                        subTextBoxUrl.Text = currentUrl;
                                        subBtnBack.Enabled = core.CanGoBack;
                                        subBtnForward.Enabled = core.CanGoForward;
                                    }
                                }
                            };

                            subWebView.CoreWebView2.HistoryChanged += (sender, ev) =>
                            {
                                var core = subWebView.CoreWebView2;
                                if (core != null)
                                {
                                    subBtnBack.Enabled = core.CanGoBack;
                                    subBtnForward.Enabled = core.CanGoForward;
                                }
                            };

                            if (historyList.Count > 0)
                            {
                                listBoxHistory.SelectedIndex = 0;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(historyForm, $"子內核初始化失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                subBtnBack.Click += (s, args) => { if (subWebView.CoreWebView2 != null && subWebView.CoreWebView2.CanGoBack) subWebView.CoreWebView2.GoBack(); };
                subBtnForward.Click += (s, args) => { if (subWebView.CoreWebView2 != null && subWebView.CoreWebView2.CanGoForward) subWebView.CoreWebView2.GoForward(); };
                subBtnRefresh.Click += (s, args) => { subWebView.CoreWebView2?.Reload(); };

                subBtnGo.Click += (s, args) =>
                {
                    if (subWebView.CoreWebView2 != null && !string.IsNullOrWhiteSpace(subTextBoxUrl.Text))
                    {
                        string targetUrl = subTextBoxUrl.Text.Trim();
                        if (!targetUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                            !targetUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                            !targetUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                        {
                            targetUrl = "https://" + targetUrl;
                        }
                        subWebView.CoreWebView2.Navigate(targetUrl);
                    }
                };

                subTextBoxUrl.KeyDown += (s, args) =>
                {
                    if (args.KeyCode == Keys.Enter)
                    {
                        subBtnGo.PerformClick();
                        args.SuppressKeyPress = true;
                    }
                };

                Action loadSelectedHistory = () =>
                {
                    if (listBoxHistory.SelectedItem is HistoryItem selectedItem && subWebView.CoreWebView2 != null)
                    {
                        subTextBoxUrl.Text = selectedItem.Url;
                        subWebView.CoreWebView2.Navigate(selectedItem.Url);

                        if (webView21.CoreWebView2 != null)
                        {
                            webView21.CoreWebView2.Navigate(selectedItem.Url);
                            textBoxUrl.Text = selectedItem.Url;
                        }
                    }
                };

                listBoxHistory.SelectedIndexChanged += (s, args) => loadSelectedHistory();
                listBoxHistory.DoubleClick += (s, args) => loadSelectedHistory();

                historyForm.ShowDialog(this);
            }
        }

        private void UpdateNavigationButtons()
        {
            if (webView21.CoreWebView2 != null)
            {
                btnBack.Enabled = webView21.CoreWebView2.CanGoBack;
                btnForward.Enabled = webView21.CoreWebView2.CanGoForward;
            }
        }

        private void TextBoxUrl_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BtnGo_Click(sender, e);
                e.SuppressKeyPress = true;
            }
        }

        private async void BtnGo_Click(object? sender, EventArgs e)
        {
            if (webView21 != null && webView21.CoreWebView2 != null && !string.IsNullOrWhiteSpace(textBoxUrl.Text))
            {
                try
                {
                    string targetUrl = textBoxUrl.Text.Trim();

                    // 如果用戶在地址欄輸入的是內部協議，重新載入內置主頁
                    if (targetUrl.Equals("about:lemon-home", StringComparison.OrdinalIgnoreCase))
                    {
                        webView21.CoreWebView2.NavigateToString(GetInternalHomePageHtml());
                        textBoxUrl.Text = "about:lemon-home";
                        return;
                    }

                    if (!targetUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !targetUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                        !targetUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                    {
                        targetUrl = "https://" + targetUrl;
                    }

                    webView21.CoreWebView2.Navigate(targetUrl);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"跳轉失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}