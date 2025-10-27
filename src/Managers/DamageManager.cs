using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace FortniteHits.Managers;

public class DamageManager
{
    private readonly Dictionary<int, Dictionary<int, ShotgunData>> _shotgunData = new();
    private readonly Dictionary<int, List<ParticleInfo>> _damageParticles = new();
    private readonly float _distance;
    private int _tickCount = 0;

    private class ShotgunData
    {
        public int TotalDamage { get; set; }
        public bool IsCrit { get; set; }
        public Vector? Position { get; set; }
        public bool IsProcessing { get; set; }
        public int ProcessTick { get; set; }
    }

    private class ParticleInfo
    {
        public CParticleSystem? Particle { get; set; }
        public int RemoveTick { get; set; }
        public bool IsChild { get; set; }
        public int Digit { get; set; }
        public Vector Position { get; set; } = new(0, 0, 0);
        public bool IsCrit { get; set; }
        public bool IsRight { get; set; }
    }

    public DamageManager(float distance)
    {
        _distance = distance;
    }

    public void OnTick()
    {
        _tickCount++;

        if (_shotgunData.Count == 0 && _damageParticles.Count == 0)
            return;

        var attackersToRemove = new List<int>();
        foreach (var attackerKvp in _shotgunData)
        {
            var victimsToRemove = new List<int>();
            foreach (var victimKvp in attackerKvp.Value)
            {
                var data = victimKvp.Value;
                if (data.IsProcessing && _tickCount >= data.ProcessTick)
                {
                    if (data.TotalDamage > 0)
                    {
                        ShowDamageParticle(attackerKvp.Key, victimKvp.Key, data.TotalDamage, data.IsCrit, data.Position);
                    }
                    victimsToRemove.Add(victimKvp.Key);
                }
            }

            foreach (var victimSlot in victimsToRemove)
                attackerKvp.Value.Remove(victimSlot);

            if (attackerKvp.Value.Count == 0)
                attackersToRemove.Add(attackerKvp.Key);
        }

        foreach (var slot in attackersToRemove)
            _shotgunData.Remove(slot);

        var playersToRemove = new List<int>();
        foreach (var playerKvp in _damageParticles)
        {
            for (int i = playerKvp.Value.Count - 1; i >= 0; i--)
            {
                var particleInfo = playerKvp.Value[i];
                if (_tickCount >= particleInfo.RemoveTick)
                {
                    if (particleInfo.Particle?.IsValid == true)
                    {
                        particleInfo.Particle.Remove();
                    }
                    playerKvp.Value.RemoveAt(i);

                    if (!particleInfo.IsChild)
                    {
                        CreateChildParticle(
                            particleInfo.Digit,
                            particleInfo.Position,
                            particleInfo.IsCrit,
                            particleInfo.IsRight,
                            playerKvp.Value
                        );
                    }
                }
            }

            if (playerKvp.Value.Count == 0)
                playersToRemove.Add(playerKvp.Key);
        }

        foreach (var slot in playersToRemove)
            _damageParticles.Remove(slot);
    }

    public void ProcessDamage(int attackerSlot, int victimSlot, int damage, bool isHeadshot, string weapon, CCSPlayerController victim)
    {
        if (IsShotgunWeapon(weapon))
        {
            HandleShotgunDamage(attackerSlot, victimSlot, damage, isHeadshot, victim);
        }
        else
        {
            ShowDamageParticle(attackerSlot, victimSlot, damage, isHeadshot, null);
        }
    }

    public void CleanupPlayer(int slot)
    {
        _shotgunData.Remove(slot);

        if (_damageParticles.TryGetValue(slot, out var particles))
        {
            foreach (var particleInfo in particles)
            {
                if (particleInfo.Particle?.IsValid == true)
                {
                    particleInfo.Particle.Remove();
                }
            }
            particles.Clear();
            _damageParticles.Remove(slot);
        }
    }

    public void CleanupAllParticles()
    {
        foreach (var particles in _damageParticles.Values)
        {
            foreach (var particleInfo in particles)
            {
                if (particleInfo.Particle?.IsValid == true)
                {
                    particleInfo.Particle.Remove();
                }
            }
            particles.Clear();
        }
        _damageParticles.Clear();
    }

    private static bool IsShotgunWeapon(string weapon)
    {
        return weapon is "xm1014" or "nova" or "mag7" or "sawedoff";
    }

    private void HandleShotgunDamage(int attackerSlot, int victimSlot, int damage, bool isHeadshot, CCSPlayerController victim)
    {
        if (!_shotgunData.ContainsKey(attackerSlot))
            _shotgunData[attackerSlot] = new Dictionary<int, ShotgunData>();

        if (!_shotgunData[attackerSlot].ContainsKey(victimSlot))
            _shotgunData[attackerSlot][victimSlot] = new ShotgunData();

        var data = _shotgunData[attackerSlot][victimSlot];

        if (!data.IsProcessing)
        {
            data.IsProcessing = true;
            data.TotalDamage = damage;
            data.IsCrit = isHeadshot;
            data.ProcessTick = _tickCount + 6;

            if (victim?.PlayerPawn?.Value?.AbsOrigin != null)
            {
                var origin = victim.PlayerPawn.Value.AbsOrigin;
                data.Position = new Vector(origin.X, origin.Y, origin.Z);
            }
        }
        else
        {
            data.TotalDamage += damage;
            if (isHeadshot)
                data.IsCrit = true;
        }
    }

    private void ShowDamageParticle(int attackerSlot, int victimSlot, int damage, bool isCrit, Vector? storedPosition)
    {
        var attacker = Utilities.GetPlayerFromSlot(attackerSlot);
        var victim = Utilities.GetPlayerFromSlot(victimSlot);

        if (attacker?.PlayerPawn?.Value?.AbsOrigin == null || victim?.PlayerPawn?.Value == null)
            return;

        var victimPawn = victim.PlayerPawn.Value;
        if (victimPawn.AbsOrigin == null)
            return;

        Vector victimPos = storedPosition ?? victimPawn.AbsOrigin;

        var digits = GetDamageDigits(damage);
        var attackerPos = attacker.PlayerPawn.Value.AbsOrigin;
        var distance = CalculateDistance(attackerPos, victimPos);
        var spacing = distance > 700.0f ? (distance / 700.0f * 6.0f) : 6.0f;

        var random = new Random();
        var baseOffset = new Vector(
            (float)(random.NextDouble() - 0.5) * spacing,
            (float)(random.NextDouble() - 0.5) * spacing,
            0
        );

        bool isDucking = ((PlayerFlags)victimPawn.Flags).HasFlag(PlayerFlags.FL_DUCKING);
        float heightOffset = isCrit ? (isDucking ? 45.0f : 60.0f) : (isDucking ? 25.0f : 35.0f);
        heightOffset += (float)(random.NextDouble() * (isCrit ? 10.0f : 20.0f));

        var halfCount = (float)Math.Ceiling(digits.Count / 2.0);

        if (!_damageParticles.ContainsKey(attackerSlot))
            _damageParticles[attackerSlot] = new List<ParticleInfo>();

        for (int i = 0; i < digits.Count; i++)
        {
            var digitPos = new Vector(
                victimPos.X + baseOffset.X + (i - halfCount) * spacing,
                victimPos.Y + baseOffset.Y + (i - halfCount) * spacing,
                victimPos.Z + heightOffset
            );

            CreateDamageParticle(
                digits[digits.Count - 1 - i],
                digitPos,
                isCrit,
                i > halfCount,
                _damageParticles[attackerSlot]
            );
        }
    }

    private static List<int> GetDamageDigits(int damage)
    {
        if (damage == 0) return new List<int> { 0 };

        var digits = new List<int>();
        while (damage > 0)
        {
            digits.Add(damage % 10);
            damage /= 10;
        }
        return digits;
    }

    private void CreateDamageParticle(int digit, Vector position, bool isCrit, bool isRight, List<ParticleInfo> particleList)
    {
        try
        {
            var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
            if (particle == null) return;

            particle.EffectName = $"particles/kolka/fortnite_dmg_v2/kolka_damage_{digit}_{(isRight ? "fr" : "fl")}{(isCrit ? "_crit" : "")}.vpcf";
            particle.StartActive = true;
            particle.Teleport(position);
            particle.DispatchSpawn();

            particleList.Add(new ParticleInfo
            {
                Particle = particle,
                RemoveTick = _tickCount + 32,
                IsChild = false,
                Digit = digit,
                Position = position,
                IsCrit = isCrit,
                IsRight = isRight
            });
        }
        catch { }
    }

    private void CreateChildParticle(int digit, Vector position, bool isCrit, bool isRight, List<ParticleInfo> particleList)
    {
        try
        {
            var childParticle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
            if (childParticle == null) return;

            childParticle.EffectName = $"particles/kolka/fortnite_dmg_v2/kolka_damage_{digit}_{(isRight ? "fr" : "fl")}{(isCrit ? "_crit" : "")}_child.vpcf";
            childParticle.StartActive = true;
            childParticle.Teleport(position);
            childParticle.DispatchSpawn();

            particleList.Add(new ParticleInfo
            {
                Particle = childParticle,
                RemoveTick = _tickCount + 160,
                IsChild = true,
                Digit = digit,
                Position = position,
                IsCrit = isCrit,
                IsRight = isRight
            });
        }
        catch { }
    }

    private static float CalculateDistance(Vector pos1, Vector pos2)
    {
        float dx = pos1.X - pos2.X;
        float dy = pos1.Y - pos2.Y;
        float dz = pos1.Z - pos2.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}