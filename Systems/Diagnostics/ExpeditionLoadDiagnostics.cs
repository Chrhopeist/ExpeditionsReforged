using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Systems.Diagnostics
{
    /// <summary>
    /// Records per-expedition JSON load failures and emits a diagnostic log for server operators.
    /// </summary>
    internal sealed class ExpeditionLoadDiagnostics
    {
        private const string LogFileName = "expedition_load_errors.log";
        private const string LogsFolderName = "Logs";

        private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);
        private readonly HashSet<string> _failedExpeditions = new(StringComparer.Ordinal);
        private readonly HashSet<string> _successfulExpeditions = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the recorded error messages keyed by expedition id.
        /// </summary>
        public IReadOnlyDictionary<string, List<string>> Errors => _errors;

        /// <summary>
        /// Gets the total number of successfully registered expeditions.
        /// </summary>
        public int SuccessCount => _successfulExpeditions.Count;

        /// <summary>
        /// Gets the total number of expeditions that failed to load.
        /// </summary>
        public int FailureCount => _failedExpeditions.Count;

        /// <summary>
        /// Records a failure message for the specified expedition id.
        /// </summary>
        public void RecordFailure(string expeditionId, string reason)
        {
            string resolvedId = ResolveExpeditionId(expeditionId);

            if (!_errors.TryGetValue(resolvedId, out List<string> messages))
            {
                messages = new List<string>();
                _errors[resolvedId] = messages;
            }

            if (!string.IsNullOrWhiteSpace(reason))
            {
                messages.Add(reason);
            }

            _failedExpeditions.Add(resolvedId);
            _successfulExpeditions.Remove(resolvedId);
        }

        /// <summary>
        /// Records a successful expedition registration.
        /// </summary>
        public void RecordSuccess(string expeditionId)
        {
            string resolvedId = ResolveExpeditionId(expeditionId);

            if (_failedExpeditions.Contains(resolvedId))
            {
                return;
            }

            _successfulExpeditions.Add(resolvedId);
        }

        /// <summary>
        /// Writes the diagnostics log to the mod Logs directory.
        /// </summary>
        public void WriteLog(Mod mod)
        {
            if (mod is null)
            {
                throw new ArgumentNullException(nameof(mod));
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            string logsDirectory = Path.Combine(Main.SavePath, "ModLoader", mod.Name, LogsFolderName);
            string logPath = Path.Combine(logsDirectory, LogFileName);

            try
            {
                Directory.CreateDirectory(logsDirectory);

                var builder = new StringBuilder();
                builder.AppendLine($"Expedition load diagnostics ({DateTimeOffset.Now:O})");
                builder.AppendLine($"Total processed: {SuccessCount + FailureCount}");
                builder.AppendLine($"Successes: {SuccessCount}");
                builder.AppendLine($"Failures: {FailureCount}");
                builder.AppendLine();

                if (FailureCount == 0)
                {
                    builder.AppendLine("All expeditions loaded successfully.");
                }
                else
                {
                    builder.AppendLine("Failed expeditions:");

                    foreach (var entry in _errors)
                    {
                        builder.AppendLine($"- {entry.Key}");
                        foreach (string message in entry.Value)
                        {
                            builder.AppendLine($"  - {message}");
                        }
                    }
                }

                File.WriteAllText(logPath, builder.ToString());
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Failed to write expedition diagnostics log to '{logPath}'.", ex);
            }
        }

        private static string ResolveExpeditionId(string expeditionId)
        {
            return string.IsNullOrWhiteSpace(expeditionId) ? "<unknown>" : expeditionId;
        }
    }
}
