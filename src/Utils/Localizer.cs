using CounterStrikeSharp.API.Modules.Utils;

namespace FortniteHits.Utils;

public class Localizer
{
    private readonly Dictionary<string, string> _phrases = new();
    private readonly string _defaultLanguage = "en";
    private string _currentLanguage = "en";

    public void LoadLanguage(string pluginPath, string language)
    {
        _phrases.Clear();
        _currentLanguage = language;

        string langPath = Path.Combine(pluginPath, "langs", $"{language}.json");
        string defaultLangPath = Path.Combine(pluginPath, "langs", $"{_defaultLanguage}.json");

        // Try to load requested language
        if (File.Exists(langPath))
        {
            LoadLanguageFile(langPath);
        }
        // Fallback to default language
        else if (File.Exists(defaultLangPath))
        {
            LoadLanguageFile(defaultLangPath);
            Console.WriteLine($"[FortniteHits] Language '{language}' not found, using default '{_defaultLanguage}'");
        }
        else
        {
            Console.WriteLine($"[FortniteHits] No language files found! Please create {defaultLangPath}");
        }
    }

    private void LoadLanguageFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var phrases = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (phrases != null)
            {
                foreach (var kvp in phrases)
                {
                    _phrases[kvp.Key] = kvp.Value;
                }
            }
            Console.WriteLine($"[FortniteHits] Loaded {_phrases.Count} phrases from {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FortniteHits] Error loading language file {path}: {ex.Message}");
        }
    }

    public string GetPhrase(string key, params object[] args)
    {
        if (_phrases.TryGetValue(key, out string? phrase))
        {
            try
            {
                // Replace color codes
                phrase = ReplaceColorCodes(phrase);

                // Add prefix if not already present and key is not the prefix itself
                if (key != "zFH_Prefix" && !phrase.Contains("{prefix}"))
                {
                    string prefix = GetPhrase("zFH_Prefix");
                    phrase = $"{prefix}{phrase}";
                }
                else if (phrase.Contains("{prefix}"))
                {
                    string prefix = GetPhrase("zFH_Prefix");
                    phrase = phrase.Replace("{prefix}", prefix);
                }

                return args.Length > 0 ? string.Format(phrase, args) : phrase;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FortniteHits] Error formatting phrase '{key}': {ex.Message}");
                return phrase;
            }
        }
        return key; // Return key if phrase not found
    }

    private static string ReplaceColorCodes(string text)
    {
        return text
            .Replace("{default}", ChatColors.Default.ToString())
            .Replace("{white}", ChatColors.White.ToString())
            .Replace("{darkred}", ChatColors.DarkRed.ToString())
            .Replace("{green}", ChatColors.Green.ToString())
            .Replace("{lightyellow}", ChatColors.LightYellow.ToString())
            .Replace("{lightblue}", ChatColors.LightBlue.ToString())
            .Replace("{olive}", ChatColors.Olive.ToString())
            .Replace("{lime}", ChatColors.Lime.ToString())
            .Replace("{red}", ChatColors.Red.ToString())
            .Replace("{purple}", ChatColors.Purple.ToString())
            .Replace("{grey}", ChatColors.Grey.ToString())
            .Replace("{yellow}", ChatColors.Yellow.ToString())
            .Replace("{gold}", ChatColors.Gold.ToString())
            .Replace("{silver}", ChatColors.Silver.ToString())
            .Replace("{blue}", ChatColors.Blue.ToString())
            .Replace("{darkblue}", ChatColors.DarkBlue.ToString())
            .Replace("{bluegrey}", ChatColors.BlueGrey.ToString())
            .Replace("{magenta}", ChatColors.Magenta.ToString())
            .Replace("{lightred}", ChatColors.LightRed.ToString());
    }
}