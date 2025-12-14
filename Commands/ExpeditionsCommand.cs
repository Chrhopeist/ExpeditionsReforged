using System;
using ExpeditionsReforged.Players;
using Terraria;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Commands
{
    /// <summary>
    /// Temporary explicit entry point for testing.
    /// - /expeditions : toggles the expedition UI
    /// - /expeditionstart [id] : starts an expedition (default forest_scout)
    /// - /expeditiontrack [id|off] : track/untrack an expedition
    /// </summary>
    public class ExpeditionsCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "expeditions";
        public override string Usage => "/expeditions";
        public override string Description => "Toggle the Expeditions UI.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (Main.gameMenu || Main.LocalPlayer is null)
                return;

            var mp = Main.LocalPlayer.GetModPlayer<ExpeditionsPlayer>();
            mp.ExpeditionUIOpen = !mp.ExpeditionUIOpen;

            Main.NewText(mp.ExpeditionUIOpen ? "Expeditions UI opened." : "Expeditions UI closed.");
        }
    }

    public class ExpeditionStartCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "expeditionstart";
        public override string Usage => "/expeditionstart [expeditionId]";
        public override string Description => "Start an expedition (default: expeditions:forest_scout).";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (Main.gameMenu || Main.LocalPlayer is null)
                return;

            string id = (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
                ? args[0]
                : "expeditions:forest_scout";

            var mp = Main.LocalPlayer.GetModPlayer<ExpeditionsPlayer>();
            mp.TryStartExpedition(id);

            // In multiplayer client, the call sends a request and returns false locally.
            // The server will sync state back through your existing SyncPlayer packet.
            Main.NewText($"Start requested: {id}");
        }
    }

    public class ExpeditionTrackCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "expeditiontrack";
        public override string Usage => "/expeditiontrack [expeditionId|off]";
        public override string Description => "Track/untrack an expedition.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (Main.gameMenu || Main.LocalPlayer is null)
                return;

            string id = (args.Length > 0) ? args[0] : string.Empty;
            if (string.Equals(id, "off", StringComparison.OrdinalIgnoreCase))
                id = string.Empty;

            var mp = Main.LocalPlayer.GetModPlayer<ExpeditionsPlayer>();
            mp.TryTrackExpedition(id);

            Main.NewText(string.IsNullOrWhiteSpace(id) ? "Untracked expedition." : $"Tracking: {id}");
        }
    }
}
