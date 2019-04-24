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
    class MiscExtension : InfinityExtension {
        [InVarName("modded", User = true, Perms = PermFlags.Host)]
        class ModdedInVar : InVar {
            public override string Get(NetworkUser user) {
                return RoR2Application.isModded ? "1" : "0";
            }

            public override void Set(NetworkUser user, string value) {
                RoR2Application.isModded = int.Parse(value) != 0;
            }
        }
    }
}
