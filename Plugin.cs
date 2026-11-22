// language: C#, file: Plugin.cs, target: BepInEx 6 IL2CPP / Among Us (Steam)
// Real role change happens in RoleManager.SelectRoles (game start, before spawn) via Harmony.
// Config + friend selection live here; all game access is reflection over loaded Il2Cpp types.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace OstoraVivi
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public static Plugin Instance;
        internal static ConfigEntry<bool> ForceImpostorSelf;
        internal static ConfigEntry<bool> ForceImpostorFriends;
        internal static ConfigEntry<string> FriendNames;
        internal static ConfigEntry<bool> EspEnabled;
        internal static ConfigEntry<bool> KillNearest;
        internal static ConfigEntry<bool> KillAll;
        internal static ConfigEntry<bool> TeleportCursor;
        internal static readonly HashSet<string> FriendNameSet = new HashSet<string>();

        public override void Load()
        {
            Instance = this;
            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");
            ForceImpostorSelf = Config.Bind("Roles", "ForceImpostorSelf", false, "Make yourself Impostor at game start");
            ForceImpostorFriends = Config.Bind("Roles", "ForceImpostorFriends", false, "Make chosen friends Impostor at game start");
            FriendNames = Config.Bind("Roles", "FriendNames", "", "Comma-separated names of friends to force as Impostor");
            EspEnabled = Config.Bind("Visuals", "ESP", true, "Show role labels above players");
            KillNearest = Config.Bind("Cheats", "KillNearest", false, "Kill player nearest to cursor");
            KillAll = Config.Bind("Cheats", "KillAll", false, "Kill all other players");
            TeleportCursor = Config.Bind("Cheats", "TeleportCursor", false, "Teleport to cursor");
            RebuildFriendSet();
            FriendNames.SettingChanged += (_, __) => RebuildFriendSet();

            var harmony = new Harmony("ostora.vivi");
            harmony.PatchAll();

            ClassInjector.RegisterTypeInIl2Cpp<OstoraViviBehaviour>();
            AddComponent<OstoraViviBehaviour>();
            Log.LogInfo("ostora vivi loaded (Harmony patches active).");
        }

        internal static void RebuildFriendSet()
        {
            FriendNameSet.Clear();
            if (string.IsNullOrWhiteSpace(FriendNames.Value)) return;
            foreach (var name in FriendNames.Value.Split(','))
            {
                var t = name.Trim();
                if (t.Length > 0) FriendNameSet.Add(t);
            }
        }

        internal static void SaveFriends()
        {
            // Build a comma list without string.Join over Il2Cpp HashSet (avoids unstripping).
            string joined = "";
            foreach (var n in FriendNameSet)
            {
                if (joined.Length > 0) joined += ",";
                joined += n;
            }
            FriendNames.Value = joined;
        }

        internal static void LogMsg(string m)
        {
            try { if (Instance != null) Instance.Log.LogMessage("ov: " + m); } catch { }
        }
    }

    // PATCH: runs once at game start, right after the host assigns roles. This is the
    // authoritative moment — before players finish spawning into the map. Setting roles
    // here gives a real Impostor that other clients see, with no lobby RPC spam / kick.
    [HarmonyPatch]
    public static class SelectRolesPatch
    {
        static System.Reflection.MethodInfo TargetMethod()
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
            if (asm == null) return null;
            var t = asm.GetType("RoleManager", false);
            return t?.GetMethod("SelectRoles", BindingFlags.Public | BindingFlags.Instance);
        }

        static void Postfix()
        {
            try
            {
                if (!GameProbe.Ready && !GameProbe.TryInit()) return;

                var local = GameProbe.LocalPlayer();
                if (local == null) return;

                if (Plugin.ForceImpostorSelf != null && Plugin.ForceImpostorSelf.Value)
                    GameProbe.AssignRoleAuthoritative(local);

                if (Plugin.ForceImpostorFriends != null && Plugin.ForceImpostorFriends.Value)
                {
                    foreach (var p in GameProbe.AllPlayers())
                    {
                        if (p == null || p.Equals(local)) continue;
                        var name = GameProbe.PlayerName(p);
                        if (name != null && Plugin.FriendNameSet.Contains(name))
                            GameProbe.AssignRoleAuthoritative(p);
                    }
                }
                Plugin.LogMsg("SelectRoles postfix applied impostor roles.");
            }
            catch (Exception e) { Plugin.LogMsg("SelectRoles patch err: " + e.GetType().Name + " " + e.Message); }
        }
    }

    public class OstoraViviBehaviour : MonoBehaviour
    {
        private const float BoxSize = 90f;
        private static readonly Color ImpostorColor = new Color(1f, 0.25f, 0.25f, 1f);
        private static readonly Color CrewmateColor = new Color(0.25f, 0.65f, 1f, 1f);
        private static readonly Color FriendColor = new Color(1f, 0.85f, 0.2f, 1f);

        private bool menuOpen = true;
        private bool wasInGame;
        private string nameInput = "";
        private bool lastF1;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            GameProbe.TryInit();
        }

        private void Update()
        {
            bool f1 = Input.GetKeyDown(KeyCode.F1);
            if (f1 && !lastF1) menuOpen = !menuOpen;
            lastF1 = f1;

            if (!GameProbe.Ready && !GameProbe.TryInit()) return;
            if (!GameProbe.IsGameStarted()) { wasInGame = false; return; }
            if (!wasInGame) { wasInGame = true; }

            try { ApplyKillCheats(); } catch (Exception e) { LogErr("kill", e); }
            try { ApplyTeleport(); } catch (Exception e) { LogErr("tp", e); }
        }

        private void OnGUI()
        {
            bool inGame = GameProbe.Ready && GameProbe.IsGameStarted();
            if (Plugin.EspEnabled != null && Plugin.EspEnabled.Value && inGame)
            {
                try { DrawEsp(); } catch (Exception e) { LogErr("esp", e); }
            }
            if (menuOpen)
            {
                try { DrawMenu(); } catch (Exception e) { LogErr("menu", e); }
            }
        }

        private static readonly HashSet<string> loggedErrors = new HashSet<string>();
        private static void LogErr(string where, Exception e)
        {
            try
            {
                var key = where + ":" + e.GetType().Name;
                if (Plugin.Instance != null && loggedErrors.Add(key))
                    Plugin.Instance.Log.LogWarning($"ov {where}: {e.GetType().Name}: {e.Message}");
            }
            catch { }
        }

        private void ApplyKillCheats()
        {
            var local = GameProbe.LocalPlayer();
            if (local == null) return;
            GameProbe.ZeroKillTimer(local);

            if (Plugin.KillNearest != null && Plugin.KillNearest.Value)
            {
                var cam = GameProbe.UiCamera();
                if (cam != null)
                {
                    var cursor = cam.ScreenToWorldPoint(Input.mousePosition);
                    PlayerControlLike nearest = null;
                    var best = float.MaxValue;
                    foreach (var p in GameProbe.AllPlayers())
                    {
                        if (p == null || p.Equals(local) || GameProbe.IsDead(p)) continue;
                        var d = Vector3.Distance(GameProbe.Position(p), cursor);
                        if (d < best) { best = d; nearest = p; }
                    }
                    if (nearest != null) GameProbe.Murder(local, nearest.control);
                }
            }

            if (Plugin.KillAll != null && Plugin.KillAll.Value)
            {
                foreach (var p in GameProbe.AllPlayers())
                {
                    if (p == null || p.Equals(local) || GameProbe.IsDead(p)) continue;
                    GameProbe.Murder(local, p.control);
                }
            }
        }

        private void ApplyTeleport()
        {
            if (Plugin.TeleportCursor == null || !Plugin.TeleportCursor.Value) return;
            var local = GameProbe.LocalPlayer();
            var cam = GameProbe.UiCamera();
            if (local == null || cam == null) return;
            var world = cam.ScreenToWorldPoint(Input.mousePosition);
            world.z = GameProbe.Position(local).z;
            GameProbe.SetPosition(local, world);
        }

        private void DrawEsp()
        {
            var cam = GameProbe.UiCamera();
            if (cam == null) return;
            foreach (var p in GameProbe.AllPlayers())
            {
                if (p == null) continue;
                var name = GameProbe.PlayerName(p);
                if (name == null) continue;
                var screen = cam.WorldToScreenPoint(GameProbe.Position(p));
                if (screen.z < 0f) continue;
                var x = screen.x - BoxSize / 2f;
                var y = UnityEngine.Screen.height - screen.y - BoxSize / 2f;
                if (x < -300f || x > UnityEngine.Screen.width + 300f) continue;
                var imp = GameProbe.IsImpostor(p);
                var color = imp ? ImpostorColor
                          : (Plugin.FriendNameSet.Contains(name) ? FriendColor : CrewmateColor);
                GUI.color = color;
                GUI.Label(new Rect(x, y - 18f, 260f, 20f), name + (imp ? " [IMP]" : " [CREW]"));
                GUI.color = Color.white;
            }
        }

        private void DrawMenu()
        {
            GUI.Box(new Rect(10f, 10f, 320f, 500f), "ostora vivi  (F1 hide)");
            float y = 38f;

            GUI.color = Color.white;
            if (GUI.Button(new Rect(20f, y, 300f, 24f), "ESP: " + Plugin.EspEnabled.Value))
                Plugin.EspEnabled.Value = !Plugin.EspEnabled.Value;
            y += 28f;
            if (GUI.Button(new Rect(20f, y, 300f, 24f), "Force Impostor SELF: " + Plugin.ForceImpostorSelf.Value))
                Plugin.ForceImpostorSelf.Value = !Plugin.ForceImpostorSelf.Value;
            y += 28f;
            if (GUI.Button(new Rect(20f, y, 300f, 24f), "Force Impostor FRIENDS: " + Plugin.ForceImpostorFriends.Value))
                Plugin.ForceImpostorFriends.Value = !Plugin.ForceImpostorFriends.Value;
            y += 30f;

            GUI.Label(new Rect(20f, y, 300f, 20f), "Click player to toggle IMP:");
            y += 20f;
            if (GameProbe.Ready)
            {
                var players = GameProbe.AllPlayers();
                int shown = 0;
                foreach (var p in players)
                {
                    if (shown >= 8) break;
                    if (p == null) continue;
                    var nm = GameProbe.PlayerName(p) ?? "?";
                    bool selected = Plugin.FriendNameSet.Contains(nm);
                    if (GUI.Button(new Rect(20f, y, 300f, 22f), (selected ? "[IMP] " : "[    ] ") + nm))
                    {
                        if (selected) Plugin.FriendNameSet.Remove(nm);
                        else Plugin.FriendNameSet.Add(nm);
                        Plugin.SaveFriends();
                    }
                    y += 24f;
                    shown++;
                }
            }
            y += 6f;

            if (GUI.Button(new Rect(20f, y, 300f, 24f), "Kill Nearest: " + Plugin.KillNearest.Value))
                Plugin.KillNearest.Value = !Plugin.KillNearest.Value;
            y += 28f;
            if (GUI.Button(new Rect(20f, y, 300f, 24f), "Kill All: " + Plugin.KillAll.Value))
                Plugin.KillAll.Value = !Plugin.KillAll.Value;
            y += 28f;
            if (GUI.Button(new Rect(20f, y, 300f, 24f), "Teleport To Cursor: " + Plugin.TeleportCursor.Value))
                Plugin.TeleportCursor.Value = !Plugin.TeleportCursor.Value;
        }
    }

    // Reflection over loaded game assemblies — no Assembly-CSharp path load.
    internal sealed class PlayerControlLike
    {
        public object control;
        public object data;
        public string name;
        public bool isImpostor;
        public bool dead;
        public Vector3 pos;
    }

    internal static class GameProbe
    {
        private static bool init;
        private static Assembly asm;
        private static Type tPlayer, tNpi, tHud, tRoleTypes, tRoleMgr, tAmongUsClient;
        private static PropertyInfo pLocal, pAll, pData, pRoleType, pName, pIsDead, pRoleMgrInst, pUICam, pGameState;
        private static MethodInfo mSetRole, mKillTimer, mMurder, mRpcMurder, mSnapTo;
        private static object impostorVal, gameStateStarted;

        internal static bool Ready => init;

        private static void Log(string m) { try { Plugin.LogMsg(m); } catch { } }

        internal static bool TryInit()
        {
            if (init) return true;
            try
            {
                asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                if (asm == null) { Log("no Assembly-CSharp"); return false; }

                tPlayer = asm.GetType("PlayerControl", false);
                tNpi = asm.GetType("NetworkedPlayerInfo", false);
                tHud = asm.GetType("HudManager", false);
                tRoleTypes = asm.GetType("AmongUs.GameOptions.RoleTypes", false);
                tRoleMgr = asm.GetType("RoleManager", false);
                tAmongUsClient = asm.GetType("AmongUsClient", false);
                Log($"types pc={tPlayer != null} npi={tNpi != null} hud={tHud != null} roles={tRoleTypes != null} rm={tRoleMgr != null} auc={tAmongUsClient != null}");
                if (tPlayer == null || tNpi == null || tRoleTypes == null) return false;

                pLocal = tPlayer.GetProperty("LocalPlayer", BindingFlags.Public | BindingFlags.Static);
                pAll = tPlayer.GetProperty("AllPlayerControls", BindingFlags.Public | BindingFlags.Static);
                pData = tPlayer.GetProperty("Data", BindingFlags.Public | BindingFlags.Instance);
                mKillTimer = tPlayer.GetMethod("SetKillTimer", BindingFlags.Public | BindingFlags.Instance);
                mMurder = tPlayer.GetMethod("MurderPlayer", BindingFlags.Public | BindingFlags.Instance);
                mRpcMurder = tPlayer.GetMethod("RpcMurderPlayer", BindingFlags.Public | BindingFlags.Instance);

                pRoleType = tNpi.GetProperty("RoleType", BindingFlags.Public | BindingFlags.Instance);
                pName = tNpi.GetProperty("PlayerName", BindingFlags.Public | BindingFlags.Instance);
                pIsDead = tNpi.GetProperty("IsDead", BindingFlags.Public | BindingFlags.Instance);

                if (tRoleMgr != null)
                {
                    pRoleMgrInst = tRoleMgr.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    mSetRole = tRoleMgr.GetMethod("SetRole", BindingFlags.Public | BindingFlags.Instance);
                }
                if (tHud != null)
                    pUICam = tHud.GetProperty("UICamera", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

                if (tAmongUsClient != null)
                {
                    var gsType = tAmongUsClient.GetProperty("GameState", BindingFlags.Public | BindingFlags.Instance)?.PropertyType;
                    if (gsType != null)
                    {
                        pGameState = tAmongUsClient.GetProperty("GameState", BindingFlags.Public | BindingFlags.Instance);
                        var startedField = gsType.GetField("Started", BindingFlags.Public | BindingFlags.Static);
                        if (startedField != null) gameStateStarted = startedField.GetValue(null);
                    }
                }

                impostorVal = Enum.Parse(tRoleTypes, "Impostor");
                init = true;
                Log("GameProbe ready");
                return true;
            }
            catch (Exception e) { Log("init FAIL " + e.GetType().Name + " " + e.Message); return false; }
        }

        internal static bool IsGameStarted()
        {
            try
            {
                if (pGameState != null && gameStateStarted != null)
                {
                    var auc = asm.GetType("AmongUsClient", false)?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                    if (auc != null)
                    {
                        var st = pGameState.GetValue(auc);
                        return st != null && st.Equals(gameStateStarted);
                    }
                }
                return LocalPlayer() != null;
            }
            catch { return false; }
        }

        internal static object LocalPlayer()
        {
            try { return pLocal?.GetValue(null); } catch { return null; }
        }

        internal static List<PlayerControlLike> AllPlayers()
        {
            var result = new List<PlayerControlLike>();
            try
            {
                var list = pAll?.GetValue(null);
                if (list == null) return result;
                int count = (int)list.GetType().GetProperty("Count").GetValue(list);
                var getItem = list.GetType().GetProperty("Item");
                for (int i = 0; i < count; i++)
                {
                    var pc = getItem.GetValue(list, new object[] { i });
                    if (pc == null) continue;
                    var data = pData?.GetValue(pc);
                    result.Add(new PlayerControlLike
                    {
                        control = pc,
                        data = data,
                        name = data != null ? pName?.GetValue(data) as string : null,
                        isImpostor = data != null && pRoleType?.GetValue(data) != null && pRoleType.GetValue(data).Equals(impostorVal),
                        dead = data != null && pIsDead != null && (bool)pIsDead.GetValue(data),
                        pos = Position(pc)
                    });
                }
            }
            catch (Exception e) { Log("AllPlayers err " + e.Message); }
            return result;
        }

        internal static string PlayerName(PlayerControlLike p) => p?.name;
        internal static bool IsDead(PlayerControlLike p) => p != null && p.dead;
        internal static bool IsImpostor(PlayerControlLike p) => p != null && p.isImpostor;
        internal static Vector3 Position(PlayerControlLike p) => p != null ? p.pos : Vector3.zero;

        internal static void AssignRoleAuthoritative(object playerControl)
        {
            try
            {
                var rm = pRoleMgrInst?.GetValue(null);
                if (rm == null || mSetRole == null) { Log("no RoleManager.Instance"); return; }
                mSetRole.Invoke(rm, new object[] { playerControl, impostorVal });
            }
            catch (Exception e) { Log("AssignRole err " + e.GetType().Name + " " + e.Message); }
        }

        internal static void ZeroKillTimer(object pc) { try { mKillTimer?.Invoke(pc, new object[] { 0f }); } catch { } }

        internal static void Murder(object killer, object target)
        {
            try { mRpcMurder?.Invoke(killer, new object[] { target, true }); } catch { }
        }

        internal static Camera UiCamera()
        {
            try { var c = pUICam?.GetValue(null) as Camera; if (c != null) return c; } catch { }
            return Camera.main;
        }

        internal static Vector3 Position(object pc)
        {
            try
            {
                var tr = pc.GetType().GetProperty("transform").GetValue(pc);
                return (Vector3)tr.GetType().GetProperty("position").GetValue(tr);
            }
            catch { return Vector3.zero; }
        }

        internal static void SetPosition(object pc, Vector3 world)
        {
            try
            {
                var tr = pc.GetType().GetProperty("transform").GetValue(pc);
                tr.GetType().GetProperty("position").SetValue(tr, world);
                var nt = pc.GetType().GetProperty("NetTransform")?.GetValue(pc);
                if (nt != null)
                {
                    mSnapTo = nt.GetType().GetMethod("RpcSnapTo");
                    if (mSnapTo != null) mSnapTo.Invoke(nt, new object[] { new Vector2(world.x, world.y) });
                }
            }
            catch { }
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "ostora.vivi";
        public const string PLUGIN_NAME = "ostora vivi";
        public const string PLUGIN_VERSION = "1.1.0";
    }
}
