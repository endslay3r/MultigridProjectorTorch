using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MultigridProjector.Logic;
using Sandbox.Game.Screens.Helpers;

namespace MultigridProjector.Patches
{
    // RestoreToolbar calls MyToolbar.SetItemAtIndex(int, MyToolbarItem) but the game
    // now requires a third bool argument: SetItemAtIndex(int, MyToolbarItem, bool).
    // This transpiler inserts the missing argument (sendChangeNotification = true)
    // and rebinds the call to the 3-argument overload, fixing the MissingMethodException.
    [HarmonyPatch(typeof(ReferenceFixer))]
    [HarmonyPatch(nameof(ReferenceFixer.RestoreToolbar))]
    public static class MyReferenceFixerPatches
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (var i = 0; i < codes.Count; i++)
            {
                if ((codes[i].opcode == OpCodes.Call || codes[i].opcode == OpCodes.Callvirt) &&
                    codes[i].operand is MethodInfo method &&
                    method.Name == "SetItemAtIndex" &&
                    method.DeclaringType == typeof(MyToolbar) &&
                    method.GetParameters().Length == 2)
                {
                    // Push the missing bool argument (true = sendChangeNotification) onto the stack
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldc_I4_1));

                    // Rebind to the 3-argument overload: SetItemAtIndex(int, MyToolbarItem, bool)
                    var targetMethod = typeof(MyToolbar).GetMethod(
                        "SetItemAtIndex",
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        new[] { typeof(int), typeof(MyToolbarItem), typeof(bool) },
                        null);

                    if (targetMethod != null)
                    {
                        codes[i + 1].operand = targetMethod;
                    }
                }
            }

            return codes.AsEnumerable();
        }
    }
}
