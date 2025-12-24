using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

            Texture2D markerTexture = ModContent.Request<Texture2D>("ExpeditionsReforged/Assets/UI/ExpeditionExclamation").Value;
            Vector2 drawPosition = npc.Top + new Vector2(0f, -markerTexture.Height - 4f);
            drawPosition -= screenPos;

            Vector2 origin = new Vector2(markerTexture.Width / 2f, markerTexture.Height);
            float scale = 0.25f;

            spriteBatch.Draw(
                markerTexture,
                drawPosition + origin,
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
