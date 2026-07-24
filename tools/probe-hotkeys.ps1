Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public class Kbd {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
  public const uint KEYEVENTF_KEYUP = 0x0002;
  public static void Tap(byte vk) {
    keybd_event(vk, 0, 0, UIntPtr.Zero);
    System.Threading.Thread.Sleep(40);
    keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
  }
}
'@

$p = Get-Process -Name UEBS2 -ErrorAction Stop | Select-Object -First 1
if ($p.MainWindowHandle -eq [IntPtr]::Zero) { throw "UEBS2 has no main window handle" }
[void][Kbd]::ShowWindow($p.MainWindowHandle, 9)
Start-Sleep -Milliseconds 200
[void][Kbd]::SetForegroundWindow($p.MainWindowHandle)
Start-Sleep -Milliseconds 800
Write-Host "Focused PID=$($p.Id) hwnd=$($p.MainWindowHandle)"

# VK_F8=0x77, F4=0x73, F2=0x71, F3=0x72, F1=0x70
[Kbd]::Tap(0x77)
Write-Host "Sent F8"
Start-Sleep -Seconds 2.5
1..3 | ForEach-Object { [Kbd]::Tap(0x73); Start-Sleep -Milliseconds 250 }
Write-Host "Sent F4 x3"
Start-Sleep -Milliseconds 200
1..2 | ForEach-Object { [Kbd]::Tap(0x71); Start-Sleep -Milliseconds 250 }
Write-Host "Sent F2 x2"
Start-Sleep -Seconds 1
Write-Host "Done sending keys"
