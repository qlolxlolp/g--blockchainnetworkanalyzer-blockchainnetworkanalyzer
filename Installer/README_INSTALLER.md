# Blockchain Network Analyzer - Installer
# نصب‌کننده Blockchain Network Analyzer

## 📦 درباره / About

این نصب‌کننده نسخه قابل حمل برنامه را به یک نصب کامل تبدیل می‌کند.
This installer converts the portable version to a full installation.

## 🔧 نیازمندی‌ها / Requirements

برای ساخت نصب‌کننده / To build the installer:

1. **Inno Setup 6** (رایگان)
   - دانلود از: https://jrsoftware.org/isdl.php
   - Download from: https://jrsoftware.org/isdl.php
   - یا نسخه portable را در پوشه `Installer\InnoSetup\` قرار دهید
   - Or place portable version in `Installer\InnoSetup\` folder

2. **Portable Release** (باید قبلاً ساخته شده باشد)
   - اجرای `build_portable.bat` یا `create_portable_package.ps1`
   - Run `build_portable.bat` or `create_portable_package.ps1`

## 🚀 ساخت نصب‌کننده / Building Installer

### روش 1: استفاده از اسکریپت خودکار (توصیه می‌شود)
### Method 1: Using automated script (Recommended)

```powershell
cd BlockchainNetworkAnalyzer\BlockchainNetworkAnalyzer\Installer
.\build_installer_with_portable.ps1
```

این اسکریپت:
- ابتدا نسخه قابل حمل را می‌سازد
- سپس نصب‌کننده را ایجاد می‌کند

This script:
- First builds the portable release
- Then creates the installer

### روش 2: ساخت دستی
### Method 2: Manual build

```cmd
REM Step 1: Build portable release
cd BlockchainNetworkAnalyzer\BlockchainNetworkAnalyzer
.\build_portable.bat

REM Step 2: Build installer
cd Installer
.\build_installer.bat
```

### روش 3: استفاده از Inno Setup GUI
### Method 3: Using Inno Setup GUI

1. Inno Setup را باز کنید
2. فایل `installer.iss` را باز کنید
3. Build → Compile را اجرا کنید

## 📁 ساختار فایل‌ها / File Structure

```
Installer/
├── installer.iss                    → اسکریپت Inno Setup
├── build_installer.bat            → اسکریپت ساخت (CMD)
├── build_installer_with_portable.ps1 → اسکریپت ساخت کامل (PowerShell)
└── README_INSTALLER.md             → این فایل

Installer_Output/
└── Install_BlockchainNetworkAnalyzer.exe → فایل نصب‌کننده نهایی
```

## ✨ ویژگی‌های نصب‌کننده / Installer Features

✅ **نصب کامل / Full Installation**
   - نصب در Program Files
   - ایجاد Shortcut در Start Menu
   - ایجاد Shortcut در Desktop (اختیاری)

✅ **Uninstaller**
   - حذف کامل برنامه
   - حذف فایل‌های داده (اختیاری)

✅ **Registry Entries**
   - ثبت در Windows Registry
   - نمایش در Programs and Features

✅ **Auto Directory Creation**
   - ایجاد خودکار پوشه‌های Data, Logs, Reports, Exports

✅ **Admin Rights**
   - درخواست دسترسی Administrator برای نصب

## 📋 تنظیمات نصب / Installation Settings

- **مسیر نصب پیش‌فرض**: `C:\Program Files\Blockchain Network Analyzer`
- **گروه Start Menu**: `Blockchain Network Analyzer`
- **نیاز به Administrator**: بله

## 🎯 استفاده از نصب‌کننده / Using the Installer

1. دوبار کلیک روی `Install_BlockchainNetworkAnalyzer.exe`
2. دسترسی Administrator را تایید کنید
3. مسیر نصب را انتخاب کنید (یا پیش‌فرض را بپذیرید)
4. گزینه‌های اضافی را انتخاب کنید (Desktop shortcut و غیره)
5. Install را کلیک کنید
6. پس از نصب، برنامه به صورت خودکار اجرا می‌شود

## 🔄 حذف نصب / Uninstallation

1. Control Panel → Programs and Features
2. Blockchain Network Analyzer را پیدا کنید
3. Uninstall را کلیک کنید

یا:

1. Start Menu → Blockchain Network Analyzer
2. Uninstall را کلیک کنید

## 📝 تغییرات در installer.iss

برای سفارشی‌سازی نصب‌کننده، فایل `installer.iss` را ویرایش کنید:

- `MyAppName`: نام برنامه
- `MyAppVersion`: نسخه برنامه
- `MyAppPublisher`: نام ناشر
- `DefaultDirName`: مسیر نصب پیش‌فرض
- `SetupIconFile`: آیکون نصب‌کننده (اختیاری)

## ⚠️ نکات مهم / Important Notes

1. **Portable Release**: باید قبل از ساخت نصب‌کننده وجود داشته باشد
2. **Inno Setup**: باید نصب شده باشد یا نسخه portable در دسترس باشد
3. **Output**: فایل نصب‌کننده در `Installer_Output\` ایجاد می‌شود
4. **Size**: فایل نصب‌کننده حدود 105-110 MB خواهد بود

## 🎉 نتیجه / Result

پس از ساخت موفق، فایل `Install_BlockchainNetworkAnalyzer.exe` در پوشه `Installer_Output` ایجاد می‌شود که:

- قابل انتشار است
- قابل نصب در هر کامپیوتر Windows است
- شامل تمام وابستگی‌ها است
- Uninstaller دارد

After successful build, `Install_BlockchainNetworkAnalyzer.exe` will be created in `Installer_Output` folder which:

- Is distributable
- Can be installed on any Windows computer
- Includes all dependencies
- Has uninstaller

