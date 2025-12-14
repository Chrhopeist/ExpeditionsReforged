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
            _expeditionInterface?.Update(gameTime);
            _trackerInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            ExpeditionsPlayer expeditionsPlayer = Main.LocalPlayer?.GetModPlayer<ExpeditionsPlayer>();
            var clientConfig = ModContent.GetInstance<ExpeditionsClientConfig>();

            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

            if (mouseTextIndex != -1)
            {
                bool trackerVisible = expeditionsPlayer != null && (expeditionsPlayer.TrackerUIOpen || (clientConfig.TrackerAutoShow && !string.IsNullOrWhiteSpace(expeditionsPlayer.TrackedExpeditionId)));
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
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

                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
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
        }
    }
}
