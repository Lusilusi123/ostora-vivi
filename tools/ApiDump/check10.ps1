$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\Assembly-CSharp.dll")
$pc = $asm.MainModule.GetType("PlayerControl")
"--- PlayerControl fields"
foreach ($f in $pc.Fields) { if ($f.Name -match '^(roleAssigned|killTimer|inVent|IsKillTimerEnabled|Visible|PlayerId|MyPhysics|NetTransform)$') { "F public=$($f.IsPublic) static=$($f.IsStatic) $($f.FieldType.Name) $($f.Name)" } }
"--- PlayerControl methods of interest"
foreach ($m in $pc.Methods) { if ($m.IsPublic -and $m.Name -match '^(GetTruePosition|RpcSetRole|CoSetRole|SetRole|MurderPlayer|RpcMurderPlayer|CmdCheckMurder|CheckMurder|Revive|Die)$') { "M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
"--- AmongUsClient instance"
$ac = $asm.MainModule.GetType("AmongUsClient")
foreach ($p in $ac.Properties) { if ($p.Name -eq 'Instance') { "P static=$($p.GetMethod.IsStatic) $($p.PropertyType.FullName) $($p.Name)" } }
foreach ($f in $ac.Fields) { if ($f.Name -eq 'Instance') { "F static=$($f.IsStatic) $($f.FieldType.FullName) $($f.Name)" } }
"--- HudManager buttons"
$hm = $asm.MainModule.GetType("HudManager")
foreach ($p in $hm.Properties) { if ($p.Name -match 'KillButton|ImpostorVentButton|PlayerCam|UICamera|Chat') { "P $($p.PropertyType.FullName) $($p.Name)" } }
"--- ActionButton props"
$ab = $asm.MainModule.GetType("ActionButton")
foreach ($p in $ab.Properties) { "P $($p.PropertyType.Name) $($p.Name)" }
"--- KillButton methods"
$kb = $asm.MainModule.GetType("KillButton")
foreach ($m in $kb.Methods) { if ($m.IsPublic) { "M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
"--- GameData AllPlayers"
$gd = $asm.MainModule.GetType("GameData")
foreach ($p in $gd.Properties) { if ($p.Name -match 'AllPlayers|Instance|PlayerCount') { "P $($p.PropertyType.FullName) $($p.Name)" } }
"--- ShipStatus Instance"
$ss = $asm.MainModule.GetType("ShipStatus")
foreach ($p in $ss.Properties) { if ($p.Name -match 'Instance|AllVents|AllCameras') { "P $($p.PropertyType.FullName) $($p.Name)" } }
"--- Vent methods"
$v = $asm.MainModule.GetType("Vent")
foreach ($m in $v.Methods) { if ($m.IsPublic) { "M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
"--- CustomNetworkTransform SnapTo"
$cnt = $asm.MainModule.GetType("CustomNetworkTransform")
foreach ($m in $cnt.Methods) { if ($m.Name -match 'SnapTo') { "M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
"--- DestroyableSingleton Instance"
$ds = $asm.MainModule.GetType("DestroyableSingleton`1")
foreach ($p in $ds.Properties) { "P static=$($p.GetMethod.IsStatic) $($p.PropertyType.Name) $($p.Name)" }
