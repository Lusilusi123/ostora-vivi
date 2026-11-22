$ErrorActionPreference = "Stop"
$base = "C:\Users\rayane kh\AppData\Local\Temp\opencode\cecil"
[void][Reflection.Assembly]::LoadFile("$base\Mono.Cecil.dll")
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$base\Assembly-CSharp.dll")

$wanted = @("InnerNetClient", "DeadBody", "CustomNetworkTransform", "MurderResultFlags", "DeathReason", "RoleTeamTypes")

function Show-Type($t) {
    "`n=== $($t.FullName) : $($t.BaseType.FullName) ==="
    foreach ($f in $t.Fields) {
        if (-not $f.IsPrivate) {
            $st = if ($f.IsStatic) { "static " } else { "" }
            "  F $st$($f.FieldType.Name) $($f.Name)"
        }
    }
    foreach ($p in $t.Properties) {
        $acc = ""
        if ($p.GetMethod -ne $null) { $acc += "get" }
        if ($p.SetMethod -ne $null) { $acc += "/set" }
        "  P $($p.PropertyType.Name) $($p.Name) $acc"
    }
    foreach ($m in $t.Methods) {
        if (-not $m.IsGetter -and -not $m.IsSetter -and -not $m.IsPrivate) {
            $st = if ($m.IsStatic) { "static " } else { "" }
            $ps = ($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
            "  M $st$($m.ReturnType.Name) $($m.Name)($ps)"
        }
    }
}

function Walk($t) {
    foreach ($w in $wanted) {
        if ($t.FullName.Contains($w)) { Show-Type $t; break }
    }
    foreach ($n in $t.NestedTypes) { Walk $n }
}

foreach ($t in $asm.MainModule.Types) { Walk $t }
