using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoR2.Mods;
using RoR2;
using UnityEngine;
using Harmony;

namespace InfinityMod {
    class LobbyExtension : InfinityExtension {
        [InCmd("lobby_join_delay", User = true)]
        public class LobbyJoinInVar : InVar {
            public static float Value = 5f;
            public override string Get(NetworkUser user) {
                return Value.ToString();
            }
            public override void Set(NetworkUser user, string value) {
                Value = float.Parse(value);
            }
        }

        [InCmd("lobby_start_delay", User = true)]
        public class LobbyStartInVar : InVar {
            public static float Value = 20f;
            public override string Get(NetworkUser user) {
                return Value.ToString();
            }
            public override void Set(NetworkUser user, string value) {
                Value = float.Parse(value);
            }
        }

        [InCmd("lobby_host_min", User = true)]
        public class ChestUnlockInVar : InVar {
            public override string Get(NetworkUser user) {
                var minimumPlayerCount = new Traverse(typeof(SteamworksLobbyManager)).Field<int>("minimumPlayerCount");
                return minimumPlayerCount.Value.ToString();
            }
            public override void Set(NetworkUser user, string value) {
                var minimumPlayerCount = new Traverse(typeof(SteamworksLobbyManager)).Field<int>("minimumPlayerCount");
                minimumPlayerCount.Value = int.Parse(value);
            }
        }
        
        [HarmonyPatch(typeof(RoR2.Networking.SteamLobbyFinder))]
        [HarmonyPatch("Awake")]
        static class SteamLobbyFinder_Awake_Patch {
            static void Postfix(RoR2.Networking.SteamLobbyFinder __instance) {
                __instance.joinOnlyDuration = LobbyJoinInVar.Value;
                __instance.waitForFullDuration = LobbyStartInVar.Value;
            }
        }
    }
}
