using Harmony;
using RoR2.Mods;
using RoR2;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System;

namespace InfinityMod {
    public static class Util {
        public static Dictionary<TValue, TKey> Reverse<TKey, TValue>(this IDictionary<TKey, TValue> source) {
            var dictionary = new Dictionary<TValue, TKey>();
            foreach (var entry in source) {
                if (!dictionary.ContainsKey(entry.Value))
                    dictionary.Add(entry.Value, entry.Key);
            }
            return dictionary;
        }

        public static void Raise(this object source, string eventName, params object[] args) {
            var eventDelegate = new Traverse(source).Field(eventName).GetValue<MulticastDelegate>();
            if (eventDelegate != null) {
                var param = new object[args.Length + 1];
                param[0] = source;
                for (int i = 0; i < args.Length; i++) {
                    param[i + 1] = args[i];
                }
                foreach (var handler in eventDelegate.GetInvocationList()) {
                    handler.Method.Invoke(handler.Target, param);
                }
            }
        }

        public static void Raise(this Type T, string eventName, params object[] args) {
            var eventDelegate = Traverse.Create(T).Field(eventName).GetValue<MulticastDelegate>();
            if (eventDelegate != null) {
                foreach (var handler in eventDelegate.GetInvocationList()) {
                    handler.Method.Invoke(handler.Target, args);
                }
            }
        }
    }

    [Flags]
    public enum PermFlags {
        None = 0x0,
        Trusted = 0x1,
        Admin = 0x3,
        Host = 0x4,
    }

    public class Perms {
        public static Dictionary<PermFlags, string> flagNames = new Dictionary<PermFlags, String>() {
            {PermFlags.None, "none"},
            {PermFlags.Trusted, "trusted"},
            {PermFlags.Admin, "admin"},
            {PermFlags.Host, "host"},
        };

        public static Dictionary<string, PermFlags> flagNamesRev = flagNames.Reverse();
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class InVarName : Attribute {
        public string Name;
        public bool User = false;
        public PermFlags Perms = PermFlags.Admin;
        public PermFlags ExPerms = PermFlags.Host;
        public InVarName(string name) {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class InCmd : Attribute {
        public string Name;
        public string Hint = "";
        public bool User = false;
        public PermFlags Perms = PermFlags.Admin;
        public PermFlags ExPerms = PermFlags.Host;
        public InCmd(string name) {
            Name = name;
        }
    }

    public abstract class InVar {
        public abstract void Set(NetworkUser user, string value);
        public abstract string Get(NetworkUser user);
    }

    abstract class InfinityExtension {
        public virtual void Init() { }
    }

    public class Infinity {
        public static Assembly baseAssembly;
        public static RoR2.Console console;
        public static HarmonyInstance harmony;

        static void registerCommandDirect(string name, RoR2.Console.ConCommandDelegate cb, string hint = "") {
            object catalog = Traverse.Create(console).Field("concommandCatalog").GetValue();
            var conCommandType = typeof(RoR2.Console).GetNestedType("ConCommand", BindingFlags.NonPublic);
            var conCommandCtor = conCommandType.GetConstructor(Type.EmptyTypes);

            var cmd = conCommandCtor.Invoke(new object[0]);

            conCommandType.GetField("flags").SetValue(cmd, ConVarFlags.None);
            conCommandType.GetField("action").SetValue(cmd, cb);
            conCommandType.GetField("helpText").SetValue(cmd, hint);

            catalog.GetType().GetProperty("Item").SetValue(catalog, cmd, new[] {
                name.ToLower(System.Globalization.CultureInfo.InvariantCulture)
            });
        }

        public static void registerCmd(InCmd cmd, RoR2.Console.ConCommandDelegate cb) {
            registerCommandDirect(cmd.Name, cb, cmd.Hint);
        }

        public static void registerVar(InVarName name, InVar var) {
            registerCommandDirect(name.Name, (ConCommandArgs args) => {
                if (args.Count == 0) {
                    Debug.Log(name.Name + " = " + var.Get(args.sender));
                } else {
                    if (args.Count != 1) {
                        Debug.LogWarning("zero or one arguments expected");
                    } else {
                        var.Set(args.sender, args[0]);
                        Debug.Log(name.Name + " = " + var.Get(args.sender));
                    }
                }
            });
        }

        [ModEntry("Infinity", "1.0.0", "PixelToast")]
        public static void Init() {
            harmony = HarmonyInstance.Create("com.pxtst.infinity");
            baseAssembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(baseAssembly);
        }

        [HarmonyPatch(typeof(RoR2.Console))]
        [HarmonyPatch("Awake")]
        static class Console_Awake_Patch {
            static void Postfix(RoR2.Console __instance) {
                Debug.Log("Initializing Infinity mod");

                console = __instance;

                InfinityExtension[] extensions = new InfinityExtension[] {
                    new MiscExtension(),
                    new PermsExtension(),
                    new StatsExtension(),
                    new LobbyExtension(),
                    new ChestsExtension(),
                    new LevelExtension(),
                    new TeleporterExtension(),
                };

                for (int i = 0; i < extensions.Length; i++) {
                    Debug.Log("Initializing " + extensions[i].GetType().Name);
                    extensions[i].Init();

                    foreach (MethodInfo methodInfo in extensions[i].GetType().GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                        object[] attr = methodInfo?.GetCustomAttributes(false);
                        if (attr != null) foreach (var at in attr) {
                            if (at is InCmd) {
                                registerCmd(at as InCmd, (RoR2.Console.ConCommandDelegate)Delegate.CreateDelegate(typeof(RoR2.Console.ConCommandDelegate), methodInfo));
                            }
                        }
                    }
                    
                    foreach (Type subType in extensions[i].GetType().GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)) {
                        object[] attr = subType?.GetCustomAttributes(false);
                        if (attr != null) foreach (var at in attr) {
                            if (at is InVarName) {
                                registerVar(at as InVarName, (InVar)subType.GetConstructor(Type.EmptyTypes).Invoke(new object[0]));
                            }
                        }
                    }
                }
                
		        RoR2.Console.instance.SubmitCmd(null, "exec infinity_start", false);
            }
        }

        [HarmonyPatch(typeof(EntityStates.SurvivorPod.Release))]
        [HarmonyPatch("OnEnter")]
        static class Release_OnEnter_Patch {
            static void Postfix(EntityStates.SurvivorPod.Release __instance) {
                var survivorPodController = (SurvivorPodController)typeof(EntityStates.SurvivorPod.SurvivorPodBaseState).GetProperty("survivorPodController", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                var characterBody = survivorPodController.characterBodyObject.GetComponent<CharacterBody>();
                var user = LocalUserManager.GetFirstLocalUser().currentNetworkUser;
                if (characterBody == user.GetCurrentBody())
		            RoR2.Console.instance.SubmitCmd(user, "exec infinity_pod", false);
            }
        }
    }
}