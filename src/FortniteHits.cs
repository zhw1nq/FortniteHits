using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using FortniteHits.Config;
using FortniteHits.Managers;

namespace FortniteHits;

[MinimumApiVersion(344)]
public class FortniteHits : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "Fortnite Hits";
    public override string ModuleVersion => "1.3.1";
    public override string ModuleAuthor => "zhw1nq";
    public override string ModuleDescription => "Fortnite-style damage numbers display";

    public PluginConfig Config { get; set; } = new();

    private PlayerManager _playerManager = null!;
    private DamageManager _damageManager = null!;

    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
        _playerManager = new PlayerManager();
        _damageManager = new DamageManager(Config.Distance);
    }

    public override void Load(bool hotReload)
    {
        RegisterCommands();
        RegisterListener<Listeners.OnTick>(_damageManager.OnTick);
        Server.PrintToConsole("[FortniteHits] Plugin loaded");
    }

    public override void Unload(bool hotReload)
    {
        RemoveListener<Listeners.OnTick>(_damageManager.OnTick);
        _damageManager?.CleanupAllParticles();
    }

    private void RegisterCommands()
    {
        if (Config.Commands.Count == 0)
            return;

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
        if (player?.IsValid != true || player.IsBot)
            return;

        int slot = player.Slot;

        if (!Config.FreeAccess && !_playerManager.HasAccess(slot))
        {
            player.PrintToChat(Localizer["zFH_NoAccess"]);
            return;
        }

        _playerManager.ToggleEnabled(slot);

        player.PrintToChat(_playerManager.IsEnabled(slot)
            ? Localizer["zFH_Enable"]
            : Localizer["zFH_Disable"]);
    }

    [GameEventHandler]
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;

        if (attacker?.IsValid != true || victim?.IsValid != true || attacker.IsBot || attacker == victim)
            return HookResult.Continue;

        int attackerSlot = attacker.Slot;

        if (!Config.FreeAccess && !_playerManager.HasAccess(attackerSlot))
            return HookResult.Continue;

        if (!_playerManager.IsEnabled(attackerSlot))
            return HookResult.Continue;

        _damageManager.ProcessDamage(
            attackerSlot,
            victim.Slot,
            @event.DmgHealth,
            @event.Hitgroup == 1,
            @event.Weapon,
            victim
        );

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player?.IsValid == true && !player.IsBot)
        {
            _playerManager.OnPlayerConnected(player);
        }
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player != null)
        {
            _playerManager.OnPlayerDisconnected(player.Slot);
            _damageManager.CleanupPlayer(player.Slot);
        }
        return HookResult.Continue;
    }

    public void GiveClientAccess(CCSPlayerController player)
    {
        if (player?.IsValid == true)
            _playerManager.GiveAccess(player.Slot);
    }

    public void TakeClientAccess(CCSPlayerController player)
    {
        if (player?.IsValid == true)
            _playerManager.TakeAccess(player.Slot);
    }

    public bool HasClientAccess(CCSPlayerController player)
    {
        return player?.IsValid == true && (Config.FreeAccess || _playerManager.HasAccess(player.Slot));
    }

    public bool IsClientEnabled(CCSPlayerController player)
    {
        return player?.IsValid == true && _playerManager.IsEnabled(player.Slot);
    }

    public void SetClientEnabled(CCSPlayerController player, bool enabled)
    {
        if (player?.IsValid == true)
        {
            _playerManager.SetEnabled(player.Slot, enabled);
        }
    }
}