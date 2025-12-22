using ExpeditionsReforged.Systems;
using Terraria;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Content.NPCs;

/// <summary>
/// Base NPC type that only adds the expedition chat button and delegates to the expedition UI.
/// </summary>
public abstract class ExpeditionChatNPC : ModNPC
{
    public override void SetChatButtons(ref string button, ref string button2)
    {
        // Only show the expedition button if this NPC can offer expeditions for the local player.
        if (Main.LocalPlayer != null && ExpeditionService.IsExpeditionGiver(NPC.type, Main.LocalPlayer))
        {
            button2 = "Expedition";
        }
    }

    public override void OnChatButtonClicked(bool firstButton, ref string shop)
    {
        if (firstButton)
        {
            return;
        }

        // Client-only UI: open the expedition list for this NPC without mutating gameplay state.
        if (Main.dedServ)
        {
            return;
        }

        ModContent.GetInstance<ExpeditionsSystem>().OpenNpcExpeditionUI(NPC.type);
    }
}
