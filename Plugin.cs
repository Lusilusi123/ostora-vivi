// language: C#, file: Plugin.cs, target: BepInEx 6 IL2CPP / Among Us (Steam)
// Real API names verified against BepInEx/interop via ApiDump. Config lives in one place
// and is read by the injected MonoBehaviour through the static Instance pointer.
using System;
using System.Collections.Generic;
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

            ForceImpostorSelf = Config.Bind("General", "ForceImpostorSelf", false, "Force local player to Impostor (local visual)");
            ForceImpostorFriends = Config.Bind("General", "ForceImpostorFriends", false, "Force named friends to Impostor (local visual)");
            FriendNames = Config.Bind("General", "FriendNames", "", "Comma-separated friend names");
            KillNearest = Config.Bind("Cheats", "KillNearest", false, "Kill the player nearest to the cursor");
            KillAll = Config.Bind("Cheats", "KillAll", false, "Kill all other players");
            TeleportCursor = Config.Bind("Cheats", "TeleportCursor", false, "Teleport local player to cursor");

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
                var trimmed = name.Trim();
                if (trimmed.Length > 0) FriendNameSet.Add(trimmed);
            }
        }
    }

    public class OstoraViviBehaviour : MonoBehaviour
    {
        private const float BoxSize = 90f;

        private static readonly Color ImpostorColor = new Color(1f, 0.25f, 0.25f, 1f);
        private static readonly Color CrewmateColor = new Color(0.25f, 0.65f, 1f, 1f);
        private static readonly Color FriendColor = new Color(1f, 0.85f, 0.2f, 1f);

        private GUIStyle labelStyle;
        private Camera UiCamera
        {
            get
            {
                if (HudManager.Instance != null && HudManager.Instance.UICamera != null)
                    return HudManager.Instance.UICamera;
                return Camera.main;
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        }

        private bool wasInGame;

        private void Update()
        {
            if (!IsInGame()) { wasInGame = false; return; }

            // New match started — clear the "already assigned" set so roles apply again.
            if (!wasInGame) { AssignedPlayers.Clear(); wasInGame = true; }

            ApplyForceImpostor();
            ApplyKillCheats();
            ApplyTeleport();
        }

        private static bool IsInGame()
        {
            return PlayerControl.LocalPlayer != null && PlayerControl.AllPlayerControls != null;
        }

        private static AmongUs.GameOptions.RoleTypes ImpostorRole => AmongUs.GameOptions.RoleTypes.Impostor;

        // RpcSetRole is the RPC that broadcasts a role change to EVERY client (host included).
        // Sending it ourselves means no host authority is required — each client executes it
        // locally on the target player. This is the same packet the host uses, just sent by us.
        private void ApplyForceImpostor()
        {
            var local = PlayerControl.LocalPlayer;
            if (local == null) return;

            if (Plugin.ForceImpostorSelf != null && Plugin.ForceImpostorSelf.Value && local.Data != null)
                AssignRealImpostor(local);

            if (Plugin.ForceImpostorFriends != null && Plugin.ForceImpostorFriends.Value)
            {
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player == local || player.Data == null) continue;
                    if (Plugin.FriendNameSet.Contains(player.Data.PlayerName))
                        AssignRealImpostor(player);
                }
            }
        }

        // Guarded so the RPC goes out once per player per match.
        private static readonly HashSet<int> AssignedPlayers = new HashSet<int>();

        private static void AssignRealImpostor(PlayerControl player)
        {
            if (player == null || player.Data == null) return;
            if (player.Data.RoleType == ImpostorRole) return;
            if (!AssignedPlayers.Add(player.Data.PlayerId)) return;

            // Broadcast the role change. The host may still re-assert its own state on the next
            // authority tick, but the packet reaches all clients and applies immediately.
            try { player.RpcSetRole(ImpostorRole, true); } catch { }
        }

        private void ApplyKillCheats()
        {
            var local = PlayerControl.LocalPlayer;
            if (local == null) return;

            local.SetKillTimer(0f);

            var killButton = HudManager.Instance != null ? HudManager.Instance.KillButton : null;

            if (Plugin.KillNearest != null && Plugin.KillNearest.Value && killButton != null && UiCamera != null)
            {
                var cursorWorld = UiCamera.ScreenToWorldPoint(Input.mousePosition);
                PlayerControl nearest = null;
                var nearestDist = float.MaxValue;
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player == local || player.Data == null || player.Data.IsDead) continue;
                    var dist = Vector3.Distance(player.transform.position, cursorWorld);
                    if (dist < nearestDist) { nearestDist = dist; nearest = player; }
                }
                if (nearest != null)
                {
                    killButton.SetTarget(nearest);
                    killButton.DoClick();
                    try { local.RpcMurderPlayer(nearest, true); } catch { }
                }
            }

            if (Plugin.KillAll != null && Plugin.KillAll.Value)
            {
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player == local || player.Data == null || player.Data.IsDead) continue;
                    try { local.RpcMurderPlayer(player, true); } catch { }
                    try { local.MurderPlayer(player, MurderResultFlags.Succeeded); } catch { }
                }
            }
        }

        private void ApplyTeleport()
        {
            if (Plugin.TeleportCursor == null || !Plugin.TeleportCursor.Value) return;
            var local = PlayerControl.LocalPlayer;
            if (local == null || UiCamera == null) return;

            var world = UiCamera.ScreenToWorldPoint(Input.mousePosition);
            world.z = local.transform.position.z;
            local.transform.position = world;
            try { local.NetTransform.RpcSnapTo(new Vector2(world.x, world.y)); } catch { }
        }

        private void OnGUI()
        {
            if (Plugin.Instance == null || !IsInGame()) return;
            DrawEsp();
            DrawMenu();
        }

        private void DrawEsp()
        {
            var cam = UiCamera;
            if (cam == null) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null) continue;
                var screen = cam.WorldToScreenPoint(player.transform.position);
                if (screen.z < 0f) continue;
                var x = screen.x - BoxSize / 2f;
                var y = UnityEngine.Screen.height - screen.y - BoxSize / 2f;
                if (x < -200f || x > UnityEngine.Screen.width + 200f) continue;

                var isImpostor = player.Data.RoleType == ImpostorRole;
                var color = isImpostor ? ImpostorColor
                          : Plugin.FriendNameSet.Contains(player.Data.PlayerName) ? FriendColor
                          : CrewmateColor;

                labelStyle.normal.textColor = color;
                GUI.Label(new Rect(x, y - 18f, 200f, 20f), $"{player.Data.PlayerName} [{(isImpostor ? "IMP" : "CREW")}]", labelStyle);
            }
        }

        private void DrawMenu()
        {
            var box = new Rect(10f, 10f, 240f, 210f);
            GUI.Box(box, "ostora vivi");

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

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "ostora.vivi";
        public const string PLUGIN_NAME = "ostora vivi";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}

