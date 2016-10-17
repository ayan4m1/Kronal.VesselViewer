@echo off

msbuild /t:Build /p:Configuration=Release /p:TargetFramework=v3.5 Kronal.VesselViewer.sln
mkdir GameData
mkdir GameData\VesselViewer

xcopy VesselViewer\Resources GameData\VesselViewer /E

mkdir GameData\VesselViewer\Plugins
copy VesselViewer\bin\Release\VesselViewer.dll GameData\VesselViewer\Plugins\