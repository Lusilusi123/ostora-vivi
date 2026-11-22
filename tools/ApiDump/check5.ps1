$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\UnityEngine.InputLegacyModule.dll")
$t = $asm.MainModule.GetType("UnityEngine.Input")
"=== Input ==="
foreach ($m in $t.Methods) { if ($m.IsPublic -and $m.Name -match 'GetKeyDown|GetKeyUp|GetKey|mousePosition') { "  M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
$asm2 = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\UnityEngine.IMGUIModule.dll")
foreach ($tn in @("UnityEngine.GUIStyle","UnityEngine.GUI/WindowFunction","UnityEngine.GUIContent")) {
  $t2 = $asm2.MainModule.GetType($tn)
  if (-not $t2) { "NOT FOUND $tn"; continue }
  "=== $($t2.FullName) ==="
  foreach ($m in $t2.Methods) { if ($m.IsPublic -and $m.Name -match '\.ctor|Invoke') { "  M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
  foreach ($p in $t2.Properties) { if ($p.Name -match 'fontSize|alignment|textColor|normal|fontStyle|wordWrap|richText') { "  P $($p.PropertyType.Name) $($p.Name)" } }
}
