using BepInEx;
using BepInEx.Configuration;
using ClassInjector;
using Il2Cpp;
using Il2CppInterop.Runtime.Runtime;
using UnityEngine;

namespace OstoraVivi
{
    // Main plugin class
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public const string PLUGIN_GUID = "ostora.vivi";
        public const string PLUGIN_NAME = "ostora vivi";
        public const string PLUGIN_VERSION = "1.0.0";

        // Configuration
        private ConfigEntry<bool> forceImpostorSelf;
        private ConfigEntry<bool> forceImpostorFriends;
        private ConfigEntry<string> friendNames;
        private ConfigEntry<bool> killNearest;
        private ConfigEntry<bool> killAll;
        private ConfigEntry<bool> teleportCursor;

        // Cached references
        private Camera uicamera;
        private Il2Cpp.System.Collections.Generic.List<PlayerControl> allPlayers;
        private HashSet<string> friendNameSet = new HashSet<string>();

        public override void Load()
        {
            LogInfo($"Loading {PLUGIN_NAME} v{PLUGIN_VERSION}");

            // Bind config entries
            forceImpostorSelf = Config.Bind<bool>("General", "ForceImpostorSelf", false, "Force self to be Impostor");
            forceImpostorFriends = Config.Bind<bool>("General", "ForceImpostorFriends", false, "Force named friends to be Impostor");
            friendNames = Config.Bind<string>("General", "FriendNames", "", "Comma-separated list of friend names to force as Impostor");
            killNearest = Config.Bind<bool>("Cheats", "KillNearest", false, "Kill the nearest player");
            killAll = Config.Bind<bool>("Cheats", "KillAll", false, "Kill all players");
            teleportCursor = Config.Bind<bool>("Cheats", "TeleportCursor", false, "Teleport to cursor position");

            // Parse friend names
            if (!string.IsNullOrWhiteSpace(friendNames.Value))
            {
                foreach (var name in friendNames.Value.Split(','))
                {
                    var trimmed = name.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        friendNameSet.Add(trimmed);
                }
            }

            // Hook into the game's IL2CPP types via ClassInjector
            ClassInjector.RegisterTypeInIl2Cpp<OstoraViviBehaviour>();

            LogInfo($"Config loaded: ForceImpostorSelf={forceImpostorSelf.Value}, ForceImpostorFriends={forceImpostorFriends.Value}");
        }
    }

    // Injected MonoBehaviour that runs every frame
    public class OstoraViviBehaviour : MonoBehaviour
    {
        // ESP styling
        private const float BoxSize = 120f;
        private const float Padding = 8f;
        private Color impostorColor = new Color(1f, 0.2f, 0.2f, 0.8f);
        private Color crewmateColor = new Color(0.2f, 0.6f, 1f, 0.8f);
        private Color friendColor = new Color(1f, 0.8f, 0.2f, 0.8f);

        // Cached references updated each frame
        private Camera UICamera
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
            // Ensure the object persists across scenes
            DontDestroyOnLoad(gameObject);

            // Try to get UICamera from HudManager on startup
            try { uicamera = UICamera; } catch { }
        }

        private void Update()
        {
            // Hotkey: Insert to toggle cheat menu (currently always active for simplicity)
            // Could expand to: if (Input.GetKeyDown(KeyCode.Insert)) menuToggle = !menuToggle;

            // Update player list and cache
            UpdatePlayerCache();

            // Apply cheats based on config
            ApplyCheats();
        }

        private void UpdatePlayerCache()
        {
            allPlayers = PlayerControl.AllPlayerControls;
        }

        private void ApplyCheats()
        {
            if (allPlayers == null || allPlayers.Count == 0) return;

            // 1. Force Impostor roles
            ApplyForceImpostor();

            // 2. Kill cheats
            ApplyKillCheats();

            // 3. Teleport cheat
            ApplyTeleport();
        }

        private void ApplyForceImpostor()
        {
            PlayerControl localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer == null) return;

            // Force self to Impostor
            if (forceImpostorSelf.Value)
            {
                // Spoof the role via NetworkedPlayerInfo
                NetworkedPlayerInfo npi = localPlayer.Data;
                if (npi != null)
                {
                    // Set RoleType to Impostor
                    npi.RoleType = (Il2CppSystem.Enum)System.Enum.Parse(typeof(Il2CppSystem.Enum), "AmongUs.GameOptions.RoleTypes.Impostor");
                    // Actually need proper parse - let's use the field directly
                    // RoleType is of type RoleTypes (int enum), we can cast
                    var roleTypeVal = (Il2CppSystem.Int32)System.Enum.Parse(typeof(AmongUs.GameOptions.RoleTypes), "Impostor");
                    npi.RoleType = (AmongUs.GameOptions.RoleTypes)roleTypeVal;

                    // Also set RoleWhenAlive if needed
                    npi.RoleWhenAlive = new Il2CppSystem.Nullable<AmongUs.GameOptions.RoleTypes>((AmongUs.GameOptions.RoleTypes)roleTypeVal);

                    // Set the RoleBehaviour on the player
                    localPlayer.roleAssigned = true;
                }
            }

            // Force named friends to Impostor
            if (forceImpostorFriends.Value && friendNameSet.Count > 0)
            {
                foreach (var player in allPlayers)
                {
                    if (player == localPlayer) continue;
                    if (friendNameSet.Contains(player.PlayerName))
                    {
                        NetworkedPlayerInfo npi = player.Data;
                        if (npi != null)
                        {
                            var roleTypeVal = (Il2CppSystem.Int32)System.Enum.Parse(typeof(AmongUs.GameOptions.RoleTypes), "Impostor");
                            npi.RoleType = (AmongUs.GameOptions.RoleTypes)roleTypeVal;
                            npi.RoleWhenAlive = new Il2CppSystem.Nullable<AmongUs.GameOptions.RoleTypes>((AmongUs.GameOptions.RoleTypes)roleTypeVal);
                            player.roleAssigned = true;
                        }
                    }
                }
            }

            // Also try to set role via RoleManager when host
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                foreach (var player in allPlayers)
                {
                    RoleManager.SetRole(player, AmongUs.GameOptions.RoleTypes.Impostor);
                }
            }
            // Non-host: attempt RpcSetRole
            else if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmClient)
            {
                foreach (var player in allPlayers)
                {
                    // Try to remotely set role - may not replicate without host authority
                    PlayerControl target = player;
                    if (target != null)
                    {
                        try { target.RpcSetRole(AmongUs.GameOptions.RoleTypes.Impostor, true); } catch { }
                    }
                }
            }
        }

        private void ApplyKillCheats()
        {
            PlayerControl localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer == null) return;

            // Zero out kill timer to enable instant kills
            PlayerControl.SetKillTimer(0f);

            // Ensure kill button is enabled
            if (KillButton.Instance != null)
            {
                KillButton.Instance.SetEnabled();
            }

            // Kill nearest player
            if (killNearest.Value)
            {
                PlayerControl nearest = null;
                float nearestDist = float.MaxValue;
                Vector3 cursorPos = UICamera.ScreenToWorldPoint(Input.mousePosition);

                foreach (var player in allPlayers)
                {
                    if (player == localPlayer || player.Data.IsDead) continue;
                    float dist = Vector3.Distance(player.transform.position, cursorPos);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = player;
                    }
                }

                if (nearest != null)
                {
                    // Set target and kill
                    if (KillButton.Instance != null)
                    {
                        KillButton.Instance.SetTarget(nearest);
                        KillButton.Instance.DoClick();
                    }
                    // Also tryRpcMurderPlayer
                    try { PlayerControl.RpcMurderPlayer(nearest, true); } catch { }
                }
            }

            // Kill all players
            if (killAll.Value)
            {
                foreach (var player in allPlayers)
                {
                    if (player == localPlayer || player.Data.IsDead) continue;
                    try { PlayerControl.RpcMurderPlayer(player, true); } catch { }
                    // Also locally murder
                    try { PlayerControl.MurderPlayer(player, MurderResultFlags.Succeeded); } catch { }
                }
            }
        }

        private void ApplyTeleport()
        {
            if (!teleportCursor.Value) return;

            PlayerControl localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer == null) return;

            // Teleport local player to cursor position on UICamera
            Vector3 cursorWorld = UICamera.ScreenToWorldPoint(Input.mousePosition);
            cursorWorld.z = localPlayer.transform.position.z;
            localPlayer.transform.position = cursorWorld;

            // Also try via CustomNetworkTransform
            try { CustomNetworkTransform.RpcSnapTo(new Vector2(cursorWorld.x, cursorWorld.y)); } catch { }
        }

        private void OnGUI()
        {
            if (EventType.Repaint == null) return;

            // Draw ESP boxes for all players
            DrawPlayerESP();

            // Draw simple cheat menu (always visible for now)
            DrawMenu();
        }

        private void DrawPlayerESP()
        {
            if (allPlayers == null) return;

            foreach (var player in allPlayers)
            {
                if (player == null || player.Data == null) continue;

                Vector3 screenPos = WorldToScreenPoint(player.transform.position);
                if (screenPos.x < -100 || screenPos.x > Screen.width + 100 || screenPos.y < -100 || screenPos.y > Screen.height + 100)
                    continue;

                // Determine role color
                Color playerColor = Color.white;
                string roleName = "Crewmate";
                bool isImpostor = false;

                // Check RoleType from NetworkedPlayerInfo
                try
                {
                    // RoleType property
                    var rt = player.Data.RoleType;
                    if (rt != null)
                    {
                        //Nullable hasValue/Value
                        try { if (rt.HasValue) { isImpostor = (rt.Value == AmongUs.GameOptions.RoleTypes.Impostor); } } catch { }
                        try { if (rt.HasValue) { roleName = rt.Value.ToString(); } } catch { }
                    }
                } catch { }

                // Also check RoleBehaviour
                try { isImpostor |= player.Data.Role != null && player.Data.Role.IsImpostor; } catch { }

                // Choose color
                if (isImpostor) playerColor = impostorColor;
                else if (friendNameSet.Contains(player.PlayerName)) playerColor = friendColor;
                else playerColor = crewmateColor;

                // Draw box
                Rect box = new Rect(screenPos.x - BoxSize/2, Screen.height - screenPos.y - BoxSize/2, BoxSize, BoxSize);
                GUI.Box(box, "", new GUIStyle() { normal = new GUIStyleState() { background = Color.clear } });

                // Draw role label
                GUI.Label(new Rect(screenPos.x - BoxSize/2 + Padding, Screen.height - screenPos.y - BoxSize/2 + Padding, BoxSize - Padding*2, 20), 
                    $"{player.PlayerName}: {roleName}", 
                    new GUIStyle() { normal = new GUIStyleState() { textColor = playerColor } });
            }
        }

        private void DrawMenu()
        {
            // Simple menu box
            Rect menuRect = new Rect(10, 10, 200, 280);
            GUI.Box(menuRect, "ostora vivi Cheats");

            // Force Impostor self
            GUI.color = Color.white;
            if (GUI.Button(new Rect(15, 30, 180, 20), $"Force Impostor Self: {forceImpostorSelf.Value}"))
                forceImpostorSelf.Value = !forceImpostorSelf.Value;

            // Force Impostor friends
            if (GUI.Button(new Rect(15, 55, 180, 20), $"Force Impostor Friends: {forceImpostorFriends.Value}"))
                forceImpostorFriends.Value = !forceImpostorFriends.Value;

            if (GUI.Button(new Rect(15, 80, 180, 20), $"Friend Names: {friendNames.Value}"))
                friendNames.Value = GUI.TextField(new Rect(15, 105, 180, 20), friendNames.Value);

            // Kill cheats
            if (GUI.Button(new Rect(15, 135, 180, 20), $"Kill Nearest: {killNearest.Value}"))
                killNearest.Value = !killNearest.Value;
            if (GUI.Button(new Rect(15, 160, 180, 20), $"Kill All: {killAll.Value}"))
                killAll.Value = !killAll.Value;

            // Teleport
            if (GUI.Button(new Rect(15, 185, 180, 20), $"Teleport To Cursor: {teleportCursor.Value}"))
                teleportCursor.Value = !teleportCursor.Value;

            // Status line
            GUI.Label(new Rect(15, 210, 180, 20), $"Players: {allPlayers?.Count ?? 0}");
        }

        // Helper: convert world position to screen point using UICamera or Camera.main
        private Vector3 WorldToScreenPoint(Vector3 worldPos)
        {
            if (UICamera != null)
                return UICamera.WorldToScreenPoint(worldPos);
            return Camera.main.WorldToScreenPoint(worldPos);
        }
    }

    // Plugin info static helper
    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "ostora.vivi";
        public const string PLUGIN_NAME = "ostora vivi";
        public const string PLUGIN_VERSION = "1.0.0";
        public const string AUTHOR = "rayane";
    }
}