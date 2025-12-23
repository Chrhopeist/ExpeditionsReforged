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
            ExpeditionsReforged mod = ModContent.GetInstance<ExpeditionsReforged>();
            // Temporary logging to confirm DialogueTweak integration attempts during debugging.
            mod.Logger.Info("DialogueTweakCompat.TryRegisterDialogueButtons: starting registration.");

            if (Main.dedServ)
            {
                mod.Logger.Info("DialogueTweakCompat.TryRegisterDialogueButtons: skipped on dedicated server.");
                return;
            }

            bool hasDialogueTweak = ModLoader.TryGetMod("DialogueTweak", out Mod dialogueTweak);
            mod.Logger.Info($"DialogueTweakCompat.TryRegisterDialogueButtons: DialogueTweak loaded = {hasDialogueTweak}.");

            if (!hasDialogueTweak)
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

                // Minimal logging to confirm the proof-of-concept button registration succeeded.
                ExpeditionsReforged.Instance?.Logger.Info("DialogueTweak detected: registered Test dialogue button.");
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
            // Lock the NPC filter to the quest giver currently being talked to.
            NPC? talkNpc = Main.LocalPlayer?.TalkNPC;
            ModContent.GetInstance<ExpeditionsSystem>().OpenExpeditionUi(talkNpc);
        }

        private static void HandleTestButtonHover()
        {
            // No-op: this button is a UI-only proof of concept.
        }
    }
}
