# Releases

| File | Who it's for | Install |
|------|----------------|---------|
| **[UEBS2Stereo-EasyInstall-1.1.6.zip](UEBS2Stereo-EasyInstall-1.1.6.zip)** | Most players | Open `Into_UEBS2_Game_Folder`, copy everything into the UEBS2 game folder (or run `Install.bat`). Includes BepInEx. |
| [UEBS2Stereo-1.1.6.zip](UEBS2Stereo-1.1.6.zip) | Already have BepInEx | Extract into the UEBS2 game folder (`BepInEx/plugins/UEBS2Stereo/`). |

Rebuild:

```powershell
powershell -File tools/package-easy-install.ps1
powershell -File tools/package-nexus.ps1
```
