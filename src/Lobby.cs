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
        [InVarName("lobby_join_delay", User = true)]
        public class LobbyJoinInVar : InVar {
            static float Value = 5f;
            public override string Get(NetworkUser user) {
                return Value.ToString();
            }
            public override void Set(NetworkUser user, string value) {
                Value = float.Parse(value);
            }
        }

        [InVarName("lobby_start_delay", User = true)]
        public class LobbyStartInVar : InVar {
            static float Value = 5f;
            public override string Get(NetworkUser user) {
                return Value.ToString();
            }
            public override void Set(NetworkUser user, string value) {
                Value = float.Parse(value);
            }
        }

        [InVarName("lobby_host_min", User = true)]
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
    }
}
