using ExpeditionsReforged.Content.Expeditions;
using ExpeditionsReforged.Players;
using ExpeditionsReforged.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Common.Globals
{
    // Draws a simple quest marker over NPCs that have at least one available expedition for the local player.
    public class ExpeditionQuestMarkerGlobalNPC : GlobalNPC
    {
        private const float MarkerOffsetY = 12f;

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc is null || !npc.active)
            {
                return;
            }

            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
            {
                return;
            }

            if (player?.active != true)
            {
                return;
            }

            bool isGiver = ExpeditionService.IsExpeditionGiver(npc.type, player);

            if (!isGiver)
            {
                return;
            }

            bool showMarker;
            try
            {
                showMarker = ShouldShowQuestMarker(npc, player);
            }
            catch
            {
                return;
            }

            if (!showMarker)
            {
                return;
            }

            Texture2D markerTexture = ModContent.Request<Texture2D>("ExpeditionsReforged/Assets/UI/ExpeditionExclamation").Value;
            Vector2 drawPosition = npc.Top + new Vector2(0f, -MarkerOffsetY);
            drawPosition -= screenPos;
            drawPosition -= new Vector2(markerTexture.Width * 0.5f, markerTexture.Height);

            spriteBatch.Draw(markerTexture, drawPosition, Color.White);
        }

        private static bool ShouldShowQuestMarker(NPC npc, Player player)
        {
            if (player == null)
            {
                return false;
            }

            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();
            if (registry == null || registry.Definitions == null)
            {
                return false;
            }

            // Only show markers for expeditions the local player can accept from this specific NPC.
            ExpeditionsPlayer expeditionsPlayer = player.GetModPlayer<ExpeditionsPlayer>();

            foreach (ExpeditionDefinition definition in registry.Definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (definition.QuestGiverNpcId != npc.type)
                {
                    continue;
                }

                if (ExpeditionService.CanAcceptExpedition(player, definition, out _))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
