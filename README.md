# LemonBrowser
# 🐾 AuspiciousExplorer (檸檬瀏覽器)

<p align="center">
  <b>Lemon Studio 專屬極客桌面瀏覽器，基於 Chromium 內核與 WebView2 構建。</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%2F11-blue" alt="Platform">
  <img src="https://img.shields.io/badge/Framework-.NET%208%20(WPF%2FWinForms)-green" alt="Framework">
  <img src="https://img.shields.io/badge/Status-Beta-orange" alt="Status">
</p>

---

## 💡 專案簡介

**AuspiciousExplorer** 是一款專為極客與獨立創作打造的高性能桌面瀏覽器。底層基於 Chromium 內核與 WebView2 構建，完美解鎖 GPU 硬體加速，帶來絲滑流暢的網頁瀏覽與高畫質直播觀賞體驗。

「類似 VIA，關閉即終止所有進程，輕量而純粹。」

---

## ✨ 核心特色

* **🐾 內置專屬內部主頁**：自帶 Lemon Studio 風格的極客首頁，隨時在地址欄輸入 `about:lemon-home` 即可返回。
* **🚀 GPU 硬體加速**：強制開啟 Chromium 硬體加速與視頻解碼，觀看 Bilibili 影片或直播絲滑流暢（在 Windows 11 下體驗更佳）。
* **🛡️ 智慧歷史導航中心**：點擊「歷史」按鈕即可喚醒管理中心，支援單擊或雙擊歷史項目直接跳轉回對應網頁。
* **🪟 獨立子分頁管理**：自動攔截網頁中的新窗口打開請求（如 `target="_blank"`），並在配備完整導航工具欄（後退、前進、刷新、地址欄）的子分頁中獨立運行。
* **📥 即時下載監控**：支援完整的檔案下載攔截與進度實時回顯，下載進度直接顯示在窗口標題欄上。

---

## 📖 使用指南

1. **訪問主頁**：啟動瀏覽器即進入內置主頁，或在地址欄輸入 `about:lemon-home`。
2. **網址導航**：在頂部地址欄輸入網址並按下 `Enter` 鍵，支援智能協議自動補全（如輸入 `bilibili.com` 會自動補全 `https://`）。
3. **歷史跳轉**：點擊頂部的「歷史」按鈕，在彈出的列表中雙擊任意歷史記錄即可瞬移回到該頁面。

---

## 🛠️ 技術棧

* **語言**：C# (.NET 8)
* **UI 框架**：Windows Forms
* **核心內核**：Microsoft.Web.WebView2 (Chromium)

---
## 软件内语言注意！
由于作者是闲时开发，未能加入您的所在国家地区或语言的话还请见谅。
---

## 📜 版權聲明

Copyright © 2026 Lemon Studio. All Rights Reserved.
