using System;
using ExpeditionsReforged.Players;
using ExpeditionsReforged.Systems;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Compat
{
    internal sealed class DialogueTweakCompat : ModSystem
    {
        private const string ExpeditionButtonId = "ExpeditionsReforged:Expedition";
        private const string ExpeditionButtonLocalizationKey = "Mods.ExpeditionsReforged.UI.ExpeditionButton";

        public override void Load()
        {
            if (Main.dedServ)
            {
                return;
            }

            if (!ModLoader.TryGetMod("DialogueTweak", out Mod dialogueTweak))
            {
                return;
            }

            // DialogueTweak integration is optional; failures should not interrupt mod loading.
            try
            {
                dialogueTweak.Call(
                    "RegisterButton",
                    ExpeditionButtonId,
                    Language.GetTextValue(ExpeditionButtonLocalizationKey),
                    null,
                    (Func<NPC, bool>)IsNpcEligibleForExpeditions,
                    (Action<NPC>)HandleExpeditionButtonClick
                );
            }
            catch (Exception)
            {
                // Fail silently if DialogueTweak changes its API or Mod.Call signature.
            }
        }

        private static bool IsNpcEligibleForExpeditions(NPC npc)
        {
            if (npc == null)
            {
                return false;
            }

            if (!NPCID.Sets.ActsLikeTownNPC[npc.type])
            {
                return false;
            }

            return ExpeditionService.IsExpeditionGiver(npc.type, Main.LocalPlayer);
        }

        private static void HandleExpeditionButtonClick(NPC npc)
        {
            if (Main.dedServ)
            {
                return;
            }

            // Close the NPC chat panel before opening the Expeditions UI.
            Main.npcChatRelease = true;
            Main.playerInventory = false;

            ExpeditionsPlayer expeditionsPlayer = Main.LocalPlayer.GetModPlayer<ExpeditionsPlayer>();
            expeditionsPlayer.ExpeditionUIOpen = true;

            ModContent.GetInstance<ExpeditionsSystem>().OpenExpeditionUi();
        }
    }
}
