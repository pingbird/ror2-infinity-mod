using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoR2.Mods;
using System.Reflection;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using Harmony;

namespace InfinityMod {
    class TeleporterExtension : InfinityExtension {
        [InVarName("tele_speed", Perms = PermFlags.Admin)]
        class TeleSpeedInVar : InVar {
            public static float Speed = 1.0f;
            public override string Get(NetworkUser user) {
                return Speed.ToString();
            }

            public override void Set(NetworkUser user, string value) {
                Speed = float.Parse(value);
            }
        }

        [InVarName("tele_boss_stacks", Perms = PermFlags.Admin)]
        class TeleBossStacksInVar : InVar {
            public override string Get(NetworkUser user) {
                return TeleporterInteraction.instance.shrineBonusStacks.ToString();
            }

            public override void Set(NetworkUser user, string value) {
                TeleporterInteraction.instance.shrineBonusStacks = int.Parse(value);
            }
        }

        [InVarName("tele_shop_portal", Perms = PermFlags.Admin)]
        class TeleShopPortalInVar : InVar {
            public override string Get(NetworkUser user) {
                return TeleporterInteraction.instance.shouldAttemptToSpawnShopPortal ? "1" : "0";
            }

            public override void Set(NetworkUser user, string value) {
                TeleporterInteraction.instance.shouldAttemptToSpawnShopPortal = int.Parse(value) != 0;
            }
        }

        [InVarName("tele_gold_portal", Perms = PermFlags.Admin)]
        class TeleGoldPortalInVar : InVar {
            public override string Get(NetworkUser user) {
                return TeleporterInteraction.instance.shouldAttemptToSpawnGoldshoresPortal ? "1" : "0";
            }

            public override void Set(NetworkUser user, string value) {
                TeleporterInteraction.instance.shouldAttemptToSpawnGoldshoresPortal = int.Parse(value) != 0;
            }
        }

        [InVarName("tele_ms_portal", Perms = PermFlags.Admin)]
        class TeleMSPortalInVar : InVar {
            public override string Get(NetworkUser user) {
                return TeleporterInteraction.instance.shouldAttemptToSpawnMSPortal ? "1" : "0";
            }

            public override void Set(NetworkUser user, string value) {
                TeleporterInteraction.instance.shouldAttemptToSpawnMSPortal = int.Parse(value) != 0;
            }
        }

        [InCmd("tele_ping", User = true, Perms = PermFlags.Admin)]
        private static void TelePingCmd(ConCommandArgs args) {
            var ctrl = args.sender.master.GetComponent<PingerController>();
            if (ctrl == null) {
                Debug.Log("No pinger controller :(");
            }
            var tele = TeleporterInteraction.instance;
            PingerController.PingInfo pingInfo = new PingerController.PingInfo {
				active = true,
                origin = tele.transform.position,
                targetNetworkIdentity = ClientScene.objects[tele.netId],
			};
            typeof(PingerController).GetMethod("SetCurrentPing", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ctrl, new object[] { pingInfo });
        }

        [InCmd("tele_exit", User = true, Perms = PermFlags.Admin)]
        private static void TeleExitCmd(ConCommandArgs args) {
            TeleporterInteraction.instance.GetComponent<SceneExitController>().Begin();
        }

        [HarmonyPatch(typeof(TeleporterInteraction))]
        [HarmonyPatch("OnStateChanged")]
        static class TeleporterInteraction_OnStateChanged_Patch {
            static bool Prefix(TeleporterInteraction __instance, ref ChildLocator ___childLocator, int oldActivationState, int newActivationState) {
                switch (newActivationState) {
                    case 0x00: // TeleporterInteraction.ActivationState.Idle
                        return false;
                    case 0x01: // TeleporterInteraction.ActivationState.IdleToCharging
                        ___childLocator.FindChild("IdleToChargingEffect").gameObject.SetActive(true);
                        ___childLocator.FindChild("PPVolume").gameObject.SetActive(true);
                        return false;
                    case 0x02: // TeleporterInteraction.ActivationState.Charging
                    {
                        Util.RaiseStatic(typeof(TeleporterInteraction), "onTeleporterBeginChargingGlobal", __instance);
                        if (NetworkServer.active) {
                            if (__instance.bonusDirector) {
                                __instance.bonusDirector.enabled = true;
                            }
                            if (__instance.bossDirector) {
                                __instance.bossDirector.enabled = true;
                                __instance.bossDirector.monsterCredit += (float)((int)(600f * Mathf.Pow(Run.instance.compensatedDifficultyCoefficient, 0.5f) * (float)(1 + __instance.shrineBonusStacks)));
                                __instance.bossDirector.currentSpawnTarget = __instance.gameObject;
                                __instance.bossDirector.SetNextSpawnAsBoss();
                            }
                            if (DirectorCore.instance) {
                                CombatDirector[] components = DirectorCore.instance.GetComponents<CombatDirector>();
                                if (components.Length != 0) {
                                    CombatDirector[] array = components;
                                    for (int i = 0; i < array.Length; i++) {
                                        array[i].enabled = false;
                                    }
                                }
                            }

                            var chestLockCoroutine = new Traverse(__instance).Field<Coroutine>("chestLockCoroutine");
                            System.Collections.IEnumerator lockBody() {
                                Debug.Log("Function patched :poggers:");
			                    List<GameObject> lockInstances = new List<GameObject>();
			                    Vector3 myPosition = __instance.transform.position;
			                    float maxDistanceSqr = __instance.clearRadius * __instance.clearRadius;
			                    PurchaseInteraction[] purchasables = UnityEngine.Object.FindObjectsOfType<PurchaseInteraction>();
			                    int num;
			                    for (int i = 0; i < purchasables.Length; i = num)
			                    {
                                    if (ChestsExtension.ChestUnlockInVar.Unlock) break;
				                    if (purchasables[i] && purchasables[i].available)
				                    {
					                    Vector3 position = purchasables[i].transform.position;
					                    if ((position - myPosition).sqrMagnitude > maxDistanceSqr && !purchasables[i].lockGameObject)
					                    {
						                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.lockPrefab, position, Quaternion.identity);
						                    NetworkServer.Spawn(gameObject);
						                    purchasables[i].NetworklockGameObject = gameObject;
						                    lockInstances.Add(gameObject);
						                    yield return new WaitForSeconds(0.1f);
					                    }
				                    }
				                    num = i + 1;
			                    }
			                    while (__instance.NetworkactivationStateInternal == 0x02 && !ChestsExtension.ChestUnlockInVar.Unlock) // TeleporterInteraction.ActivationState.Charging
			                    {
				                    yield return new WaitForSeconds(1f);
			                    }
			                    for (int i = 0; i < lockInstances.Count; i = num)
			                    {
				                    UnityEngine.Object.Destroy(lockInstances[i]);
				                    yield return new WaitForSeconds(0.1f);
				                    num = i + 1;
			                    }
			                    yield break;
                            }
                            if (chestLockCoroutine.Value == null) {
                                chestLockCoroutine.Value = __instance.StartCoroutine(lockBody());
                            }
                        }
                        ___childLocator.FindChild("IdleToChargingEffect").gameObject.SetActive(false);
                        ___childLocator.FindChild("ChargingEffect").gameObject.SetActive(true);
                        return false;
                    }
                    case 0x03: { // TeleporterInteraction.ActivationState.Charged
                        new Traverse(__instance).Field<GameObject>("teleporterPositionIndicator").Value.GetComponent<RoR2.UI.ChargeIndicatorController>().isCharged = true;
                        ___childLocator.FindChild("ChargingEffect").gameObject.SetActive(false);
                        ___childLocator.FindChild("ChargedEffect").gameObject.SetActive(true);
                        ___childLocator.FindChild("BossShrineSymbol").gameObject.SetActive(false);
                        
                        Util.RaiseStatic(typeof(TeleporterInteraction), "onTeleporterChargedGlobal", __instance);
                        return false;
                    }
                    case 0x04: { // TeleporterInteraction.ActivationState.Finished
                        ___childLocator.FindChild("ChargedEffect").gameObject.SetActive(false);
                        Util.RaiseStatic(typeof(TeleporterInteraction), "onTeleporterFinishGlobal", __instance);
                        return false;
                    }
                    default:
                        throw new ArgumentOutOfRangeException("newActivationState", newActivationState, null);
                }
            }
        }

        [HarmonyPatch(typeof(TeleporterInteraction))]
        [HarmonyPatch("StateFixedUpdate")]
        static class TeleporterInteraction_StateFixedUpdate_Patch {
            static bool Prefix(TeleporterInteraction __instance) {
                object Get(String name) {
                    return typeof(TeleporterInteraction).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                }

                void Set(String name, object value) {
                    typeof(TeleporterInteraction).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, value);
                }

                object Call(String name, params object[] parameters) {
                    return typeof(TeleporterInteraction).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, parameters);
                }

            switch (__instance.NetworkactivationStateInternal) {
                case 0x01: // TeleporterInteraction.ActivationState.IdleToCharging
                    Set("idleToChargingStopwatch", (float)Get("idleToChargingStopwatch") + Time.fixedDeltaTime);
                    if ((float)Get("idleToChargingStopwatch") > 3f) {
                        __instance.NetworkactivationStateInternal = 0x02; // TeleporterInteraction.ActivationState.Charging
                    }
                    break;
                case 0x02: // TeleporterInteraction.ActivationState.Charging
                {
                    int num = Run.instance ? Run.instance.livingPlayerCount : 0;
                    float num2 = (num != 0) ? (TeleSpeedInVar.Speed * (int)Call("GetPlayerCountInRadius") / (float)num * Time.fixedDeltaTime) : 0f;
                    bool isCharging = num2 > 0f;
                    __instance.remainingChargeTimer = Mathf.Max(__instance.remainingChargeTimer - num2, 0f);
                    if (NetworkServer.active) {
                        __instance.NetworkchargePercent = (uint)((byte)Mathf.RoundToInt(99f * (1f - __instance.remainingChargeTimer / __instance.chargeDuration)));
                    }
                    if (SceneWeatherController.instance) {
                        SceneWeatherController.instance.weatherLerp = SceneWeatherController.instance.weatherLerpOverChargeTime.Evaluate(1f - __instance.remainingChargeTimer / __instance.chargeDuration);
                    }
                    if (!(GameObject)Get("teleporterPositionIndicator")) {
                        var ind = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/PositionIndicators/TeleporterChargingPositionIndicator"), __instance.transform.position, Quaternion.identity);
                        Set("teleporterPositionIndicator", ind);
                        ind.GetComponent<PositionIndicator>().targetTransform = __instance.transform;
                    } else {
                        RoR2.UI.ChargeIndicatorController component = ((GameObject)Get("teleporterPositionIndicator")).GetComponent<RoR2.UI.ChargeIndicatorController>();
                        component.isCharging = isCharging;
                        component.chargingText.text = ((uint)Get("chargePercent")).ToString() + "%";
                    }

                    Call("UpdateMonstersClear");
                    if (__instance.remainingChargeTimer <= 0f && NetworkServer.active) {
                        if (__instance.bonusDirector) {
                            __instance.bonusDirector.enabled = false;
                        }
                        if ((bool)Get("monstersCleared")) {
                            if (__instance.bossDirector) {
                                __instance.bossDirector.enabled = false;
                            }
                            __instance.NetworkactivationStateInternal = 0x03; // TeleporterInteraction.ActivationState.Charged
                            Call("OnChargingFinished");
                        }
                    }
                    break;
                }
                case 0x03: // TeleporterInteraction.ActivationState.Charged
                    Set("monsterCheckTimer", (float)Get("monsterCheckTimer") - Time.fixedDeltaTime);
                    if ((float)Get("monsterCheckTimer") <= 0f) {
                        Set("monsterCheckTimer", 1f);
                        Call("UpdateMonstersClear");
                    }
                    __instance.NetworkshowBossIndicator = false;
                    break;
            }
            if (__instance.clearRadiusIndicator) {
                __instance.clearRadiusIndicator.SetActive(__instance.NetworkactivationStateInternal >= 0x02); // TeleporterInteraction.ActivationState.Charging
            }

                return false;
            }
        }
    }
}
