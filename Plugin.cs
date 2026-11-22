// language: C#, file: Plugin.cs, target: BepInEx 6 IL2CPP / Among Us (Steam)
// Uses reflection over Il2Cpp types so no static Assembly-CSharp reference is needed at runtime.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
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
        internal static ConfigEntry<bool> KillNearest;
        internal static ConfigEntry<bool> KillAll;
        internal static ConfigEntry<bool> TeleportCursor;
        internal static readonly HashSet<string> FriendNameSet = new HashSet<string>();

        public override void Load()
        {
            Instance = this;
            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");
            ForceImpostorSelf = Config.Bind("General", "ForceImpostorSelf", false, "Force local player to Impostor");
            ForceImpostorFriends = Config.Bind("General", "ForceImpostorFriends", false, "Force named friends to Impostor");
            FriendNames = Config.Bind("General", "FriendNames", "", "Comma-separated friend names");
            KillNearest = Config.Bind("Cheats", "KillNearest", false, "Kill nearest player to cursor");
            KillAll = Config.Bind("Cheats", "KillAll", false, "Kill all players");
            TeleportCursor = Config.Bind("Cheats", "TeleportCursor", false, "Teleport to cursor");
            RebuildFriendSet();
            FriendNames.SettingChanged += (_, __) => RebuildFriendSet();
            ClassInjector.RegisterTypeInIl2Cpp<OstoraViviBehaviour>();
            AddComponent<OstoraViviBehaviour>();
            Log.LogInfo("ostora vivi loaded.");
        }

        private static void RebuildFriendSet()
        {
            FriendNameSet.Clear();
            if (string.IsNullOrWhiteSpace(FriendNames.Value)) return;
            foreach (var name in FriendNames.Value.Split(','))
            {
                var t = name.Trim();
                if (t.Length > 0) FriendNameSet.Add(t);
            }
        }
    }

    // All game access goes through Il2Cpp reflection so the CLR never has to resolve
    // Assembly-CSharp by name (Smart App Control blocks that path load in this setup).
    public class OstoraViviBehaviour : MonoBehaviour
    {
        private const float BoxSize = 90f;
        private static readonly Color ImpostorColor = new Color(1f, 0.25f, 0.25f, 1f);
        private static readonly Color CrewmateColor = new Color(0.25f, 0.65f, 1f, 1f);
        private static readonly Color FriendColor = new Color(1f, 0.85f, 0.2f, 1f);
        private GUIStyle labelStyle;
        private bool wasInGame;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            GameProbe.TryInit();
        }

        private void Update()
        {
            if (!GameProbe.TryInit()) { wasInGame = false; return; }
            if (!GameProbe.InGame()) { wasInGame = false; return; }
            if (!wasInGame) { GameProbe.Reset(); wasInGame = true; }

            try { ApplyForceImpostor(); } catch (Exception e) { LogErr("force", e); }
            try { ApplyKillCheats(); } catch (Exception e) { LogErr("kill", e); }
            try { ApplyTeleport(); } catch (Exception e) { LogErr("tp", e); }
        }

        private void OnGUI()
        {
            if (labelStyle == null)
                labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            if (!GameProbe.Ready || !GameProbe.InGame()) return;
            try { DrawEsp(); } catch (Exception e) { LogErr("esp", e); }
            try { DrawMenu(); } catch (Exception e) { LogErr("menu", e); }
        }

        private static void LogErr(string where, Exception e)
        {
            if (Plugin.Instance != null)
                Plugin.Instance.Log.LogWarning($"ov {where}: {e.GetType().Name}: {e.Message}");
        }

        private void ApplyForceImpostor()
        {
            if ((Plugin.ForceImpostorSelf == null || !Plugin.ForceImpostorSelf.Value) &&
                (Plugin.ForceImpostorFriends == null || !Plugin.ForceImpostorFriends.Value)) return;

            var local = GameProbe.LocalPlayer();
            if (local == null) return;

            if (Plugin.ForceImpostorSelf != null && Plugin.ForceImpostorSelf.Value)
                GameProbe.SetImpostor(local);

            if (Plugin.ForceImpostorFriends != null && Plugin.ForceImpostorFriends.Value)
            {
                foreach (var p in GameProbe.AllPlayers())
                {
                    if (p == null || p.Equals(local)) continue;
                    var name = GameProbe.PlayerName(p);
                    if (name != null && Plugin.FriendNameSet.Contains(name))
                        GameProbe.SetImpostor(p);
                }
            }
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
                    object nearest = null;
                    var best = float.MaxValue;
                    foreach (var p in GameProbe.AllPlayers())
                    {
                        if (p == null || p.Equals(local) || GameProbe.IsDead(p)) continue;
                        var d = Vector3.Distance(GameProbe.Position(p), cursor);
                        if (d < best) { best = d; nearest = p; }
                    }
                    if (nearest != null) GameProbe.Murder(local, nearest);
                }
            }

            if (Plugin.KillAll != null && Plugin.KillAll.Value)
            {
                foreach (var p in GameProbe.AllPlayers())
                {
                    if (p == null || p.Equals(local) || GameProbe.IsDead(p)) continue;
                    GameProbe.Murder(local, p);
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
                var imp = GameProbe.IsImpostor(p);
                var color = imp ? ImpostorColor : (Plugin.FriendNameSet.Contains(name) ? FriendColor : CrewmateColor);
                labelStyle.normal.textColor = color;
                GUI.Label(new Rect(x, y - 18f, 240f, 20f), $"{name} [{(imp ? "IMP" : "CREW")}]", labelStyle);
            }
        }

        private void DrawMenu()
        {
            GUI.Box(new Rect(10f, 10f, 240f, 210f), "ostora vivi");
            GUI.color = Color.white;
            if (GUI.Button(new Rect(20f, 35f, 220f, 22f), $"Force Impostor Self: {Plugin.ForceImpostorSelf.Value}"))
                Plugin.ForceImpostorSelf.Value = !Plugin.ForceImpostorSelf.Value;
            if (GUI.Button(new Rect(20f, 62f, 220f, 22f), $"Force Impostor Friends: {Plugin.ForceImpostorFriends.Value}"))
                Plugin.ForceImpostorFriends.Value = !Plugin.ForceImpostorFriends.Value;
            Plugin.FriendNames.Value = GUI.TextField(new Rect(20f, 90f, 220f, 22f), Plugin.FriendNames.Value);
            if (GUI.Button(new Rect(20f, 118f, 220f, 22f), $"Kill Nearest: {Plugin.KillNearest.Value}"))
                Plugin.KillNearest.Value = !Plugin.KillNearest.Value;
            if (GUI.Button(new Rect(20f, 145f, 220f, 22f), $"Kill All: {Plugin.KillAll.Value}"))
                Plugin.KillAll.Value = !Plugin.KillAll.Value;
            if (GUI.Button(new Rect(20f, 172f, 220f, 22f), $"Teleport To Cursor: {Plugin.TeleportCursor.Value}"))
                Plugin.TeleportCursor.Value = !Plugin.TeleportCursor.Value;
        }
    }

    // Reflection cache over Il2Cpp types. Types are found by name via the already-loaded
    // game assemblies (no Assembly.Load by name, no path load).
    internal static class GameProbe
    {
        private static bool init;
        private static System.Type tPlayerControl, tNetworkedInfo, tHudManager, tRoleTypes, tRoleMgr;
        private static PropertyInfo pLocal, pAll, pData, pRoleType, pName, pIsDead, pRoleMgrInst, pUICam;
        private static MethodInfo mRpcSetRole, mSetKillTimer, mMurder, mRpcMurder, mWorldToScreen, mScreenToWorld;

        internal static bool Ready => init;
        private static object ImpostorVal;

        private static void Log(string msg)
        {
            try { if (Plugin.Instance != null) Plugin.Instance.Log.LogMessage("ov: " + msg); } catch { }
        }

        internal static bool TryInit()
        {
            if (init) return true;
            try
            {
                Log("TryInit start");
                // Find loaded assemblies whose names we need, without Assembly.Load.
                Assembly asm = null;
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (a.GetName().Name == "Assembly-CSharp") { asm = a; break; }
                }
                if (asm == null)
                {
                    Log("no Assembly-CSharp in loaded assemblies");
                    return false;
                }

                tPlayerControl = asm.GetType("PlayerControl", false);
                tNetworkedInfo = asm.GetType("NetworkedPlayerInfo", false);
                tHudManager = asm.GetType("HudManager", false);
                tRoleTypes = asm.GetType("AmongUs.GameOptions.RoleTypes", false);
                tRoleMgr = asm.GetType("RoleManager", false);
                Log($"types: pc={tPlayerControl != null} npi={tNetworkedInfo != null} hud={tHudManager != null} roles={tRoleTypes != null}");
                if (tPlayerControl == null || tNetworkedInfo == null) return false;

                pLocal = tPlayerControl.GetProperty("LocalPlayer", BindingFlags.Public | BindingFlags.Static);
                pAll = tPlayerControl.GetProperty("AllPlayerControls", BindingFlags.Public | BindingFlags.Static);
                pData = tPlayerControl.GetProperty("Data", BindingFlags.Public | BindingFlags.Instance);
                pName = tNetworkedInfo.GetProperty("PlayerName", BindingFlags.Public | BindingFlags.Instance);
                pRoleType = tNetworkedInfo.GetProperty("RoleType", BindingFlags.Public | BindingFlags.Instance);
                pIsDead = tNetworkedInfo.GetProperty("IsDead", BindingFlags.Public | BindingFlags.Instance);
                mRpcSetRole = tPlayerControl.GetMethod("RpcSetRole", BindingFlags.Public | BindingFlags.Instance);
                mSetKillTimer = tPlayerControl.GetMethod("SetKillTimer", BindingFlags.Public | BindingFlags.Instance);
                mMurder = tPlayerControl.GetMethod("MurderPlayer", BindingFlags.Public | BindingFlags.Instance);
                mRpcMurder = tPlayerControl.GetMethod("RpcMurderPlayer", BindingFlags.Public | BindingFlags.Instance);

                if (tRoleTypes != null)
                    ImpostorVal = Enum.Parse(tRoleTypes, "Impostor");

                if (tHudManager != null)
                    pUICam = tHudManager.GetProperty("UICamera", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

                if (tRoleMgr != null)
                    pRoleMgrInst = tRoleMgr.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

                var mainCam = UnityEngine.Camera.main;
                if (mainCam != null)
                {
                    var tCamera = mainCam.GetType();
                    mWorldToScreen = tCamera.GetMethod("WorldToScreenPoint");
                    mScreenToWorld = tCamera.GetMethod("ScreenToWorldPoint");
                }

                init = true;
                try { if (Plugin.Instance != null) Plugin.Instance.Log.LogMessage($"ov: game types bound (players OK)"); } catch { }
                return true;
            }
            catch (Exception e)
            {
                try { if (Plugin.Instance != null) Plugin.Instance.Log.LogWarning($"GameProbe init FAIL: {e.GetType().Name}: {e.Message}"); } catch { }
                return false;
            }
        }

        internal static bool InGame() => LocalPlayer() != null;

        internal static void Reset() { }

        internal static object LocalPlayer()
        {
            try { return pLocal?.GetValue(null); } catch { return null; }
        }

        internal static IEnumerable<object> AllPlayers()
        {
            try
            {
                var list = pAll?.GetValue(null);
                if (list == null) return Array.Empty<object>();
                var countProp = list.GetType().GetProperty("Count");
                int count = countProp != null ? (int)countProp.GetValue(list) : 0;
                var result = new List<object>(count);
                var getItem = list.GetType().GetProperty("Item");
                for (int i = 0; i < count; i++)
                    result.Add(getItem.GetValue(list, new object[] { i }));
                return result;
            }
            catch { return Array.Empty<object>(); }
        }

        internal static string PlayerName(object p)
        {
            try
            {
                var data = pData?.GetValue(p);
                if (data == null) return null;
                return pName?.GetValue(data) as string;
            }
            catch { return null; }
        }

        internal static bool IsDead(object p)
        {
            try { var data = pData?.GetValue(p); return data != null && (bool)pIsDead.GetValue(data); }
            catch { return false; }
        }

        internal static bool IsImpostor(object p)
        {
            try
            {
                var data = pData?.GetValue(p);
                if (data == null || pRoleType == null || ImpostorVal == null) return false;
                var v = pRoleType.GetValue(data);
                return v != null && v.Equals(ImpostorVal);
            }
            catch { return false; }
        }

        internal static void SetImpostor(object p)
        {
            try { mRpcSetRole?.Invoke(p, new object[] { ImpostorVal, true }); }
            catch (Exception e) { try { Plugin.Instance.Log.LogWarning($"rpcsetrole: {e.Message}"); } catch { } }
        }

        internal static void ZeroKillTimer(object p)
        {
            try { mSetKillTimer?.Invoke(p, new object[] { 0f }); } catch { }
        }

        internal static void Murder(object killer, object target)
        {
            try { mRpcMurder?.Invoke(killer, new object[] { target, true }); } catch { }
        }

        internal static Camera UiCamera()
        {
            try
            {
                if (pUICam != null)
                {
                    var c = pUICam.GetValue(null) as Camera;
                    if (c != null) return c;
                }
                return Camera.main;
            }
            catch { return Camera.main; }
        }

        internal static Vector3 Position(object p)
        {
            try
            {
                var tr = p.GetType().GetProperty("transform").GetValue(p);
                var t = tr.GetType();
                var pos = t.GetProperty("position").GetValue(tr);
                return (Vector3)pos;
            }
            catch { return Vector3.zero; }
        }

        internal static void SetPosition(object p, Vector3 world)
        {
            try
            {
                var tr = p.GetType().GetProperty("transform").GetValue(p);
                tr.GetType().GetProperty("position").SetValue(tr, world);
            }
            catch { }
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "ostora.vivi";
        public const string PLUGIN_NAME = "ostora vivi";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}
