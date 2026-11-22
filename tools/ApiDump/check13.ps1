$cecil = [System.Reflection.Assembly]::LoadFile("C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil\Mono.Cecil.dll")
$interop = "C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\Assembly-CSharp.dll")
"--- enums"
foreach ($n in @('MurderResultFlags','DeathReason','RoleTeamTypes','RoleTypes','GameStates','PlayerOutfitType')) {
  $asm.MainModule.GetTypes() | Where-Object { $_.Name -eq $n } | ForEach-Object { "TYPE $($_.FullName)"; foreach ($f in $_.Fields) { if ($f.IsStatic -and $f.HasConstant) { "  $($f.Name) = $($f.Constant)" } } }
}
"--- ChatController IsOpen"
$cc = $asm.MainModule.GetType("ChatController")
foreach ($p in $cc.Properties) { if ($p.Name -match 'IsOpen|isOpen|Open') { "P $($p.PropertyType.Name) $($p.Name)" } }
"--- Time"
$core = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\UnityEngine.CoreModule.dll")
$t = $core.MainModule.GetType("UnityEngine.Time")
foreach ($p in $t.Properties) { if ($p.Name -match 'deltaTime|time|unscaledTime') { "P static=$($p.GetMethod.IsStatic) $($p.PropertyType.Name) $($p.Name)" } }
"--- GUI color + slider"
$imgui = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$interop\UnityEngine.IMGUIModule.dll")
$g = $imgui.MainModule.GetType("UnityEngine.GUI")
foreach ($p in $g.Properties) { if ($p.Name -match 'color|backgroundColor|contentColor') { "P static=$($p.GetMethod.IsStatic) $($p.PropertyType.Name) $($p.Name)" } }
foreach ($m in $g.Methods) { if ($m.IsPublic -and $m.Name -match 'HorizontalSlider|DrawTexture|Toggle|Button') { "M static=$($m.IsStatic) $($m.ReturnType.Name) $($m.Name)(" + (($m.Parameters | ForEach-Object { "$($_.ParameterType.Name)" }) -join ',') + ")" } }
