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

    public class InCmdResult {
        public List<String> Result = new List<string>();
        public String Error;

        public void Add(object x) {
            Result.Add(x.ToString());
        }
    }

    public class InCmdArgs {
        public NetworkUser Sender;
        public NetworkUser Target;
        public string[] Args;

        public string this[int i] { get {
			return this.Args[i];
        }}

		public int Length { get {
			return this.Args.Length;
	    }}
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
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
    
    public delegate void InCmdDelegate(InCmdResult res, InCmdArgs args);

    public abstract class InVar {
        public abstract void Set(NetworkUser user, string value);
        public abstract string Get(NetworkUser user);
    }

    public abstract class InfinityExtension {
        public virtual void Init() { }
    }

    public class Infinity {
        public static Assembly baseAssembly;
        public static RoR2.Console console;
        public static HarmonyInstance harmony;

        static void registerCmd(InCmd attr, InCmdDelegate cb) {
            object catalog = Traverse.Create(console).Field("concommandCatalog").GetValue();
            var conCommandType = typeof(RoR2.Console).GetNestedType("ConCommand", BindingFlags.NonPublic);
            var conCommandCtor = conCommandType.GetConstructor(Type.EmptyTypes);

            var cmd = conCommandCtor.Invoke(new object[0]);

            conCommandType.GetField("flags").SetValue(cmd, attr.User ? ConVarFlags.None : ConVarFlags.ExecuteOnServer);
            conCommandType.GetField("action").SetValue(cmd, (RoR2.Console.ConCommandDelegate)((ConCommandArgs args) => {
                var res = new InCmdResult();
                var cargs = new InCmdArgs() {
                    Args = args.userArgs.ToArray(),
                    Sender = args.sender,
                    Target = args.sender,
                };
                try {
                    cb(res, cargs);
                } catch (Exception e) {
                    res.Error = e.ToString() + "\n" + e.StackTrace;
                }

                if (args.sender.localUser != null) {
                    Debug.Log(res.Result.Join(null, "\n"));
                    if (res.Error != null) Debug.LogWarning(res.Error);
                } else {
                    // TODO: Support clients executing commands
                }
            }));
            conCommandType.GetField("helpText").SetValue(cmd, attr.Hint);

            catalog.GetType().GetProperty("Item").SetValue(catalog, cmd, new[] {
                attr.Name.ToLower(System.Globalization.CultureInfo.InvariantCulture)
            });
        }
        
        public static void registerVar(InCmd attr, InVar var) {
            registerCmd(attr, (InCmdResult res, InCmdArgs args) => {
                if (args.Length == 0) {
                    res.Add(attr.Name + " = " + var.Get(args.Sender));
                } else {
                    if (args.Length != 1) {
                        res.Add("zero or one arguments expected");
                    } else {
                        var.Set(args.Target, args[0]);
                        res.Add(attr.Name + " = " + var.Get(args.Target));
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
                                registerCmd(at as InCmd, (InCmdDelegate)Delegate.CreateDelegate(typeof(InCmdDelegate), methodInfo));
                            }
                        }
                    }
                    
                    foreach (Type subType in extensions[i].GetType().GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)) {
                        object[] attr = subType?.GetCustomAttributes(false);
                        if (attr != null) foreach (var at in attr) {
                            if (at is InCmd) {
                                registerVar(at as InCmd, (InVar)subType.GetConstructor(Type.EmptyTypes).Invoke(new object[0]));
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