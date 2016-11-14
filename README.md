VesselViewer - Continuation of Kronal Vessel Viewer <sup>[original thread](http://www.curse.com/ksp-mods/kerbal/224287-kronal-vessel-viewer-kvv-exploded-ship-view)</sup>

_Yet another fork to make the mod compatible with KSP 1.2.0._

### Usage

When in the VAB/SPH, click the "KVV" icon to toggle the screenshot UI pane. More docs would make for a nice PR.


### Building

1) Run `package.bat` in an msbuild-enabled command prompt.

2) Copy the resulting `.\GameData` directory into your KSP install directory.


### Known Issues

Currently, it loads in KSP 1.2.0 but most of the shader-related aspects are not working as intended. Specifically, the blueprint and color-adjust shaders need to be rewritten because the existing versions were precompiled and so not supported with Unity 5.4, which is a requirement for KSP 1.2.


### Changelog

#### v0.0.5
* Includes [Git fingerboxes](https://github.com/fingerboxes) patch for in-game rendering glitches & other weirdness (unconfirmed bug)
* Thanks to KSP-IRC: TaranisElsu for helping me find the solution to the VAB/SPH facitilty detection
* Thanks to [Git m4v](https://github.com/m4v) (RCS Build Assist) again for keeping their code public so I could reference the click through code


#### v0.0.4 - Pitch Perfect
* Added 'Auto-Preview' checkbox (for slower computers)
* Fixed Bug where parts would not 'Offset' (Formerly Explode View) unless Procedural Fairings was installed
* Background colour sliders (white is no longer the only background colour render option) located under 'Blue Print'
* Blue Print Shader is now disabled by default
  * 'Blue Print shader' was causing the issue with the white rendering lines and off colouring in the bottom left corner
  * Background colour controls are now available under 'Blue Print' which will eventually become 'Background' or 'Canvas'
* UI Adjustments
  * Shadow Control Dial (experimental)
  * Bigger buttons
  * Moved Orthographic Button
  * Changed 'Exploded' references to 'Offset'
  * Image quality can now be controlled with a dial
* [Git deckblad](https://github.com/deckblad) ([KSP forums mrBlaQ](http://forum.kerbalspaceprogram.com/members/102679-mrBlaQ)) resolved:
  * Shadow Rendering Control
  * Adjusted Camera Positioning
  * Improved Camera Controls
  * Part Option for Clamps
  * Procedural fairings bug fixes
    * Existing bug still exists where you must select minimum 4 fairings to hide 'Front Half'
  * Edge Detect shader adjustment


#### v0.0.3 - mrBlaQ
* GUI Window Click trap implmented.  (Thanks [Git M4V](https://github.com/m4v/RCSBuildAid/blob/master/Plugin/GUI/MainWindow.cs#L296) for directing me here)
* [Git deckblad](https://github.com/deckblad) ([KSP forums mrBlaQ](http://forum.kerbalspaceprogram.com/members/102679-mrBlaQ)) resolved:
  * Fixed white lines issue by restricting image size to 4096px (max any dimension)
  * Made all renders Jump Up to 4096px.  This creates higher quality renders with smaller craft.
* Nils Daumann [\(Git Slin\)](https://github.com/Slin/) was kind enough to change the license on the fxaa shader. 
* To Install:
  * Replace all Existing <KSP ROOT>GameData/KronalUtils/ files (.DLL & KronalUtils/fxaa shader changed but be sure replace everything)
  * No Dependancies
* To Build/Compile:
  * Normal KSP Modding (Build with required KSP DLLs)
  * Download and Build with [KAS dll](https://github.com/KospY/KAS)


#### v0.0.2 - Dat-U-Eye

* Change Config Defaults
* Changed button layouts and preview
* [Git deckblad](https://github.com/deckblad) ([KSP forums mrBlaQ](http://forum.kerbalspaceprogram.com/members/102679-mrBlaQ)) added support for [KAS Parts](https://github.com/KospY/KAS)


#### v0.0.1 - El Padlina

* Fixed glitch where Save button wouldn't undisable.  Now disables when you click 'Revert' after click 'Explode'
* Commits from [Pull Request 4e2601f](https://github.com/WojtekWZ/ksp-kronalutils/commit/4e2601f071dcb2d573b49d096c2a7c3e0fdf05ae) from [Git WojtekWZ](https://github.com/WojtekWZ) aka [Reddit /u/el_padlina](http://www.reddit.com/user/el_padlina)
  * Added GUI Button
  * New Dials for better control over shaders


#### v0.0.0 - Revival

* Made 'Stable' with Stock KSP v0.24.2
* Writes to screenshot folder (Windows/OSX confirmed)
* Includes name of Vessel in filename


#### v0.0.0a

* Made 'Stable' with Stock KSP v0.23.0
