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
    class StatsExtension : InfinityExtension {
        class BaseFloatInVar : InVar {
            String Name;
            public BaseFloatInVar(String name) { Name = name; }

            public override string Get(NetworkUser user) {
                var b = user.GetCurrentBody();
                if (b == null) return "null";
                return ((float)Traverse.Create(b).Field(Name).GetValue()).ToString();
            }

            public override void Set(NetworkUser user, string value) {
                var b = user.GetCurrentBody();
                if (b == null) return;
                Traverse.Create(b).Field(Name).SetValue(float.Parse(value));
            }
        }

        public override void Init() {
            foreach (var nm in new[] {
                "baseMaxHealth",
                "baseRegen",
                "baseMaxShield",
                "baseMoveSpeed",
                "baseAcceleration",
                "baseJumpPower",
                "baseDamage",
                "baseAttackSpeed",
                "baseCrit",
                "baseArmor",
            }) {
                var v = new InVarName("stat_" + nm.Substring(4));
                v.User = true;
                Infinity.registerVar(v, new BaseFloatInVar(nm));
            }
        }

        [InVarName("stat_jumpcount", User = true)]
        class BaseJumpInVar : InVar {
            public override string Get(NetworkUser user) {
                var b = user.GetCurrentBody();
                if (b == null) return "null";
                return b.baseJumpCount.ToString();
            }

            public override void Set(NetworkUser user, string value) {
                var b = user.GetCurrentBody();
                if (b == null) return;
                b.baseJumpCount = int.Parse(value);
            }
        }

        [InVarName("lunar", User = true)]
        class LunarInVar : InVar {
            public override string Get(NetworkUser user) {
                return user.lunarCoins.ToString();
            }

            public override void Set(NetworkUser user, string value) {
                var v = uint.Parse(value);
                var c = user.lunarCoins;
                if (c > v) {
                    user.DeductLunarCoins(c - v);
                } else if (c < v) {
                    user.AwardLunarCoins(v - c);
                }
            }
        }

        [InVarName("money", User = true, Perms = PermFlags.Trusted)]
        class MoneyInVar : InVar {
            public override string Get(NetworkUser user) {
                return user.master.money.ToString();
            }

            public override void Set(NetworkUser user, string value) {
                user.master.money = uint.Parse(value);
            }
        }

        [InCmd("team_money", Perms = PermFlags.Admin)]
        private static void CmdTeamMoney(ConCommandArgs args) {
            var user = LocalUserManager.GetFirstLocalUser();

            if (user.currentNetworkUser == null) {
                Debug.LogFormat("Not in-game");
                return;
            }

            if (args.Count != 1) {
                Debug.LogWarning("Error: One parameter expected");
                return;
            }

            var n = uint.Parse(args[0]);
            TeamManager.instance.GiveTeamMoney(TeamIndex.Player, n);
            Debug.LogFormat("Gave all players ${0}", n);
        }
    }
}
