$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$core = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\core"
foreach ($f in @("BepInEx.Core.dll","BepInEx.Unity.IL2CPP.dll","Il2CppInterop.Runtime.dll")) {
  "##### $f"
  $asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$core\$f")
  foreach ($t in $asm.MainModule.Types) {
    if ($t.Name -match 'BasePlugin|ClassInjector|IL2CPPChainloader|ManualLogSource|ConfigFile|ConfigEntry|Il2CppType|Il2CppObjectBase') {
      "=== $($t.FullName) : $($t.BaseType) ==="
      foreach ($m in $t.Methods) { if ($m.IsPublic) { "  M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
    }
  }
}
