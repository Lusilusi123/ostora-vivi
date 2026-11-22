using Mono.Cecil;

// Dumps exact IL2CPP interop API surface for Among Us so the plugin compiles
// against this build instead of guessed member names.
var interop = @"C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\interop";
var core = @"C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\core";
var dotnet = @"C:\Program Files (x86)\Steam\steamapps\common\Among Us\dotnet";

var resolver = new DefaultAssemblyResolver();
foreach (var d in new[] { interop, core, dotnet }) resolver.AddSearchDirectory(d);
var rp = new ReaderParameters { AssemblyResolver = resolver, InMemory = true };

var asm = AssemblyDefinition.ReadAssembly(Path.Combine(interop, "Assembly-CSharp.dll"), rp);

string[] wanted =
{
    "InnerNetClient", "DeadBody", "FollowerCamera", "PlayerTask", "Console", "PlayerControl", "RoleBehaviour", "RoleManager", "RoleTypes", "NetworkedPlayerInfo",
    "PlayerInfo", "HudManager", "ActionButton", "KillButton", "Vent", "ShipStatus",
    "AmongUsClient", "CustomNetworkTransform", "MurderResultFlags", "GameOptionsData", "PlayerTask", "TaskInfo", "InnerNetObject",
    "GameData", "RoleEffect", "MeetingHud", "PlayerPhysics", "Console", "SystemTypes"
};

foreach (var t in asm.MainModule.Types.SelectMany(Flatten))
{
    var name = t.FullName;
    if (!wanted.Any(w => name.Contains(w))) continue;
    Console.WriteLine($"\n=== {name} : {t.BaseType?.FullName} ===");
    foreach (var f in t.Fields.Where(f => !f.IsPrivate))
        Console.WriteLine($"  F {(f.IsStatic ? "static " : "")}{f.FieldType.Name} {f.Name}");
    foreach (var p in t.Properties)
        Console.WriteLine($"  P {p.PropertyType.Name} {p.Name} {(p.GetMethod != null ? "get" : "")}{(p.SetMethod != null ? "/set" : "")}");
    foreach (var m in t.Methods.Where(m => !m.IsGetter && !m.IsSetter && !m.IsPrivate))
        Console.WriteLine($"  M {(m.IsStatic ? "static " : "")}{m.ReturnType.Name} {m.Name}({string.Join(", ", m.Parameters.Select(x => x.ParameterType.Name + " " + x.Name))})");
}

static IEnumerable<TypeDefinition> Flatten(TypeDefinition t)
{
    yield return t;
    foreach (var n in t.NestedTypes.SelectMany(Flatten)) yield return n;
}
