using System;
using System.Collections.Generic;
using ExpeditionsReforged.Players;
using ExpeditionsReforged.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Compat
{
    internal static class DialogueTweakCompat
    {
        private const string ExpeditionButtonText = "Expedition";

        internal static void TryRegisterDialogueButtons()
        {
            if (Main.dedServ)
            {
                return;
            }

            if (!ModLoader.TryGetMod("DialogueTweak", out Mod dialogueTweak))
            {
                return;
            }

            // DialogueTweak integration is optional; fail silently if its API changes.
            try
            {
                List<int> npcTypes = new() { NPCID.Guide };

                dialogueTweak.Call(
                    "AddButton",
                    npcTypes,
                    (Func<string>)(() => ExpeditionButtonText),
                    (Func<string>)(() => null),
                    (Action)HandleExpeditionButtonHover
                );

                // Minimal logging to confirm DialogueTweak registration succeeded.
                ExpeditionsReforged.Instance?.Logger.Info("DialogueTweak detected: registered expedition dialogue button.");
            }
            catch (Exception)
            {
                // DialogueTweak is optional; Mod.Call errors should not block mod loading.
            }
        }

        private static void HandleExpeditionButtonHover()
        {
            // DialogueTweak only supplies a hover callback, so treat a left click while hovering as activation.
            if (Main.dedServ || !Main.mouseLeft || !Main.mouseLeftRelease)
            {
                return;
            }

            ExpeditionsPlayer expeditionsPlayer = Main.LocalPlayer.GetModPlayer<ExpeditionsPlayer>();
            // Track the requested UI state even if the UI layer is recreated later.
            expeditionsPlayer.ExpeditionUIOpen = true;

            // Open the Expeditions UI without interfering with the NPC chat window.
            ModContent.GetInstance<ExpeditionsSystem>().OpenExpeditionUi();
        }

    }
}
