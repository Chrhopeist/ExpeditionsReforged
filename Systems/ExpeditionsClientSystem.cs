using ExpeditionsReforged.Players;
using Terraria;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Systems
{
    /// <summary>
    /// Client-side input listener that toggles the Expeditions UI using the registered keybind.
    /// </summary>
    public class ExpeditionsClientSystem : ModSystem
    {
        public override void PostUpdateInput()
        {
            // UI input is client-only. Do not process on dedicated servers.
            if (Main.dedServ)
            {
                return;
            }

            if (Main.gameMenu || Main.LocalPlayer is null)
            {
                return;
            }

            // Avoid toggling while the player is entering text.
            if (Main.drawingPlayerChat || Main.editSign || Main.editChest)
            {
                return;
            }

            if (ExpeditionsReforged.OpenExpeditionsKeybind is null)
            {
                return;
            }

            if (ExpeditionsReforged.OpenExpeditionsKeybind.JustPressed)
            {
                ExpeditionsPlayer expeditionsPlayer = Main.LocalPlayer.GetModPlayer<ExpeditionsPlayer>();
                expeditionsPlayer.ExpeditionUIOpen = !expeditionsPlayer.ExpeditionUIOpen;
            }
        }
    }
}
