namespace ExpeditionsReforged.Systems
{
    internal enum ExpeditionPacketType : byte
    {
        SyncPlayer,
        StartExpedition,
        ConditionProgress,
        CompleteExpedition,
        ClaimRewards,
        TrackExpedition
    }
}
