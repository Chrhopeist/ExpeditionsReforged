namespace ExpeditionsReforged.Systems
{
    internal enum ExpeditionPacketType : byte
    {
        SyncPlayer,
        SyncDefinitions,
        StartExpedition,
        ConditionProgress,
        CompleteExpedition,
        ClaimRewards,
        TrackExpedition,
        TurnInExpedition
    }
}
