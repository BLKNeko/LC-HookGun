using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HookGun.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public class AllowDeathPatch
    {
        [HarmonyPatch("AllowPlayerDeath")]
        [HarmonyPrefix]
        static bool ChangeAllowDeath()
        {

            if (HookGunMain.HookGunScript.NoDmg)
            {
                return false;
                
            }
            else
                return true;
            
        }

    }
}
