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
    class LevelExtension : InfinityExtension {
        [InVarName("level_intr_stacks", Perms = PermFlags.Admin)]
        public class LevelIntrInVar : InVar {
            public static int Stacks = 1;
            public override string Get(NetworkUser user) {
                return Stacks.ToString();
            }

            public override void Set(NetworkUser user, string value) {
                Stacks = int.Parse(value);
            }
        }

        [InVarName("level_monster_stacks", Perms = PermFlags.Admin)]
        public class LevelMonsterInVar : InVar {
            public static int Stacks = 1;
            public override string Get(NetworkUser user) {
                return Stacks.ToString();
            }

            public override void Set(NetworkUser user, string value) {
                Stacks = int.Parse(value);
            }
        }
        
        [HarmonyPatch(typeof(SceneDirector))]
        [HarmonyPatch("Start")]
        static class SceneDirector_Start_Patch {
            static bool Prefix(SceneDirector __instance, ref Xoroshiro128Plus ___rng, ref int ___interactableCredit, ref int ___monsterCredit) {
                ___rng = new Xoroshiro128Plus((ulong)Run.instance.stageRng.nextUint);
				float num = 0.5f + (float)Run.instance.participatingPlayerCount * 0.5f;
                num *= LevelIntrInVar.Stacks;
				ClassicStageInfo component = SceneInfo.instance.GetComponent<ClassicStageInfo>();
				if (component) {
					___interactableCredit = (int)((float)component.sceneDirectorInteractibleCredits * num);
					Debug.LogFormat("Spending {0} credits on interactables...", new object[] {
						___interactableCredit
					});
					___monsterCredit = (int)((float)component.sceneDirectorMonsterCredits * Run.instance.difficultyCoefficient) * LevelMonsterInVar.Stacks;
				}

                Util.RaiseStatic(typeof(SceneDirector), "onPrePopulateSceneServer", __instance);
				new Traverse(__instance).Method("PopulateScene").GetValue();
                Util.RaiseStatic(typeof(SceneDirector), "onPostPopulateSceneServer", __instance);
                return false;
            }
        }
    }
}
