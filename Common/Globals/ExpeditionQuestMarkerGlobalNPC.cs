using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ExpeditionsReforged.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Common.Globals
{
    public class ExpeditionQuestMarkerGlobalNPC : GlobalNPC
    {
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Only draw for the Guide
            if (npc == null || !npc.active || npc.townNPC == false)
                return;

            // Check specific Guide type
            if (npc.type != NPCID.Guide)
                return;

            Player player = Main.LocalPlayer;
            if (player?.active != true || player.dead)
            {
                return;
            }

            // Mirror the expedition UI availability rules so the marker only appears when the Guide can offer a quest.
            if (!ExpeditionService.IsExpeditionGiver(npc.type, player))
            {
                return;
            }

            Texture2D markerTexture = ModContent.Request<Texture2D>("ExpeditionsReforged/Assets/UI/OverheadQuestExclamation").Value;
            float scale = 0.05f;

            Vector2 worldPosition = npc.Top + new Vector2(0f, -6f);
            Vector2 screenPosition = worldPosition - screenPos;

            Vector2 origin = new Vector2(
                markerTexture.Width / 2f,
                markerTexture.Height
            );

            spriteBatch.Draw(
                markerTexture,
                screenPosition,
                null,
                Color.White,
                0f,
                origin,
                scale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
