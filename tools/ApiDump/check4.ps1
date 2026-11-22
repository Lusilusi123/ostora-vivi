$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\Il2Cppmscorlib.dll")
foreach ($t in $asm.MainModule.Types) {
  if ($t.FullName -match 'Nullable|Il2CppSystem.Collections.Generic.List`1') { "=== $($t.FullName) ===" ; foreach ($p in $t.Properties) { "  P $($p.PropertyType.Name) $($p.Name)" }; foreach ($m in $t.Methods) { if ($m.IsPublic) { "  M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } } }
}
