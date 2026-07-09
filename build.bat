@echo off
setlocal

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo .NET Framework C# compiler not found.
    exit /b 1
)

if not exist "bin" mkdir bin

"%CSC%" /nologo /target:exe /out:bin\IconWriter.exe ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  src\AppIcon.cs ^
  src\IconWriter.cs

if errorlevel 1 (
    echo Icon generation failed.
    exit /b 1
)

bin\IconWriter.exe app.ico

"%CSC%" /nologo /target:winexe /out:bin\Sanity.exe ^
  /win32icon:app.ico ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Web.Extensions.dll ^
  src\AppConfig.cs ^
  src\AppIcon.cs ^
  src\ClipboardMonitor.cs ^
  src\ConfigForm.cs ^
  src\NativeMethods.cs ^
  src\Program.cs ^
  src\StartupRegistration.cs ^
  src\TrayApplicationContext.cs ^
  src\UrlCleaner.cs

if errorlevel 1 (
    echo Build failed.
    exit /b 1
)

echo Built bin\Sanity.exe
bin\Sanity.exe --write-default-config
copy /Y bin\config.json config.json >nul
echo Default config.json updated.
echo Run bin\Sanity.exe to start the tray app.
