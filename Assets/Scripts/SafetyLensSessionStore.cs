using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Entrada do histórico: caminhos locais + registro já desserializado.
    /// </summary>
    public sealed class SafetyLensSessionEntry
    {
        public string JsonPath;
        public string CsvPath;
        public SafetyLensSessionRecord Record;
    }

    /// <summary>
    /// Persistência local offline (RF06 / RNF05): grava JSON/CSV e lista sessões para o histórico.
    /// </summary>
    public static class SafetyLensSessionStore
    {
        const string FilePrefix = "safetylens_";

        public static string LastExportDirectory { get; private set; } = string.Empty;
        public static string LastJsonPath { get; private set; } = string.Empty;
        public static string LastCsvPath { get; private set; } = string.Empty;

        public static void Export(SafetyLensSessionRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var safeId = SanitizeFilePart(record.participantId);
            var dir = Application.persistentDataPath;
            Directory.CreateDirectory(dir);

            record.exportedAtIso = DateTime.UtcNow.ToString("o");

            LastJsonPath = Path.Combine(dir, $"{FilePrefix}{safeId}_{stamp}.json");
            LastCsvPath = Path.Combine(dir, $"{FilePrefix}{safeId}_{stamp}.csv");
            LastExportDirectory = dir;

            File.WriteAllText(LastJsonPath, JsonUtility.ToJson(record, true), Encoding.UTF8);
            File.WriteAllText(LastCsvPath, BuildCsv(new[] { record }), Encoding.UTF8);

            Debug.Log($"[SafetyLens] Sessão exportada.\nJSON: {LastJsonPath}\nCSV: {LastCsvPath}");
        }

        /// <summary>
        /// Lê todos os JSON salvos (mais recentes primeiro). Ignora arquivos corrompidos.
        /// </summary>
        public static List<SafetyLensSessionEntry> ListSessions()
        {
            var result = new List<SafetyLensSessionEntry>();
            var dir = Application.persistentDataPath;
            if (!Directory.Exists(dir)) return result;

            string[] files;
            try
            {
                files = Directory.GetFiles(dir, $"{FilePrefix}*.json");
            }
            catch (IOException ex)
            {
                Debug.LogWarning($"[SafetyLens] Falha ao listar sessões: {ex.Message}");
                return result;
            }

            foreach (var jsonPath in files)
            {
                try
                {
                    var record = JsonUtility.FromJson<SafetyLensSessionRecord>(File.ReadAllText(jsonPath, Encoding.UTF8));
                    if (record == null || string.IsNullOrWhiteSpace(record.participantId)) continue;

                    var csvPath = Path.ChangeExtension(jsonPath, ".csv");
                    // Regenera CSV se o JSON existir mas o CSV tiver sido apagado (ex.: limpeza parcial).
                    if (!File.Exists(csvPath))
                    {
                        File.WriteAllText(csvPath, BuildCsv(new[] { record }), Encoding.UTF8);
                    }

                    result.Add(new SafetyLensSessionEntry
                    {
                        JsonPath = jsonPath,
                        CsvPath = csvPath,
                        Record = record
                    });
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
                {
                    Debug.LogWarning($"[SafetyLens] JSON ignorado ({Path.GetFileName(jsonPath)}): {ex.Message}");
                }
            }

            result.Sort(CompareNewestFirst);
            return result;
        }

        /// <summary>
        /// Gera um CSV com todas as sessões (uma linha por sessão) para o pesquisador exportar de uma vez.
        /// </summary>
        public static string WriteAggregateCsv(IReadOnlyList<SafetyLensSessionEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                throw new InvalidOperationException("Nenhuma sessao para exportar.");

            var records = new SafetyLensSessionRecord[entries.Count];
            for (var i = 0; i < entries.Count; i++)
                records[i] = entries[i].Record;

            var path = Path.Combine(
                Application.persistentDataPath,
                $"{FilePrefix}todas_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            File.WriteAllText(path, BuildCsv(records), Encoding.UTF8);
            return path;
        }

        public static bool CanShareLastCsv() =>
            !string.IsNullOrEmpty(LastCsvPath) && File.Exists(LastCsvPath);

        /// <summary>Apaga o par JSON/CSV da sessão. Não toca em outros arquivos.</summary>
        public static bool DeleteSession(SafetyLensSessionEntry entry)
        {
            if (entry == null) return false;
            var ok = true;
            ok &= TryDeleteFile(entry.JsonPath);
            ok &= TryDeleteFile(entry.CsvPath);
            if (!string.IsNullOrEmpty(LastJsonPath)
                && string.Equals(LastJsonPath, entry.JsonPath, StringComparison.Ordinal))
            {
                LastJsonPath = string.Empty;
                LastCsvPath = string.Empty;
            }

            return ok;
        }

        static bool TryDeleteFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return true;
            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[SafetyLens] Falha ao apagar {path}: {ex.Message}");
                return false;
            }
        }

        static int CompareNewestFirst(SafetyLensSessionEntry a, SafetyLensSessionEntry b)
        {
            var aKey = a.Record.finishedAtIso ?? a.Record.exportedAtIso ?? string.Empty;
            var bKey = b.Record.finishedAtIso ?? b.Record.exportedAtIso ?? string.Empty;
            var cmp = string.CompareOrdinal(bKey, aKey);
            if (cmp != 0) return cmp;
            return string.CompareOrdinal(b.JsonPath, a.JsonPath);
        }

        /// <summary>Exposto a testes EditMode via InternalsVisibleTo.</summary>
        internal static string BuildCsv(IReadOnlyList<SafetyLensSessionRecord> records)
        {
            var sb = new StringBuilder();
            // Colunas novas sempre no fim: CSVs antigos continuam legíveis pelas mesmas posições.
            sb.AppendLine(
                "participantId,scenarioId,scenarioTitle,elapsedSeconds,errorCount,hazardsConfirmed,hazardsTotal,quizCorrect,quizTotal,quizAnswers,startedAtIso,finishedAtIso,exportedAtIso,inspectionSeconds,quizSeconds,fpsAverage,fpsLow1Percent,framesBelow30Ratio,peakMemoryMb,markerName");
            foreach (var record in records)
            {
                if (record == null) continue;
                var answers = record.quizAnswers == null ? string.Empty : string.Join("|", record.quizAnswers);
                // InvariantCulture: em pt-BR, F2 viraria "45,5" e quebraria o delimitador CSV.
                sb.Append(Escape(record.participantId)).Append(',')
                    .Append(Escape(record.scenarioId)).Append(',')
                    .Append(Escape(record.scenarioTitle)).Append(',')
                    .Append(record.elapsedSeconds.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                    .Append(record.errorCount).Append(',')
                    .Append(record.hazardsConfirmed).Append(',')
                    .Append(record.hazardsTotal).Append(',')
                    .Append(record.quizCorrect).Append(',')
                    .Append(record.quizTotal).Append(',')
                    .Append(Escape(answers)).Append(',')
                    .Append(Escape(record.startedAtIso)).Append(',')
                    .Append(Escape(record.finishedAtIso)).Append(',')
                    .Append(Escape(record.exportedAtIso)).Append(',')
                    .Append(record.inspectionSeconds.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                    .Append(record.quizSeconds.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                    .Append(record.fpsAverage.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                    .Append(record.fpsLow1Percent.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                    .Append(record.framesBelow30Ratio.ToString("F4", CultureInfo.InvariantCulture)).Append(',')
                    .Append(record.peakMemoryMb.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                    .Append(Escape(record.markerName))
                    .AppendLine();
            }
            return sb.ToString();
        }

        internal static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        /// <summary>Prefixo do arquivo de export. Diferente de <see cref="SafetyLensSessionController.SanitizeParticipantId"/>.</summary>
        internal static string SanitizeFilePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "anon";
            var chars = value.Trim().ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '-' && chars[i] != '_')
                    chars[i] = '_';
            }
            return new string(chars);
        }
    }
}
