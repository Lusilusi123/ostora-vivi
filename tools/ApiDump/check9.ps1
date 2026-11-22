$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\Assembly-CSharp.dll")
$rm = $asm.MainModule.GetType("RoleManager")
foreach ($m in $rm.Methods) { if ($m.Name -match 'IsImpostorRole|GetRole|SetRole') { "M static=$($m.IsStatic) $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
$ds = $asm.MainModule.GetType("DestroyableSingleton`1")
if ($ds) { foreach ($p in $ds.Properties) { "DS P static=$($p.GetMethod.IsStatic) $($p.PropertyType.Name) $($p.Name)" } }
$inc = $asm.MainModule.GetType("InnerNet.InnerNetClient")
foreach ($p in $inc.Properties) { if ($p.Name -eq 'Instance') { "InnerNetClient.Instance static=$($p.GetMethod.IsStatic) type=$($p.PropertyType.FullName)" } }
$pc = $asm.MainModule.GetType("PlayerControl")
foreach ($p in $pc.Properties) { if ($p.Name -match '^(LocalPlayer|AllPlayerControls|Data|NetTransform|PlayerId)$') { "PC P static=$($p.GetMethod.IsStatic) $($p.PropertyType.Name) $($p.Name)" } }
$rb = $asm.MainModule.GetType("RoleBehaviour")
foreach ($p in $rb.Properties) { if ($p.Name -match '^(IsImpostor|NiceName|Role|TeamType|CanUseKillButton|CanVent)$') { "RB P $($p.PropertyType.Name) $($p.Name)" } }
$np = $asm.MainModule.GetType("NetworkedPlayerInfo")
foreach ($p in $np.Properties) { if ($p.Name -match '^(RoleType|Role|RoleWhenAlive|IsDead|PlayerName|PlayerId|Object)$') { "NP P $($p.PropertyType.Name) $($p.Name) set=$($p.SetMethod -ne $null)" } }
