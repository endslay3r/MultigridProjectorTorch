using System;
using System.Collections.Generic;
using MultigridProjector.Utilities;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Network;

namespace MultigridProjector.Logic
{
    // Server-side validation of BuildInternal RPCs from clients, matching the checks added
    // in Space Engineers 1.211.018 and mirrored by Multigrid Projector 1.0.5.
    //
    // The builder must be the sender's own controlled character, and builtBy must be the
    // sender's identity. MGP's own Build patches call BuildInternal locally (with the subgrid
    // index in builtBy), so this applies to RPCs from clients only.
    public static class BuildRequests
    {
        private static readonly HashSet<string> Logged = new HashSet<string>();

        // The sender checks of the vanilla BuildInternal RPC since 1.211.018: the builder is the
        // sender's own character, which the sender controls, and builtBy is the sender's identity.
        // MGP's own Build patches call BuildInternal locally, with the subgrid index in builtBy,
        // so this applies to RPCs from clients only.
        public static bool IsRpcAllowed(ulong sender, long builder, long builtBy)
        {
            var players = MySession.Static.Players;
            var player = players.GetPlayerById(new MyPlayer.PlayerId(sender));
            var character = player?.Character;
            if (
                character == null
                || character.EntityId != builder
                || player.Controller?.ControlledEntity != character
            )
            {
                LogOnce(
                    sender,
                    "rpc builder",
                    $"Refused a BuildInternal RPC from {sender}: builder {builder} is not the sender's controlled character"
                );
                return false;
            }

            var identityId = players.TryGetIdentityId(sender);
            if (identityId == 0 || builtBy != identityId)
            {
                LogOnce(
                    sender,
                    "rpc builtBy",
                    $"Refused a BuildInternal RPC from {sender}: builtBy {builtBy} is not the sender's identity {identityId}"
                );
                return false;
            }

            return true;
        }

        // A BuildInternal RPC from a client, for the record. Vanilla clients and MGP clients in
        // client-only mode send builtBy = owner.
        public static void NoteRpc(ulong sender, long owner, long builtBy)
        {
            LogOnce(
                sender,
                "rpc",
                builtBy == owner
                    ? $"BuildInternal RPC from {sender} with vanilla arguments"
                    : $"BuildInternal RPC from {sender} with builtBy {builtBy}, not the owner {owner}"
            );
        }

        private static void LogOnce(ulong sender, string kind, string message)
        {
            lock (Logged)
                if (!Logged.Add($"{sender} {kind}"))
                    return;

            PluginLog.Info(message);
        }
    }
}
