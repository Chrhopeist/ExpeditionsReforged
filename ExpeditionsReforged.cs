using System.IO;
using ExpeditionsReforged.Players;
using ExpeditionsReforged.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged
{
    public class ExpeditionsReforged : Mod
    {
        internal static ExpeditionsReforged Instance { get; private set; }

        public static ModKeybind OpenExpeditionsKeybind { get; private set; }

        public override void Load()
        {
            Instance = this;

            if (!Main.dedServ)
            {
                // Client-only keybind registration for toggling the Expeditions UI from Mod Controls.
                OpenExpeditionsKeybind = KeybindLoader.RegisterKeybind(this, "Open Expeditions", "None");
            }
        }

        public override void Unload()
        {
            OpenExpeditionsKeybind = null;
            Instance = null;
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            ExpeditionPacketType packetType = (ExpeditionPacketType)reader.ReadByte();
            switch (packetType)
            {
                case ExpeditionPacketType.SyncPlayer:
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        byte playerIndex = reader.ReadByte();
                        if (playerIndex < Main.maxPlayers)
                        {
                            Player player = Main.player[playerIndex];
                            player.GetModPlayer<ExpeditionsPlayer>().ReceiveProgressSync(reader);
                        }
                    }

                    break;

                case ExpeditionPacketType.StartExpedition:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        string expeditionId = reader.ReadString();
                        HandleStartRequest(expeditionId, whoAmI);
                    }

                    break;

                case ExpeditionPacketType.ConditionProgress:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        string conditionId = reader.ReadString();
                        int amount = reader.ReadInt32();
                        HandleConditionProgress(conditionId, amount, whoAmI);
                    }

                    break;

                case ExpeditionPacketType.CompleteExpedition:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        string expeditionId = reader.ReadString();
                        HandleCompletion(expeditionId, whoAmI);
                    }

                    break;

                case ExpeditionPacketType.ClaimRewards:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        string expeditionId = reader.ReadString();
                        HandleClaim(expeditionId, whoAmI);
                    }

                    break;

                case ExpeditionPacketType.TrackExpedition:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        string expeditionId = reader.ReadString();
                        HandleTrack(expeditionId, whoAmI);
                    }

                    break;
            }
        }

        internal static void RequestStart(string expeditionId)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Instance is null)
            {
                return;
            }

            ModPacket packet = Instance.GetPacket();
            packet.Write((byte)ExpeditionPacketType.StartExpedition);
            packet.Write(expeditionId ?? string.Empty);
            packet.Send();
        }

        internal static void RequestCompletion(string expeditionId)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Instance is null)
            {
                return;
            }

            ModPacket packet = Instance.GetPacket();
            packet.Write((byte)ExpeditionPacketType.CompleteExpedition);
            packet.Write(expeditionId ?? string.Empty);
            packet.Send();
        }

        internal static void RequestClaim(string expeditionId)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Instance is null)
            {
                return;
            }

            ModPacket packet = Instance.GetPacket();
            packet.Write((byte)ExpeditionPacketType.ClaimRewards);
            packet.Write(expeditionId ?? string.Empty);
            packet.Send();
        }

        internal static void SendProgressSync(int toWho, int forPlayer, ExpeditionsPlayer expeditionsPlayer)
        {
            if (Instance is null)
            {
                return;
            }

            ModPacket packet = Instance.GetPacket();
            packet.Write((byte)ExpeditionPacketType.SyncPlayer);
            packet.Write((byte)forPlayer);
            expeditionsPlayer.WriteProgressToPacket(packet);
            packet.Send(toWho, forPlayer);
        }

        internal static void RequestTrack(string expeditionId)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Instance is null)
            {
                return;
            }

            ModPacket packet = Instance.GetPacket();
            packet.Write((byte)ExpeditionPacketType.TrackExpedition);
            packet.Write(expeditionId ?? string.Empty);
            packet.Send();
        }

        private void HandleStartRequest(string expeditionId, int sender)
        {
            Player player = Main.player[sender];
            ExpeditionsPlayer expeditionsPlayer = player.GetModPlayer<ExpeditionsPlayer>();
            if (expeditionsPlayer.TryStartExpedition(expeditionId))
            {
                SendProgressSync(-1, player.whoAmI, expeditionsPlayer);
            }
        }

        private void HandleConditionProgress(string conditionId, int amount, int sender)
        {
            if (amount <= 0)
            {
                return;
            }

            Player player = Main.player[sender];
            ExpeditionsPlayer expeditionsPlayer = player.GetModPlayer<ExpeditionsPlayer>();
            if (expeditionsPlayer.ReportedFromServer(conditionId, amount))
            {
                SendProgressSync(-1, player.whoAmI, expeditionsPlayer);
            }
        }

        private void HandleCompletion(string expeditionId, int sender)
        {
            Player player = Main.player[sender];
            ExpeditionsPlayer expeditionsPlayer = player.GetModPlayer<ExpeditionsPlayer>();
            if (expeditionsPlayer.TryCompleteExpedition(expeditionId))
            {
                SendProgressSync(-1, player.whoAmI, expeditionsPlayer);
            }
        }

        private void HandleClaim(string expeditionId, int sender)
        {
            Player player = Main.player[sender];
            ExpeditionsPlayer expeditionsPlayer = player.GetModPlayer<ExpeditionsPlayer>();
            if (expeditionsPlayer.TryClaimRewards(expeditionId))
            {
                SendProgressSync(-1, player.whoAmI, expeditionsPlayer);
            }
        }

        private void HandleTrack(string expeditionId, int sender)
        {
            Player player = Main.player[sender];
            ExpeditionsPlayer expeditionsPlayer = player.GetModPlayer<ExpeditionsPlayer>();
            if (expeditionsPlayer.TryTrackExpedition(expeditionId))
            {
                SendProgressSync(-1, player.whoAmI, expeditionsPlayer);
            }
        }
    }
}
