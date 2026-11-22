$ErrorActionPreference = 'Stop'
$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\Assembly-CSharp.dll")
"--- DestroyableSingleton`1 fields"
$ds = $asm.MainModule.GetType("DestroyableSingleton`1")
foreach ($f in $ds.Fields) { "  F static=$($f.IsStatic) $($f.FieldType.Name) $($f.Name)" }
foreach ($m in $ds.Methods) { "  M static=$($m.IsStatic) $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ',') + ")" }
"--- AmongUsClient fields"
$ac = $asm.MainModule.GetType("AmongUsClient")
foreach ($f in $ac.Fields) { if ($f.Name -match 'AmHost|AmLocalHost|AmClient|GameState|ClientId|HostId|IsGameStarted|IsInGame|Instance') { "  F static=$($f.IsStatic) $($f.FieldType.Name) $($f.Name)" } }
"--- InnerNetClient fields"
$inc = $asm.MainModule.GetType("InnerNet.InnerNetClient")
foreach ($f in $inc.Fields) { if ($f.Name -match 'AmHost|AmLocalHost|AmClient|GameState|ClientId|HostId|IsGameStarted|IsInGame|Instance') { "  F static=$($f.IsStatic) $($f.FieldType.Name) $($f.Name)" } }
foreach ($p in $inc.Properties) { if ($p.Name -match 'AmHost|AmLocalHost|AmClient|GameState|ClientId|HostId|IsGameStarted|IsInGame|Instance') { "  P static=$($p.GetMethod.IsStatic) $($p.PropertyType.Name) $($p.Name)" } }
"--- HudManager.Instance path"
$hm = $asm.MainModule.GetType("HudManager")
foreach ($m in $hm.Methods) { if ($m.Name -match 'get_Instance|set_Instance') { "  M static=$($m.IsStatic) $($m.ReturnType.Name) $($m.Name)" } }
"--- PlayerControl LocalPlayer getter"
$pc = $asm.MainModule.GetType("PlayerControl")
foreach ($m in $pc.Methods) { if ($m.Name -match 'get_LocalPlayer|get_AllPlayerControls') { "  M static=$($m.IsStatic) $($m.ReturnType.Name) $($m.Name)" } }
"--- GameObject members"
$core = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\UnityEngine.CoreModule.dll")
$go = $core.MainModule.GetType("UnityEngine.GameObject")
foreach ($m in $go.Methods) { if ($m.Name -match 'AddComponent|SetActive|.ctor') { "  M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ',') + ")" } }
foreach ($p in $go.Properties) { if ($p.Name -match 'hideFlags|transform|activeSelf') { "  P $($p.PropertyType.Name) $($p.Name)" } }
"--- Behaviour enabled"
$bh = $core.MainModule.GetType("UnityEngine.Behaviour")
foreach ($p in $bh.Properties) { if ($p.Name -match 'enabled|isActiveAndEnabled') { "  P $($p.PropertyType.Name) $($p.Name)" } }
"--- MonoBehaviour ctor"
$mb = $core.MainModule.GetType("UnityEngine.MonoBehaviour")
foreach ($m in $mb.Methods) { if ($m.IsConstructor) { "  ctor" } }
