$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\Assembly-CSharp.dll")
$pc = $asm.MainModule.GetType("PlayerControl")
"--- PlayerControl props"
foreach ($p in $pc.Properties) { if ($p.Name -match 'roleAssigned|killTimer|inVent|IsKillTimerEnabled|Visible|PlayerId|MyPhysics|NetTransform|Data|CurrentRole') { "P static=$($p.GetMethod.IsStatic) $($p.PropertyType.FullName) $($p.Name) set=$($p.SetMethod -ne $null)" } }
"--- DestroyableSingleton type search"
$asm.MainModule.GetTypes() | Where-Object { $_.Name -match 'DestroyableSingleton' } | ForEach-Object { $_.FullName }
"--- DestroyableSingleton members"
$ds = $asm.MainModule.GetType("DestroyableSingleton`1")
if ($ds) { foreach ($m in $ds.Methods) { "M static=$($m.IsStatic) $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name)" }) -join ',') + ")" }; foreach ($f in $ds.Fields) { "F static=$($f.IsStatic) $($f.FieldType.Name) $($f.Name)" } }
"--- HudManager type base"
$hm = $asm.MainModule.GetType("HudManager")
"base: $($hm.BaseType.FullName)"
"--- ActionButton methods public"
$ab = $asm.MainModule.GetType("ActionButton")
foreach ($m in $ab.Methods) { if ($m.IsPublic -and $m.Name -match 'SetCoolDown|SetEnabled|SetDisabled|ToggleVisible|ResetCoolDown|DoClick|CanInteract|OverrideText|OverrideColor|SetAsCoolingDown') { "M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
"--- MeetingHud"
$mh = $asm.MainModule.GetType("MeetingHud")
foreach ($p in $mh.Properties) { if ($p.Name -match 'localPlayer|state|Instance') { "P $($p.PropertyType.Name) $($p.Name)" } }
"--- GameData Instance type"
$gd = $asm.MainModule.GetType("GameData")
"base: $($gd.BaseType.FullName)"
"--- ExileController"
$ec = $asm.MainModule.GetType("ExileController")
foreach ($p in $ec.Properties) { if ($p.Name -match 'initData|exiled|Instance') { "P $($p.PropertyType.Name) $($p.Name)" } }
