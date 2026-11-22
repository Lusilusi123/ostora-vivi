$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
foreach ($pair in @(@("UnityEngine.CoreModule.dll","UnityEngine.GameObject"), @("UnityEngine.CoreModule.dll","UnityEngine.Component"), @("UnityEngine.CoreModule.dll","UnityEngine.Transform"), @("UnityEngine.CoreModule.dll","UnityEngine.Vector3"), @("UnityEngine.CoreModule.dll","UnityEngine.Color"))) {
  $asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\$($pair[0])")
  $t = $asm.MainModule.GetType($pair[1])
  if (-not $t) { "NOT FOUND $($pair[1])"; continue }
  "=== $($t.FullName) ==="
  foreach ($m in $t.Methods) { if ($m.IsPublic -and $m.Name -match 'AddComponent|GetComponent|get_transform|get_position|WorldToScreen|DontDestroy|get_main') { "  M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
  foreach ($p in $t.Properties) { if ($p.Name -match 'transform|position|main') { "  P $($p.PropertyType.Name) $($p.Name)" } }
}
