$ErrorActionPreference = 'Stop'
$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\Assembly-CSharp.dll")

function DumpType($name, $filter) {
  $t = $asm.MainModule.GetType($name)
  if ($null -eq $t) { "MISSING $name"; return }
  "=== $($t.FullName)"
  foreach ($p in $t.Properties) {
    if ($filter -eq '' -or $p.Name -match $filter) {
      $st = if ($p.GetMethod) { $p.GetMethod.IsStatic } else { $false }
      $set = if ($p.SetMethod) { 'set' } else { '' }
      "  P static=$st $($p.PropertyType.Name) $($p.Name) $set"
    }
  }
  foreach ($m in $t.Methods) {
    if ($filter -eq '' -or $m.Name -match $filter) {
      "  M static=$($m.IsStatic) $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")"
    }
  }
}

DumpType 'HudManager' 'SetHudActive|KillButton|ImpostorVentButton|PlayerCam|UICamera|Chat'
DumpType 'PlayerControl' 'Murder|CheckMurder|Die$|Revive|SetKillTimer|killTimer|RpcSetRole|CoSetRole|GetTruePosition|ReportDeadBody|roleAssigned|IsKillTimerEnabled|NetTransform|Visible|inVent|Data$|PlayerId|AmOwner'
DumpType 'NetworkedPlayerInfo' 'Disconnected|IsDead|RoleType|RoleWhenAlive|Role$|PlayerName|Color|Object|PlayerId'
DumpType 'RoleManager' 'Instance|SetRole|GetRole|IsImpostorRole'
DumpType 'AmongUsClient' 'Instance'
DumpType 'GameData' 'Instance|AllPlayers'
DumpType 'ShipStatus' 'Instance|AllVents'
DumpType 'MeetingHud' 'Instance'
DumpType 'RoleBehaviour' 'NiceName|IsImpostor|TeamType|CanUseKillButton|CanVent|CanBeKilled'

"=== UnityEngine.Object name"
$core = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\UnityEngine.CoreModule.dll")
$obj = $core.MainModule.GetType("UnityEngine.Object")
foreach ($p in $obj.Properties) { if ($p.Name -eq 'name') { "  P static=$($p.GetMethod.IsStatic) $($p.PropertyType.Name) $($p.Name)" } }
foreach ($m in $obj.Methods) { if ($m.Name -match 'FindObjectsOfType|DontDestroyOnLoad') { "  M static=$($m.IsStatic) $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ',') + ")" } }
"=== Camera"
$cam = $core.MainModule.GetType("UnityEngine.Camera")
foreach ($p in $cam.Properties) { if ($p.Name -match '^main$|orthographic') { "  P static=$($p.GetMethod.IsStatic) $($p.PropertyType.Name) $($p.Name)" } }
foreach ($m in $cam.Methods) { if ($m.Name -match 'WorldToScreenPoint|ScreenToWorldPoint') { "  M static=$($m.IsStatic) $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ',') + ")" } }
"=== Component"
$comp = $core.MainModule.GetType("UnityEngine.Component")
foreach ($m in $comp.Methods) { if ($m.Name -match 'GetComponent|get_transform|get_gameObject') { "  M static=$($m.IsStatic) $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ',') + ")" } }
"=== Transform"
$tr = $core.MainModule.GetType("UnityEngine.Transform")
foreach ($p in $tr.Properties) { if ($p.Name -match '^position$|^localPosition$') { "  P $($p.PropertyType.Name) $($p.Name)" } }
"=== Vector2/Vector3"
foreach ($vn in @('UnityEngine.Vector2','UnityEngine.Vector3')) {
  $v = $core.MainModule.GetType($vn)
  "  $vn"
  foreach ($m in $v.Methods) { if ($m.IsConstructor -and $m.Parameters.Count -eq 2 -and $vn -eq 'UnityEngine.Vector2') { "    ctor(2)" } elseif ($m.IsConstructor -and $m.Parameters.Count -eq 3 -and $vn -eq 'UnityEngine.Vector3') { "    ctor(3)" } }
  foreach ($f in $v.Fields) { if ($f.Name -match '^x$|^y$|^z$') { "    F $($f.FieldType.Name) $($f.Name)" } }
}

"=== IMGUI"
$imgui = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\UnityEngine.IMGUIModule.dll")
$g = $imgui.MainModule.GetType("UnityEngine.GUI")
foreach ($m in $g.Methods) { if ($m.IsPublic -and $m.Name -match '^TextField$|^HorizontalSlider$|^BeginScrollView$|^EndScrollView$|^DrawTexture$|^Toggle$|^Button$|^Label$|^Box$') { "  M static=$($m.IsStatic) $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ',') + ")" } }
foreach ($p in $g.Properties) { if ($p.Name -match 'color|backgroundColor|contentColor|skin') { "  P static=$($p.GetMethod.IsStatic) $($p.PropertyType.Name) $($p.Name)" } }
$ev = $imgui.MainModule.GetType("UnityEngine.Event")
if ($ev) { foreach ($p in $ev.Properties) { if ($p.Name -match 'current|mousePosition') { "  Event P static=$($p.GetMethod.IsStatic) $($p.PropertyType.Name) $($p.Name)" } } }
