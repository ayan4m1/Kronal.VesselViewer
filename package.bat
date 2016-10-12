@echo off

msbuild /t:Build /p:Configuration=Release /p:TargetFramework=v3.5 Kronal.VesselViewer.sln
mkdir GameData
xcopy VesselViewer\Resources GameData\ /E
mkdir GameData\Plugins
copy VesselViewer\bin\Release\VesselViewer.dll GameData\Plugins\