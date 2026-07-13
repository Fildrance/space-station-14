@echo off
chcp 65001 >nul
echo ============================================================
echo Metadata Generator - Content.Client + Content.Server
echo This will scan both projects with Content.Shared as dependency
echo ============================================================
echo.

dotnet run --project RobustToolbox\Robust.MetadataGenerator -- source Resources\metadata\metadata.json --csproj Content.Client\Content.Client.csproj Content.Server\Content.Server.csproj
echo.
echo ============================================================
echo Done! Generated file: metadata.json
echo (Contains types from Content.Client, Content.Server, and Content.Shared)
echo ============================================================
pause

