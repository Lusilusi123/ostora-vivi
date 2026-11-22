$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
$ms = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\Il2Cppmscorlib.dll")
"--- Nullable type"
$nt = $ms.MainModule.GetTypes() | Where-Object { $_.Name -eq 'Nullable`1' } | Select-Object -First 1
if ($nt) { "type: $($nt.FullName)"; foreach ($m in $nt.Methods) { if ($m.IsPublic) { "M static=$($m.IsStatic) $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name)" }) -join ',') + ")" } }; foreach ($f in $nt.Fields) { "F $($f.FieldType.Name) $($f.Name)" } }
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\Assembly-CSharp.dll")
$np = $asm.MainModule.GetType("NetworkedPlayerInfo")
foreach ($p in $np.Properties) { if ($p.Name -match 'Color|DefaultOutfit|Outfits|CurrentOutfit') { "NP P $($p.PropertyType.FullName) $($p.Name)" } }
"--- Cursor in UnityEngine.CoreModule"
$core = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\UnityEngine.CoreModule.dll")
$cur = $core.MainModule.GetType("UnityEngine.Cursor")
if ($cur) { foreach ($p in $cur.Properties) { "P static=$($p.GetMethod.IsStatic) $($p.PropertyType.Name) $($p.Name)" } }
"--- FindObjectsOfType generic on UnityEngine.Object"
$uo = $core.MainModule.GetType("UnityEngine.Object")
foreach ($m in $uo.Methods) { if ($m.IsPublic -and $m.Name -match '^FindObjectsOfType') { "M static=$($m.IsStatic) $($m.ReturnType.FullName) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name)" }) -join ',') + ") gen=" + (($m.GenericParameters | ForEach-Object { $_.Name }) -join ',') } }
"--- Screen"
$scr = $core.MainModule.GetType("UnityEngine.Screen")
if ($scr) { foreach ($p in $scr.Properties) { if ($p.Name -match 'width|height') { "P static=$($p.GetMethod.IsStatic) $($p.PropertyType.Name) $($p.Name)" } } }
"--- Mathf"
$mf = $core.MainModule.GetType("UnityEngine.Mathf")
if ($mf) { foreach ($m in $mf.Methods) { if ($m.IsPublic -and $m.Name -match '^(Clamp|Lerp|Abs|Sqrt|Min|Max)$') { "M static=$($m.IsStatic) $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name)" }) -join ',') + ")" } } }
