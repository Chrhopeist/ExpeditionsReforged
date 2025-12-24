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

            spriteBatch.Draw(markerTexture, drawPosition, Color.White);
        }
    }
}
