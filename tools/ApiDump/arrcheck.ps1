$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$core = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\core"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$core\Il2CppInterop.Runtime.dll")
foreach ($t in $asm.MainModule.Types) {
  if ($t.Name -match '^Il2CppArrayBase|^Il2CppReferenceArray') {
    "=== $($t.FullName) ==="
    foreach ($p in $t.Properties) { "  P $($p.PropertyType.Name) $($p.Name)" }
    foreach ($m in $t.Methods) { if ($m.IsPublic) { "  M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
  }
}
