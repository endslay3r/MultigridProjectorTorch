using System;
using System.Reflection;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;
using VRage.Network;
using VRageMath;

namespace MultigridProjectorServer.Patches
{
    [PatchShim]
    [EnsureOriginalTorch(typeof(MyProjectorBase), "BuildInternal", null,"f78ce8f7", "b08d536e")]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyProjectorBase_BuildInternal
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyProjectorBase).GetMethod("BuildInternal", BindingFlags.DeclaredOnly | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)).Prefixes.Add(typeof (MyProjectorBase_BuildInternal).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));

        [ServerOnly]
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            Vector3I cubeBlockPosition,
            long owner,
            long builder,
            bool requestInstant,
            long builtBy)
        {
            var projector = __instance;

            try
            {
                // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                    return true;

                var sender = MyEventContext.Current.Sender.Value;
                BuildRequests.NoteRpc(sender, owner, builtBy);
                if (!BuildRequests.IsRpcAllowed(sender, builder, builtBy))
                    return false;

                // We use the builtBy field to pass the subgrid index
                projection.BuildInternal(cubeBlockPosition, owner, builder, requestInstant, builtBy);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            return false;
        }
    }
}