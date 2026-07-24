$log = 'C:\Program Files (x86)\Steam\steamapps\common\UEBS2\BepInEx\LogOutput.log'
Write-Host "=== Tail of LogOutput.log ==="
Get-Content $log -Tail 60
Write-Host "=== Matches ==="
Select-String -Path $log -Pattern 'engaged|Stereo tune|SBS  IPD|restored|Screen auto|Half-SBS stereo|v1\.1\.4' |
  Select-Object -Last 40 |
  ForEach-Object { $_.Line }
