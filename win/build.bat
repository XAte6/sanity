@echo off
setlocal

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo .NET Framework C# compiler not found.
    exit /b 1
)

set BIN=bin
if not exist "%BIN%" mkdir "%BIN%"

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

"%CSC%" /nologo /target:winexe /out:%BIN%\Sanity.exe ^
  /win32icon:app.ico ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Web.Extensions.dll ^
  src\AppConfig.cs ^
  src\AppIcon.cs ^
  src\BrowserHelper.cs ^
  src\ClipboardMonitor.cs ^
  src\ConfigForm.cs ^
  src\LinkOpener.cs ^
  src\NativeMethods.cs ^
  src\Program.cs ^
  src\ProtocolRegistration.cs ^
  src\StartupRegistration.cs ^
  src\TrayApplicationContext.cs ^
  src\UrlCleaner.cs

if errorlevel 1 (
    echo Build failed.
    exit /b 1
)

echo Built %BIN%\Sanity.exe
%BIN%\Sanity.exe --write-default-config
if exist "config.json" (
    copy /Y config.json %BIN%\config.json >nul
)

set RELEASES=..\releases
if not exist "%RELEASES%" mkdir "%RELEASES%"
copy /Y "%BIN%\Sanity.exe" "%RELEASES%\Sanity-win-x86.exe" >nul
echo Copied %RELEASES%\Sanity-win-x86.exe

echo Run %BIN%\Sanity.exe to start the tray app.
