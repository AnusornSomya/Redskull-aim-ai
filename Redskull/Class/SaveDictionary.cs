using Newtonsoft.Json;
using Other;
using System.IO;
using MessageBox = System.Windows.MessageBox;

namespace Class
{
    internal class SaveDictionary
    {
        private const string DefaultModelName = "1.5kR6.onnx";

        // Ensure all required directories exist at startup
        public static void EnsureDirectoriesExist()
        {
            var requiredDirectories = new[]
            {
                "bin",
                "bin\\configs",
                "bin\\labels",
                "bin\\models"
            };

            foreach (var dir in requiredDirectories)
            {
                if (!Directory.Exists(dir))
                {
                    try
                    {
                        Directory.CreateDirectory(dir);
                    }
                    catch (Exception ex)
                    {
                        LogManager.Log(LogManager.LogLevel.Error, $"Failed to create directory {dir}: {ex.Message}", true);
                    }
                }
            }

            EnsureDefaultModelExists();
        }

        private static void EnsureDefaultModelExists()
        {
            var targetPath = Path.Combine("bin", "models", DefaultModelName);
            if (File.Exists(targetPath))
            {
                return;
            }

            try
            {
                var candidatePaths = GetDefaultModelCandidatePaths();
                var sourcePath = candidatePaths.FirstOrDefault(File.Exists);

                if (string.IsNullOrEmpty(sourcePath))
                {
                    LogManager.Log(LogManager.LogLevel.Warning,
                        $"Default model '{DefaultModelName}' was not found in any known source path.", true);
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.Copy(sourcePath, targetPath, true);
                LogManager.Log(LogManager.LogLevel.Info,
                    $"Default model provisioned: {DefaultModelName}", false);
            }
            catch (Exception ex)
            {
                LogManager.Log(LogManager.LogLevel.Error,
                    $"Failed to provision default model '{DefaultModelName}': {ex.Message}", true);
            }
        }

        private static IEnumerable<string> GetDefaultModelCandidatePaths()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Path.Combine(Directory.GetCurrentDirectory(), "models", DefaultModelName),
                Path.Combine(baseDirectory, "models", DefaultModelName),
                Path.Combine(baseDirectory, "bin", "models", DefaultModelName)
            };

            var current = new DirectoryInfo(baseDirectory);
            for (int depth = 0; depth < 8 && current != null; depth++, current = current.Parent)
            {
                candidates.Add(Path.Combine(current.FullName, "models", DefaultModelName));
            }

            return candidates;
        }
        public static void WriteJSON(Dictionary<string, dynamic> dictionary, string path = "bin\\configs\\Default.cfg", string SuggestedModel = "", string ExtraStrings = "")
        {
            try
            {
                // Ensure the directory exists
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var SavedJSONSettings = new Dictionary<string, dynamic>(dictionary);
                if (!string.IsNullOrEmpty(SuggestedModel) && SavedJSONSettings.ContainsKey("Suggested Model"))
                {
                    SavedJSONSettings["Suggested Model"] = SuggestedModel + ".onnx" + ExtraStrings;
                }

                string json = JsonConvert.SerializeObject(SavedJSONSettings, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                // Only show error if it's not a directory creation issue
                MessageBox.Show($"Error writing JSON, please note:\n{ex}");
            }
        }

        public static void LoadJSON(Dictionary<string, dynamic> dictionary, string path = "bin\\configs\\Default.cfg", bool strict = true)
        {
            try
            {
                // Ensure the directory exists before checking for the file
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(path))
                {
                    WriteJSON(dictionary, path);
                    return;
                }

                var configuration = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(File.ReadAllText(path));
                if (configuration == null) return;

                foreach (var (key, value) in configuration)
                {
                    if (dictionary.ContainsKey(key))
                    {
                        dictionary[key] = value;
                    }
                    else if (!strict)
                    {
                        dictionary.Add(key, value);
                    }
                }
            }
            catch (Exception ex)
            {
                // If there's an error loading, try to recreate the file with defaults
                try
                {
                    WriteJSON(dictionary, path);
                }
                catch
                {
                    // Only show error if we can't even create a default file
                    MessageBox.Show("Error loading JSON, please note:\n" + ex.ToString());
                }
            }
        }
    }
}
