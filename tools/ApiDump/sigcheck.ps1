$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
function Dump($path, $typeName, $filter) {
  $asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($path)
  $t = $asm.MainModule.GetType($typeName)
  if (-not $t) { "NOT FOUND $typeName in $path"; return }
  "=== $($t.FullName) ==="
  foreach ($p in $t.Properties) { if ($p.Name -match $filter) { "  P $($p.PropertyType.Name) $($p.Name)" } }
  foreach ($m in $t.Methods) { if ($m.Name -match $filter) { "  M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
}
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
Dump "$interop\UnityEngine.CoreModule.dll" "UnityEngine.Object" 'FindObjectsOfType|DontDestroyOnLoad|Destroy'
Dump "$interop\UnityEngine.CoreModule.dll" "UnityEngine.Camera" 'main|WorldToScreenPoint|ScreenToWorldPoint|orthographic'
Dump "$interop\UnityEngine.CoreModule.dll" "UnityEngine.Texture2D" 'whiteTexture'
Dump "$interop\UnityEngine.IMGUIModule.dll" "UnityEngine.GUI" 'Button|Box|Label|DrawTexture|color'
Dump "$interop\UnityEngine.InputLegacyModule.dll" "UnityEngine.Input" 'GetKeyDown|mousePosition'
