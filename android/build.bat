@echo off
setlocal

set ROOT=%~dp0
set BIN=%ROOT%bin
if not exist "%BIN%" mkdir "%BIN%"

where gradle >nul 2>&1
if errorlevel 1 (
    echo Gradle not found. Install Gradle or Android Studio, then run:
    echo   cd android
    echo   gradle :app:assembleRelease
    exit /b 1
)

pushd "%ROOT%"
gradle :app:assembleRelease
if errorlevel 1 (
    echo Android build failed.
    popd
    exit /b 1
)

set RELEASES=%ROOT%..\releases
if not exist "%RELEASES%" mkdir "%RELEASES%"

set ARM_APK=app\build\outputs\apk\release\app-arm64-v8a-release-unsigned.apk
if not exist "%ARM_APK%" set ARM_APK=app\build\outputs\apk\release\app-arm64-v8a-release.apk
set X86_APK=app\build\outputs\apk\release\app-x86_64-release-unsigned.apk
if not exist "%X86_APK%" set X86_APK=app\build\outputs\apk\release\app-x86_64-release.apk

if not exist "%ARM_APK%" (
    echo ARM APK not found: %ARM_APK%
    popd
    exit /b 1
)
if not exist "%X86_APK%" (
    echo x86 APK not found: %X86_APK%
    popd
    exit /b 1
)

copy /Y "%ARM_APK%" "%BIN%\Sanity.apk" >nul
copy /Y "%ARM_APK%" "%RELEASES%\Sanity-android-arm.apk" >nul
copy /Y "%X86_APK%" "%RELEASES%\Sanity-android-x86.apk" >nul

popd
echo Built %BIN%\Sanity.apk
echo Copied %RELEASES%\Sanity-android-arm.apk
echo Copied %RELEASES%\Sanity-android-x86.apk
