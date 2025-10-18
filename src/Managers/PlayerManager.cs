using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Text.Json;

namespace FortniteHits.Managers;

public class PlayerManager
{
    private readonly Dictionary<ulong, bool> _playerSettings = new();
    private readonly Dictionary<int, bool> _hasAccess = new();
    private readonly Dictionary<int, bool> _playerEnabled = new();
    private readonly string _dataPath;

    public PlayerManager()
    {
        _dataPath = Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "plugins", "FortniteHits", "data", "players.json");
    }

    public void OnPlayerConnected(CCSPlayerController player)
    {
        if (player?.IsValid != true || player.IsBot)
            return;

        int slot = player.Slot;
        _hasAccess[slot] = false;
        _playerEnabled[slot] = LoadPlayerSetting(player);
    }

    public void OnPlayerDisconnected(int slot)
    {
        _hasAccess.Remove(slot);
        _playerEnabled.Remove(slot);
    }

    public bool HasAccess(int slot) => _hasAccess.GetValueOrDefault(slot, false);
    public void GiveAccess(int slot) => _hasAccess[slot] = true;
    public void TakeAccess(int slot) => _hasAccess[slot] = false;
    public bool IsEnabled(int slot) => _playerEnabled.GetValueOrDefault(slot, true);
    public void SetEnabled(int slot, bool enabled) => _playerEnabled[slot] = enabled;
    public void ToggleEnabled(int slot) => _playerEnabled[slot] = !_playerEnabled.GetValueOrDefault(slot, true);

    public void LoadSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(_dataPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            if (File.Exists(_dataPath))
            {
                var json = File.ReadAllText(_dataPath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                if (settings != null)
                {
                    _playerSettings.Clear();
                    foreach (var kvp in settings)
                    {
                        if (ulong.TryParse(kvp.Key, out ulong steamId))
                            _playerSettings[steamId] = kvp.Value;
                    }
                }
            }
        }
        catch { }
    }

    public void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(_dataPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            var settings = _playerSettings.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dataPath, json);
        }
        catch { }
    }

    public bool LoadPlayerSetting(CCSPlayerController player)
    {
        return player?.SteamID != null && _playerSettings.GetValueOrDefault(player.SteamID, true);
    }

    public void SavePlayerSetting(CCSPlayerController player, bool enabled)
    {
        if (player?.SteamID == null)
            return;

        _playerSettings[player.SteamID] = enabled;
        SaveSettings();
    }
}