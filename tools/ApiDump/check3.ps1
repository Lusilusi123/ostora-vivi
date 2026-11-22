$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$core = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\core"
foreach ($f in @("BepInEx.Core.dll","BepInEx.Unity.IL2CPP.dll","Il2CppInterop.Runtime.dll","0Harmony.dll")) {
  $asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$core\$f")
  $tfm = $asm.CustomAttributes | Where-Object { $_.AttributeType.Name -eq 'TargetFrameworkAttribute' } | Select-Object -First 1
  "$f -> " + ($(if ($tfm) { $tfm.ConstructorArguments[0].Value } else { 'unknown' }))
}
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
foreach ($pair in @(@("Assembly-CSharp.dll","Vent"), @("Assembly-CSharp.dll","AmongUs.GameOptions.RoleTeamTypes"), @("UnityEngine.IMGUIModule.dll","UnityEngine.GUI"), @("UnityEngine.CoreModule.dll","UnityEngine.Texture2D"), @("UnityEngine.CoreModule.dll","UnityEngine.Input"), @("UnityEngine.CoreModule.dll","UnityEngine.Screen"), @("UnityEngine.CoreModule.dll","UnityEngine.Time"), @("UnityEngine.CoreModule.dll","UnityEngine.Mathf"))) {
  $asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\$($pair[0])")
  $t = $asm.MainModule.GetType($pair[1])
  if (-not $t) { "NOT FOUND $($pair[1])"; continue }
  "=== $($t.FullName) : $($t.BaseType) ==="
  if ($t.IsEnum) { foreach ($f2 in $t.Fields) { if ($f2.IsStatic) { "  F $($f2.Name)" } } ; continue }
  foreach ($p in $t.Properties) { if ($p.Name -match 'whiteTexture|width|height|mousePosition|time|deltaTime|skin|color') { "  P $($p.PropertyType.Name) $($p.Name)" } }
  foreach ($m in $t.Methods) { if ($m.IsPublic -and $m.Name -match 'Box|Label|Button|Toggle|TextField|Window|DrawTexture|GetKeyDown|GetKey|WorldToScreen|Clamp|RoundToInt') { "  M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
}
