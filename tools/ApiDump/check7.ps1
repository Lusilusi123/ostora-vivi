$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
$core = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\core"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\Assembly-CSharp.dll")
foreach ($n in @("PlayerControl","NetworkedPlayerInfo","RoleManager","HudManager","KillButton","VentButton","GameData","ShipStatus","Vent","DeadBody","AmongUsClient","DeathReason","MurderResultFlags","GameStates","MapType","PlayerOutfitType","RoleTeamTypes","RoleTypes","ActionButton","FollowerCamera","CustomNetworkTransform","ExileController","GameOptionsData")) {
  $t = $asm.MainModule.GetType($n)
  if (-not $t) { $t = $asm.MainModule.Types | Where-Object { $_.Name -eq $n } | Select-Object -First 1 }
  if (-not $t) { "NOT FOUND $n"; continue }
  "$($t.FullName) : $($t.BaseType)"
  if ($t.IsEnum) { "   values: " + (($t.Fields | Where-Object { $_.IsStatic } | ForEach-Object { $_.Name }) -join ', ') }
}
"--- RoleManager members"
$rm = $asm.MainModule.GetType("RoleManager")
foreach ($p in $rm.Properties) { "P $($p.PropertyType.FullName) $($p.Name)" }
foreach ($m in $rm.Methods) { if ($m.IsPublic) { "M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
"--- HudManager killbutton props"
$hm = $asm.MainModule.GetType("HudManager")
foreach ($p in $hm.Properties) { if ($p.Name -match 'Kill|Vent|PlayerCam|Instance') { "P $($p.PropertyType.FullName) $($p.Name)" } }
"--- BasePlugin"
$bp = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$core\BepInEx.Unity.IL2CPP.dll").MainModule.GetType("BepInEx.Unity.IL2CPP.BasePlugin")
foreach ($m in $bp.Methods) { if ($m.IsPublic -and $m.Name -match 'AddComponent|Load|Unload') { "M $($m.ReturnType.FullName) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
"--- GUIStyleState + ScaleMode"
foreach ($f in @("UnityEngine.IMGUIModule.dll","UnityEngine.CoreModule.dll","UnityEngine.TextRenderingModule.dll")) {
  $a = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\$f")
  foreach ($t in $a.MainModule.Types) { if ($t.Name -match '^GUIStyleState$|^ScaleMode$|^TextAnchor$|^FontStyle$') { "$($t.FullName) in $f"; foreach ($p in $t.Properties) { if ($p.Name -match 'textColor|background') { "  P $($p.PropertyType.Name) $($p.Name)" } } } }
}
