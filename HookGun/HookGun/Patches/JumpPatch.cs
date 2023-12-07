using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HookGun.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public class JumpPatch
    {
        [HarmonyPatch("Jump_performed")]
        [HarmonyPrefix]
        static void ChangeJump(ref bool ___isJumping, ref bool ___isFallingFromJump)
        {
            
            //if (HookGunPlugin.HookGunScript.grappling)
            //{
            //    ___isJumping = true;
            //
            //
            //}
            ////else
            ////    ___isJumping = false;
            //
            //if (HookGunPlugin.HookGunScript.DisableJump)
            //{
            //    ___isJumping = false;
            //    HookGunPlugin.HookGunScript.DisableJump = false;
            //
            //
            //}


            //if (HookGunPlugin.HookGunScript.shoudFall)
            //{
            //    ___isFallingFromJump = true;
            //
            //
            //}
            ////else
            ////    ___isFallingFromJump = false;
            //
            //if (HookGunPlugin.HookGunScript.DisableFall)
            //{
            //    ___isFallingFromJump = false;
            //    HookGunPlugin.HookGunScript.DisableFall = false;
            //
            //
            //}
            
        }




    }
}
