@echo off
setlocal EnableDelayedExpansion

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo .NET Framework C# compiler not found.
    exit /b 1
)

set BIN=bin
if not exist "%BIN%" mkdir "%BIN%"

rem --- Version (single source of truth) ---
set VERSION=1.0.0
if exist VERSION (
    set /p VERSION=<VERSION
)
for /f "tokens=* delims= " %%A in ("%VERSION%") do set VERSION=%%A
echo Building Sanity %VERSION%

rem Sync assembly metadata with VERSION
> src\AssemblyInfo.cs (
    echo using System.Reflection;
    echo.
    echo [assembly: AssemblyTitle("Sanity"^)]
    echo [assembly: AssemblyDescription("Strip tracking parameters from URLs"^)]
    echo [assembly: AssemblyCompany("XAte6"^)]
    echo [assembly: AssemblyProduct("Sanity"^)]
    echo [assembly: AssemblyCopyright("Use and modify as you like."^)]
    echo [assembly: AssemblyVersion("%VERSION%.0"^)]
    echo [assembly: AssemblyFileVersion("%VERSION%.0"^)]
    echo [assembly: AssemblyInformationalVersion("%VERSION%"^)]
)

"%CSC%" /nologo /target:exe /out:%BIN%\IconWriter.exe ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  src\AppIcon.cs ^
  src\IconWriter.cs

if errorlevel 1 (
    echo Icon generation failed.
    exit /b 1
)

%BIN%\IconWriter.exe app.ico

set RESOURCE_ARGS=
if exist "assets\default-apps-setup.gif" (
    set RESOURCE_ARGS=/resource:assets\default-apps-setup.gif,Sanity.default-apps-setup.gif
) else (
    echo WARNING: assets\default-apps-setup.gif not found — setup wizard step 2 will show a placeholder.
)

"%CSC%" /nologo /target:winexe /out:%BIN%\Sanity.exe ^
  /win32icon:app.ico ^
  %RESOURCE_ARGS% ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Core.dll ^
  /reference:System.Web.Extensions.dll ^
  src\AssemblyInfo.cs ^
  src\AppConfig.cs ^
  src\AppIcon.cs ^
  src\AppLinks.cs ^
  src\BrowserHelper.cs ^
  src\ClipboardMonitor.cs ^
  src\ConfigForm.cs ^
  src\DefaultRules.cs ^
  src\LinkOpener.cs ^
  src\NativeMethods.cs ^
  src\Notifier.cs ^
  src\Program.cs ^
  src\ProtocolRegistration.cs ^
  src\SetupWizardForm.cs ^
  src\StartupRegistration.cs ^
  src\StatisticsForm.cs ^
  src\SvgPath.cs ^
  src\TrayApplicationContext.cs ^
  src\UiChrome.cs ^
  src\UpdateChecker.cs ^
  src\UrlCleaner.cs ^
  src\UsageMetrics.cs

if errorlevel 1 (
    echo Build failed.
    exit /b 1
)

echo Built %BIN%\Sanity.exe
copy /Y ..\defaults\regex-rules.json %BIN%\regex-rules.json >nul
%BIN%\Sanity.exe --write-default-config
if exist "%BIN%\config.json" (
    copy /Y "%BIN%\config.json" config.json >nul
)

rem --- Inno Setup installer ---
set ISCC=
if exist "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" set ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe
if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe

if not defined ISCC (
    echo.
    echo Inno Setup 6 not found. Install it, then re-run build.bat:
    echo   winget install --id JRSoftware.InnoSetup -e
    echo Portable exe is ready at %BIN%\Sanity.exe
    exit /b 1
)

set RELEASES=..\releases
if not exist "%RELEASES%" mkdir "%RELEASES%"

"%ISCC%" /DMyAppVersion=%VERSION% /Q Sanity.iss
if errorlevel 1 (
    echo Installer build failed.
    exit /b 1
)

echo Built %RELEASES%\Sanity-win-x86-setup.exe
echo Run %BIN%\Sanity.exe for a local tray build, or install from releases\.
