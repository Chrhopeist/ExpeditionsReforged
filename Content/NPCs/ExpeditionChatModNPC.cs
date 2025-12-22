using ExpeditionsReforged.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Content.NPCs
{
    public class ExpeditionChatModNPC : ModNPC
    {
        private const int WrappedNpcType = NPCID.Guide;

        public override void SetStaticDefaults()
        {
            // This NPC intentionally mirrors a vanilla NPC for chat button handling.
            // No extra static sets are required beyond the cloned defaults.
        }

        public override string Texture => $"Terraria/Images/NPC_{WrappedNpcType}";

        public override void SetDefaults()
        {
            // Clone defaults from the wrapped NPC type so this NPC mirrors vanilla behavior.
            NPC.CloneDefaults(WrappedNpcType);
            AnimationType = WrappedNpcType;
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            base.SetChatButtons(ref button, ref button2);

            // Only add the expedition button if this NPC can offer expeditions to the local player.
            if (ExpeditionService.IsExpeditionGiver(WrappedNpcType, Main.LocalPlayer))
            {
                button2 = "Expedition";
            }
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shop)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }

            // The expedition button is treated as the second button slot.
            if (!firstButton && string.IsNullOrEmpty(shop))
            {
                if (ExpeditionService.IsExpeditionGiver(WrappedNpcType, Main.LocalPlayer))
                {
                    // NPC chat actions are client-only; open the expedition UI locally.
                    ModContent.GetInstance<ExpeditionsSystem>().OpenNpcExpeditionUI(WrappedNpcType);
                }

                return;
            }

            base.OnChatButtonClicked(firstButton, ref shop);
        }
    }
}
