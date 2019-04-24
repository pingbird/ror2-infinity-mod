using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoR2.Mods;
using RoR2;
using UnityEngine;
using Harmony;
using UnityEngine.Networking;

namespace InfinityMod {
    class ChestsExtension : InfinityExtension {
        [InCmd("chest_unlock")]
        public class ChestUnlockInVar : InVar {
            public static bool Unlock = false;

            public override string Get(NetworkUser user) {
                return Unlock ? "1" : "0";
            }

            public override void Set(NetworkUser user, string value) {
                Unlock = int.Parse(value) != 0;
            }
        }

        [InCmd("chest_stacks")]
        public class ChestStacksInVar : InVar {
            public static int Stacks = 1;
            public override string Get(NetworkUser user) {
                return Stacks.ToString();
            }

            public override void Set(NetworkUser user, string value) {
                Stacks = int.Parse(value);
            }
        }

        [HarmonyPatch(typeof(ChestBehavior))]
        [HarmonyPatch("ItemDrop")]
        static class ChestBehavior_ItemDrop_Patch {
            static bool Prefix(ChestBehavior __instance) {
                if (!NetworkServer.active) {
                    Debug.LogWarning("[Server] function 'System.Void RoR2.ChestBehavior::ItemDrop()' called on client");
                    return false;
                }

                var dropPickup = new Traverse(__instance).Field<PickupIndex>("dropPickup");

                if (dropPickup.Value == PickupIndex.none) {
                    return false;
                }

                for (int i = 0; i < ChestStacksInVar.Stacks; i++) {
                    PickupDropletController.CreatePickupDroplet(dropPickup.Value, __instance.transform.position + Vector3.up * 1.5f, Vector3.up * 20f + __instance.transform.forward * 2f);
                }

                dropPickup.Value = PickupIndex.none;

                return false;
            }
        }
    }
}
