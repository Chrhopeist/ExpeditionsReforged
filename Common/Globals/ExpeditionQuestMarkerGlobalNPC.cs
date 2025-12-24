using ExpeditionsReforged.Content.Expeditions;
using ExpeditionsReforged.Players;
using ExpeditionsReforged.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Common.Globals
{
    // Draws a simple quest marker over NPCs that have at least one available expedition for the local player.
    public class ExpeditionQuestMarkerGlobalNPC : GlobalNPC
    {
        private const float MarkerOffsetY = 12f;

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc.type == NPCID.Guide)
            {
                ModContent.GetInstance<Mod>().Logger.Info("[QuestMarker] PostDraw hit for Guide");
            }

            if (npc is null || !npc.active)
            {
                return;
            }

            Player player = Main.LocalPlayer;
            if (npc.type == NPCID.Guide)
            {
                ModContent.GetInstance<Mod>().Logger.Info(
                    $"[QuestMarker] Guide active={npc.active}, playerActive={Main.LocalPlayer?.active}"
                );
            }

            if (player?.active != true)
            {
                return;
            }

            bool isGiver = ExpeditionService.IsExpeditionGiver(npc.type, player);
            if (npc.type == NPCID.Guide)
            {
                ModContent.GetInstance<Mod>().Logger.Info($"[QuestMarker] IsExpeditionGiver={isGiver}");
            }

            if (!isGiver)
            {
                return;
            }

            if (!ShouldShowQuestMarker(npc, player))
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
            // Only show markers for expeditions the local player can accept from this specific NPC.
            ExpeditionsPlayer expeditionsPlayer = player.GetModPlayer<ExpeditionsPlayer>();
            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();

            ModContent.GetInstance<Mod>().Logger.Info(
                $"[QuestMarker] Checking expeditions for NPC type {npc.type}"
            );

            foreach (ExpeditionDefinition definition in registry.Definitions)
            {
                if (definition.QuestGiverNpcId != npc.type)
                {
                    continue;
                }

                if (definition.QuestGiverNpcId == npc.type)
                {
                    ModContent.GetInstance<Mod>().Logger.Info(
                        $"[QuestMarker] Found expedition {definition.Id} for NPC {npc.type}"
                    );
                }

                if (ExpeditionService.CanAcceptExpedition(player, definition, out _))
                {
                    ModContent.GetInstance<Mod>().Logger.Info(
                        $"[QuestMarker] CanAcceptExpedition TRUE for {definition.Id}"
                    );
                    return true;
                }
            }

            return false;
        }
    }
}
