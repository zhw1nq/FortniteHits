using CounterStrikeSharp.API.Core;

namespace FortniteHits.Managers;

public class PlayerManager
{
    private readonly Dictionary<int, bool> _hasAccess = new();
    private readonly Dictionary<int, bool> _playerEnabled = new();

    public void OnPlayerConnected(CCSPlayerController player)
    {
        if (player?.IsValid != true || player.IsBot)
            return;

        _hasAccess[player.Slot] = false;
        _playerEnabled[player.Slot] = true;
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
    public void ToggleEnabled(int slot) => _playerEnabled[slot] = !IsEnabled(slot);
}