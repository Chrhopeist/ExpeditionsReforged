using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using ExpeditionsReforged.UI;
using ExpeditionsReforged.Players;
using Terraria.ModLoader.Config;

namespace ExpeditionsReforged.Systems
{
    public class ExpeditionsSystem : ModSystem
    {
        private UserInterface _expeditionInterface;
        private UserInterface _trackerInterface;
        private ExpeditionUI _expeditionUI;
        private TrackerUI _trackerUI;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            // UI states are registered during mod load, but player data is not available until a world
            // is active. ExpeditionUI defers any player-dependent population until a player exists.
            _expeditionInterface = new UserInterface();
            _trackerInterface = new UserInterface();
            _expeditionUI = new ExpeditionUI();
            _expeditionUI.Activate();
            _expeditionInterface.SetState(_expeditionUI);

            _trackerUI = new TrackerUI();
            _trackerUI.Activate();
            _trackerInterface.SetState(_trackerUI);
        }

        public override void Unload()
        {
            _expeditionInterface = null;
            _trackerInterface = null;
            _expeditionUI = null;
            _trackerUI = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            var expeditionsPlayer = Main.LocalPlayer?.GetModPlayer<ExpeditionsPlayer>();
            SyncExpeditionUiState(expeditionsPlayer);

            _expeditionInterface?.Update(gameTime);
            _trackerInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            ExpeditionsPlayer expeditionsPlayer = Main.LocalPlayer?.GetModPlayer<ExpeditionsPlayer>();
            var clientConfig = ModContent.GetInstance<ExpeditionsClientConfig>();

            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));

            bool trackerVisible = expeditionsPlayer != null && (expeditionsPlayer.TrackerUIOpen || (clientConfig.TrackerAutoShow && !string.IsNullOrWhiteSpace(expeditionsPlayer.TrackedExpeditionId)));

            // Draw the tracker behind core UI (hotbar/inventory) so it does not cover other panels.
            int trackerInsertIndex = inventoryIndex != -1 ? inventoryIndex : mouseTextIndex;
            if (trackerInsertIndex == -1)
            {
                trackerInsertIndex = layers.Count;
            }

            layers.Insert(trackerInsertIndex, new LegacyGameInterfaceLayer(
                "ExpeditionsReforged: Tracker UI",
                delegate
                {
                    if (trackerVisible && _trackerInterface?.CurrentState != null)
                    {
                        _trackerInterface.Draw(Main.spriteBatch, new GameTime());
                    }

                    return true;
                },
                InterfaceScaleType.UI));

            int expeditionInsertIndex = mouseTextIndex != -1 ? mouseTextIndex : layers.Count;
            if (trackerInsertIndex <= expeditionInsertIndex)
            {
                expeditionInsertIndex++;
            }

            layers.Insert(expeditionInsertIndex, new LegacyGameInterfaceLayer(
                "ExpeditionsReforged: Expedition UI",
                delegate
                {
                    if (expeditionsPlayer != null && expeditionsPlayer.ExpeditionUIOpen && _expeditionInterface?.CurrentState != null)
                    {
                        _expeditionInterface.Draw(Main.spriteBatch, new GameTime());
                    }

                    return true;
                },
                InterfaceScaleType.UI));
        }

        /// <summary>
        /// Client-only close helper for the main Expeditions UI.
        /// </summary>
        public void CloseExpeditionUi(ExpeditionsPlayer expeditionsPlayer)
        {
            expeditionsPlayer.ExpeditionUIOpen = false;
            _expeditionInterface?.SetState(null);
        }

        /// <summary>
        /// Client-only open helper for the main Expeditions UI.
        /// </summary>
        public void OpenExpeditionUi()
        {
            // Use the NPC overload to keep UI-opening logic consistent when no quest giver is provided.
            NPC npc = null;
            OpenExpeditionUi(npc);
        }

        /// <summary>
        /// Client-only open helper that can lock the NPC filter to the provided NPC.
        /// </summary>
        public void OpenExpeditionUi(NPC npc)
        {
            OpenExpeditionUi(npc?.type);
        }

        /// <summary>
        /// Client-only open helper that can lock the NPC filter to a specific quest giver.
        /// </summary>
        public void OpenExpeditionUi(int? questGiverNpcId)
        {
            if (Main.dedServ)
            {
                return;
            }

            ExpeditionsPlayer expeditionsPlayer = Main.LocalPlayer?.GetModPlayer<ExpeditionsPlayer>();

            if (expeditionsPlayer == null)
            {
                return;
            }

            if (_expeditionUI != null)
            {
                // Lock or clear the NPC filter before opening so the list is immediately scoped.
                _expeditionUI.SetNpcFilterLock(questGiverNpcId, questGiverNpcId.HasValue);
            }

            expeditionsPlayer.ExpeditionUIOpen = true;
            _expeditionInterface?.SetState(_expeditionUI);
        }

        private void SyncExpeditionUiState(ExpeditionsPlayer expeditionsPlayer)
        {
            if (Main.dedServ || _expeditionInterface == null || _expeditionUI == null)
            {
                return;
            }

            if (expeditionsPlayer == null || !expeditionsPlayer.ExpeditionUIOpen)
            {
                if (_expeditionInterface.CurrentState != null)
                {
                    _expeditionInterface.SetState(null);
                }

                return;
            }

            if (_expeditionInterface.CurrentState == null)
            {
                _expeditionInterface.SetState(_expeditionUI);
            }
        }
    }
}
