$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\Assembly-CSharp.dll")
"--- every type named GameStates"
$asm.MainModule.GetTypes() | Where-Object { $_.Name -eq 'GameStates' } | ForEach-Object { "$($_.FullName)" }
"--- AmongUsClient props"
$ac = $asm.MainModule.GetType("InnerNet.InnerNetClient")
foreach ($p in $ac.Properties) { if ($p.Name -match 'Instance|AmHost|AmLocalHost|AmClient|GameState|IsGameStarted|IsInGame|ClientId|HostId|GameId') { "P $($p.PropertyType.FullName) $($p.Name)" } }
"--- RoleBehaviour"
$rb = $asm.MainModule.GetType("RoleBehaviour")
foreach ($p in $rb.Properties) { "P $($p.PropertyType.FullName) $($p.Name)" }
"--- ActionButton"
$ab = $asm.MainModule.GetType("ActionButton")
foreach ($m in $ab.Methods) { if ($m.IsPublic -and $m.Name -match 'SetCoolDown|SetEnabled|SetDisabled|ToggleVisible|ResetCoolDown|DoClick|CanInteract|OverrideText|OverrideColor') { "M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
foreach ($p in $ab.Properties) { if ($p.Name -match 'IsOnCooldown|isCoolingDown|OverrideText|currentTarget|Target') { "P $($p.PropertyType.FullName) $($p.Name)" } }
"--- KillButton"
$kb = $asm.MainModule.GetType("KillButton")
foreach ($p in $kb.Properties) { "P $($p.PropertyType.FullName) $($p.Name)" }
foreach ($m in $kb.Methods) { if ($m.IsPublic -and $m.Name -match 'SetTarget|ResetKill|DoClick|CheckClick') { "M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
"--- Vent"
$v = $asm.MainModule.GetType("Vent")
foreach ($m in $v.Methods) { if ($m.IsPublic -and $m.Name -match 'EnterVent|ExitVent|CanUse|SetButtons') { "M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
foreach ($p in $v.Properties) { if ($p.Name -match 'Id$|Left|Right|Center|NearbyVents|currentVent') { "P $($p.PropertyType.FullName) $($p.Name)" } }
"--- CustomNetworkTransform"
$cnt = $asm.MainModule.GetType("CustomNetworkTransform")
foreach ($m in $cnt.Methods) { if ($m.IsPublic -and $m.Name -match 'SnapTo') { "M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
"--- PlayerControl key props"
$pc = $asm.MainModule.GetType("PlayerControl")
foreach ($p in $pc.Properties) { if ($p.Name -match '^(Data|NetTransform|MyPhysics|Visible|inVent|killTimer|roleAssigned|CurrentRole|LocalPlayer|AllPlayerControls|PlayerId)$') { "P $($p.PropertyType.FullName) $($p.Name)" } }
"--- PlayerControl static fields/props"
foreach ($p in $pc.Properties) { if ($p.GetMethod -and $p.GetMethod.IsStatic) { "STATIC P $($p.PropertyType.FullName) $($p.Name)" } }
"--- DeadBody"
$db = $asm.MainModule.GetType("DeadBody")
foreach ($p in $db.Properties) { if ($p.Name -match 'TruePosition|ParentId|Reported') { "P $($p.PropertyType.FullName) $($p.Name)" } }
"--- HudManager SetHudActive"
$hm = $asm.MainModule.GetType("HudManager")
foreach ($m in $hm.Methods) { if ($m.Name -match 'SetHudActive') { "M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
"--- GetComponent on Component"
$comp = $asm.MainModule.GetType("UnityEngine.Component")
if (-not $comp) { $ua = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\UnityEngine.CoreModule.dll"); $comp = $ua.MainModule.GetType("UnityEngine.Component") }
foreach ($m in $comp.Methods) { if ($m.IsPublic -and $m.Name -match '^GetComponent') { "M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
