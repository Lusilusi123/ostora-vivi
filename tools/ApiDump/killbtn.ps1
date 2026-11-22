$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Assembly-CSharp.dll")
foreach ($t in $asm.MainModule.Types) {
  if ($t.Name -in @('KillButton','VentButton','HudManager','DestroyableSingleton`1','PlayerControl')) {
    "=== $($t.FullName) : $($t.BaseType) ==="
    foreach ($p in $t.Properties) { "  P $($p.PropertyType.Name) $($p.Name)" }
    foreach ($m in $t.Methods) { if ($m.IsPublic -or $m.IsFamily) { "  M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
  }
}
