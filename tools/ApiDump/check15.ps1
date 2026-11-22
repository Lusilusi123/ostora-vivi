$ErrorActionPreference = 'Stop'
$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\Assembly-CSharp.dll")
foreach ($n in @('RoleManager','HudManager','MeetingHud','ShipStatus','GameData','AmongUsClient','PlayerControl','KillButton','VentButton','ActionButton')) {
  $t = $asm.MainModule.GetType($n)
  if ($t) { "$n : $($t.BaseType.FullName)" } else { "MISSING $n" }
}
"--- DestroyableSingleton"
$ds = $asm.MainModule.GetType("DestroyableSingleton`1")
if ($ds) {
  foreach ($p in $ds.Properties) { "  P static=$($p.GetMethod.IsStatic) $($p.PropertyType.Name) $($p.Name)" }
  foreach ($m in $ds.Methods) { if ($m.Name -match 'get_Instance') { "  M static=$($m.IsStatic) $($m.ReturnType.Name) $($m.Name)" } }
}
"--- PlayerControl statics"
$pc = $asm.MainModule.GetType("PlayerControl")
foreach ($p in $pc.Properties) { if ($p.GetMethod -and $p.GetMethod.IsStatic) { "  P $($p.PropertyType.Name) $($p.Name)" } }
"--- RoleManager AllRoles"
$rm = $asm.MainModule.GetType("RoleManager")
foreach ($p in $rm.Properties) { "  P $($p.PropertyType.Name) $($p.Name)" }
foreach ($f in $rm.Fields) { "  F $($f.FieldType.Name) $($f.Name)" }
"--- KillButton members"
$kb = $asm.MainModule.GetType("KillButton")
foreach ($m in $kb.Methods) { if ($m.Name -match 'DoClick|CheckClick|SetTarget|ResetKillButton|ChangeButtonText|ChangeGraphic') { "  M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ',') + ")" } }
"--- ActionButton key members"
$ab = $asm.MainModule.GetType("ActionButton")
foreach ($m in $ab.Methods) { if ($m.Name -match 'SetCoolDown|SetEnabled|SetDisabled|ToggleVisible|ResetCoolDown|OverrideText|CanInteract') { "  M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ',') + ")" } }
"--- AmongUsClient fields"
$ac = $asm.MainModule.GetType("AmongUsClient")
foreach ($p in $ac.Properties) { if ($p.Name -match 'AmHost|AmLocalHost|AmClient|GameState|ClientId|HostId|IsGameStarted|IsInGame') { "  P $($p.PropertyType.Name) $($p.Name)" } }
"--- CustomNetworkTransform"
$cnt = $asm.MainModule.GetType("CustomNetworkTransform")
foreach ($m in $cnt.Methods) { if ($m.Name -match 'RpcSnapTo|SnapTo') { "  M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ',') + ")" } }
"--- DeadBody"
$db = $asm.MainModule.GetType("DeadBody")
foreach ($p in $db.Properties) { "  P $($p.PropertyType.Name) $($p.Name)" }
"--- Vent transform check"
$v = $asm.MainModule.GetType("Vent")
foreach ($p in $v.Properties) { if ($p.Name -match 'transform|Id') { "  P $($p.PropertyType.Name) $($p.Name)" } }
