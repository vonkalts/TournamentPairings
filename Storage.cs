using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TournamentPairings
{
    public static class Storage
    {
        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

        // ✅ Auto-clean limit
        private const int MaxAutosaveSnapshotsToKeep = 30;

        public static string AppDataDir
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TournamentPairings");

        public static string AutosaveDir
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TournamentPairings", "Autosaves");

        public static void EnsureFolders()
        {
            Directory.CreateDirectory(AppDataDir);
            Directory.CreateDirectory(AutosaveDir);
        }

        public static TournamentState LoadTournament(string path)
        {
            var json = File.ReadAllText(path);
            var state = JsonSerializer.Deserialize<TournamentState>(json, JsonOpts)
                        ?? throw new InvalidDataException("Invalid tournament file.");

            state.FilePath = path;
            state.IsDirty = false;
            return state;
        }

        public static void SaveTournament(TournamentState state, string path)
        {
            EnsureFolders();

            // normal save: set file path & clear dirty
            state.FilePath = path;

            var json = JsonSerializer.Serialize(state, JsonOpts);
            File.WriteAllText(path, json);

            state.LastSavedUtc = DateTime.UtcNow;
            state.IsDirty = false;
        }

        public static string BuildAutosavePath(TournamentState state)
        {
            EnsureFolders();
            var safeName = MakeFileSafe(state.TournamentName);
            var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return Path.Combine(AutosaveDir, $"{safeName}_{stamp}.tournament");
        }

        /// <summary>
        /// Writes an autosave snapshot WITHOUT changing state.FilePath / state.IsDirty / state.LastSavedUtc.
        /// This avoids the autosave behaving like a normal Save/SaveAs.
        /// </summary>
        public static void AutosaveSnapshot(TournamentState state)
        {
            EnsureFolders();

            var path = BuildAutosavePath(state);

            // Snapshot save should not mutate the state.
            var json = JsonSerializer.Serialize(state, JsonOpts);
            File.WriteAllText(path, json);

            // ✅ auto-clean (keep last N)
            TryCleanupAutosaves(MaxAutosaveSnapshotsToKeep);
        }

        /// <summary>
        /// Keeps only the newest 'keepLast' autosaves in the Autosaves folder.
        /// Deletes older files (best effort; ignores locked files/errors).
        /// </summary>
        public static void TryCleanupAutosaves(int keepLast)
        {
            if (keepLast < 1) keepLast = 1;

            try
            {
                EnsureFolders();

                var files = Directory.GetFiles(AutosaveDir, "*.tournament", SearchOption.TopDirectoryOnly)
                                     .Select(f => new FileInfo(f))
                                     .OrderByDescending(fi => fi.LastWriteTimeUtc) // newest first
                                     .ToList();

                if (files.Count <= keepLast) return;

                foreach (var fi in files.Skip(keepLast))
                {
                    try
                    {
                        fi.Delete();
                    }
                    catch
                    {
                        // ignore (file locked, permissions, etc.)
                    }
                }
            }
            catch
            {
                // ignore any cleanup failure; autosave should never crash the app
            }
        }

        public static string MakeFileSafe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Tournament";
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
        }
    }

    public sealed class AppSettings
    {
        public List<string> RecentFiles { get; set; } = new();

        public static string SettingsPath
            => Path.Combine(Storage.AppDataDir, "settings.json");

        public static AppSettings Load()
        {
            Storage.EnsureFolders();
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }

        public void Save()
        {
            Storage.EnsureFolders();
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }

        public void AddRecent(string path)
        {
            path = Path.GetFullPath(path);

            RecentFiles.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            RecentFiles.Insert(0, path);

            if (RecentFiles.Count > 10)
                RecentFiles.RemoveRange(10, RecentFiles.Count - 10);

            Save();
        }
    }
}