Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public class Kbd2 {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
  public static void Tap(byte vk) {
    keybd_event(vk, 0, 0, UIntPtr.Zero);
    System.Threading.Thread.Sleep(40);
    keybd_event(vk, 0, 0x0002, UIntPtr.Zero);
  }
}
'@
$p = Get-Process -Name UEBS2 | Select-Object -First 1
[void][Kbd2]::SetForegroundWindow($p.MainWindowHandle)
Start-Sleep -Milliseconds 400
# F8 toggle off to leave a clean mono state after probe
[Kbd2]::Tap(0x77)
Start-Sleep -Milliseconds 800
Write-Host "Sent F8 cleanup toggle"
Get-Content "C:\Program Files (x86)\Steam\steamapps\common\UEBS2\BepInEx\LogOutput.log" -Tail 8
