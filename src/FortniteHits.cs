using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Cvars;
using FortniteHits.Config;
using FortniteHits.Managers;
using FortniteHits.Utils;

namespace FortniteHits;

[MinimumApiVersion(180)]
public class FortniteHits : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "Fortnite Hits";
    public override string ModuleVersion => "1.2.0";
    public override string ModuleAuthor => "zhw1nq, Pisex (refactored)";
    public override string ModuleDescription => "Fortnite-style damage numbers display";

    public PluginConfig Config { get; set; } = new();

    private readonly Localizer _localizer = new();
    private PlayerManager _playerManager = null!;
    private DamageManager _damageManager = null!;

    private string _pluginPath = string.Empty;

    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
        _pluginPath = Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "plugins", "FortniteHits");

        // Initialize managers
        _playerManager = new PlayerManager();
        _damageManager = new DamageManager(Config.Distance);

        // Load language based on server language or default to "en"
        string serverLanguage = "en"; // Default fallback since Server.Language is not available

        // Try to get language from ConVar if available
        try
        {
            var languageCvar = ConVar.Find("sv_lan");
            if (languageCvar != null)
            {
                // You might want to implement your own language detection logic here
                // For now, using default "en"
            }
        }
        catch
        {
            // Fallback to default language
        }

        _localizer.LoadLanguage(_pluginPath, serverLanguage);

        // Load player settings
        _playerManager.LoadSettings();
    }

    public override void Load(bool hotReload)
    {
        RegisterCommands();
        Console.WriteLine("[FortniteHits] Plugin loaded successfully!");
    }

    public override void Unload(bool hotReload)
    {
        _playerManager?.SaveSettings();
        _damageManager?.CleanupAllParticles();

        Console.WriteLine("[FortniteHits] Plugin unloaded!");
    }

    #region Commands

    private void RegisterCommands()
    {
        // Only register commands if the commands list is not empty
        if (Config.Commands == null || Config.Commands.Count == 0)
        {
            Console.WriteLine("[FortniteHits] No commands configured - toggle feature disabled");
            return;
        }

        foreach (var command in Config.Commands)
        {
            if (!string.IsNullOrWhiteSpace(command))
            {
                AddCommand($"css_{command}", "Toggle Fortnite hits display", OnToggleCommand);
            }
        }
    }

    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void OnToggleCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        int slot = player.Slot;

        if (!Config.FreeAccess && !_playerManager.HasAccess(slot))
        {
            player.PrintToChat(_localizer.GetPhrase("zFH_NoAccess"));
            return;
        }

        _playerManager.ToggleEnabled(slot);

        if (_playerManager.IsEnabled(slot))
        {
            player.PrintToChat(_localizer.GetPhrase("zFH_Enable"));
        }
        else
        {
            player.PrintToChat(_localizer.GetPhrase("zFH_Disable"));
        }

        _playerManager.SavePlayerSetting(player, _playerManager.IsEnabled(slot));
    }

    #endregion

    #region Events

    [GameEventHandler]
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;

        if (attacker == null || victim == null || !attacker.IsValid || !victim.IsValid)
            return HookResult.Continue;

        if (attacker.IsBot || attacker == victim)
            return HookResult.Continue;

        int attackerSlot = attacker.Slot;
        int victimSlot = victim.Slot;

        if (!Config.FreeAccess && !_playerManager.HasAccess(attackerSlot))
            return HookResult.Continue;

        if (!_playerManager.IsEnabled(attackerSlot))
            return HookResult.Continue;

        int damage = @event.DmgHealth;
        int hitgroup = @event.Hitgroup;
        string weapon = @event.Weapon;
        bool isHeadshot = hitgroup == 1; // HITGROUP_HEAD

        _damageManager.ProcessDamage(attackerSlot, victimSlot, damage, isHeadshot, weapon, victim);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        _playerManager.OnPlayerConnected(player);
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null)
            return HookResult.Continue;

        int slot = player.Slot;

        _playerManager.OnPlayerDisconnected(slot);
        _damageManager.CleanupPlayer(slot);

        return HookResult.Continue;
    }

    #endregion

    #region API Methods

    /// <summary>
    /// Give access to FortniteHits features for a specific player
    /// </summary>
    /// <param name="player">The player to give access to</param>
    public void GiveClientAccess(CCSPlayerController player)
    {
        if (player?.IsValid == true)
        {
            _playerManager.GiveAccess(player.Slot);
        }
    }

    /// <summary>
    /// Remove access to FortniteHits features for a specific player
    /// </summary>
    /// <param name="player">The player to remove access from</param>
    public void TakeClientAccess(CCSPlayerController player)
    {
        if (player?.IsValid == true)
        {
            _playerManager.TakeAccess(player.Slot);
        }
    }

    /// <summary>
    /// Check if a player has access to FortniteHits features
    /// </summary>
    /// <param name="player">The player to check</param>
    /// <returns>True if player has access, false otherwise</returns>
    public bool HasClientAccess(CCSPlayerController player)
    {
        if (player?.IsValid != true)
            return false;

        return Config.FreeAccess || _playerManager.HasAccess(player.Slot);
    }

    /// <summary>
    /// Check if a player has FortniteHits enabled
    /// </summary>
    /// <param name="player">The player to check</param>
    /// <returns>True if enabled, false otherwise</returns>
    public bool IsClientEnabled(CCSPlayerController player)
    {
        if (player?.IsValid != true)
            return false;

        return _playerManager.IsEnabled(player.Slot);
    }

    /// <summary>
    /// Enable or disable FortniteHits for a specific player
    /// </summary>
    /// <param name="player">The player</param>
    /// <param name="enabled">True to enable, false to disable</param>
    public void SetClientEnabled(CCSPlayerController player, bool enabled)
    {
        if (player?.IsValid != true)
            return;

        _playerManager.SetEnabled(player.Slot, enabled);
        _playerManager.SavePlayerSetting(player, enabled);
    }

    #endregion
}