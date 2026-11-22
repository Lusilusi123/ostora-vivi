$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\UnityEngine.IMGUIModule.dll")
$t = $asm.MainModule.GetType("UnityEngine.GUI")
foreach ($m in $t.Methods) { if ($m.IsPublic -and $m.Name -match '^DrawTexture$|^Button$|^Toggle$|^Box$|^Label$|^BeginGroup$|^EndGroup$') { "M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
$asm2 = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\UnityEngine.CoreModule.dll")
foreach ($tn in @("UnityEngine.Texture2D","UnityEngine.Texture","UnityEngine.Screen","UnityEngine.GUIStyleState","UnityEngine.Rect")) {
  $t2 = $asm2.MainModule.GetType($tn)
  if (-not $t2) { "NOT FOUND $tn"; continue }
  "=== $($t2.FullName) ==="
  foreach ($p in $t2.Properties) { if ($p.Name -match 'whiteTexture|width|height|textColor|background|^x$|^y$|^width$|^height$') { "  P $($p.PropertyType.Name) $($p.Name)" } }
  foreach ($m in $t2.Methods) { if ($m.IsPublic -and $m.Name -match '\.ctor|SetPixel|Apply') { "  M $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ') + ")" } }
}
