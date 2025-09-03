using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace FortniteHits.Managers;

public class DamageManager
{
    // Shotgun damage tracking
    private readonly Dictionary<int, bool> _isFired = new();
    private readonly Dictionary<int, Dictionary<int, int>> _totalSGDamage = new();
    private readonly Dictionary<int, Dictionary<int, bool>> _isCrit = new();
    private readonly Dictionary<int, Vector> _playerPosLate = new();

    // Particle tracking
    private readonly Dictionary<int, List<CParticleSystem>> _damageParticles = new();

    private readonly float _distance;

    public DamageManager(float distance)
    {
        _distance = distance;

        // Initialize collections for all slots
        for (int i = 0; i < 64; i++)
        {
            _isFired[i] = false;
            _totalSGDamage[i] = new Dictionary<int, int>();
            _isCrit[i] = new Dictionary<int, bool>();
            _damageParticles[i] = new List<CParticleSystem>();
        }
    }

    public void ProcessDamage(int attackerSlot, int victimSlot, int damage, bool isHeadshot, string weapon, CCSPlayerController victim)
    {
        if (IsShotgunWeapon(weapon))
        {
            HandleShotgunDamage(attackerSlot, victimSlot, damage, isHeadshot, victim);
        }
        else
        {
            ShowDamageParticle(attackerSlot, victimSlot, damage, isHeadshot, false);
        }
    }

    public void CleanupPlayer(int slot)
    {
        _isFired.Remove(slot);
        _totalSGDamage.Remove(slot);
        _isCrit.Remove(slot);
        _playerPosLate.Remove(slot);

        // Clean up particles
        if (_damageParticles.ContainsKey(slot))
        {
            foreach (var particle in _damageParticles[slot].ToList())
            {
                if (particle?.IsValid == true)
                {
                    particle.Remove();
                }
            }
            _damageParticles[slot].Clear();
        }
    }

    public void CleanupAllParticles()
    {
        foreach (var playerParticles in _damageParticles.Values)
        {
            foreach (var particle in playerParticles.ToList())
            {
                if (particle?.IsValid == true)
                {
                    particle.Remove();
                }
            }
            playerParticles.Clear();
        }
    }

    private static bool IsShotgunWeapon(string weapon)
    {
        return weapon switch
        {
            "xm1014" or "nova" or "mag7" or "sawedoff" => true,
            _ => false
        };
    }

    private void HandleShotgunDamage(int attackerSlot, int victimSlot, int damage, bool isHeadshot, CCSPlayerController victim)
    {
        if (!_totalSGDamage[attackerSlot].ContainsKey(victimSlot))
            _totalSGDamage[attackerSlot][victimSlot] = 0;

        if (!_isCrit[attackerSlot].ContainsKey(victimSlot))
            _isCrit[attackerSlot][victimSlot] = false;

        if (!_isFired[attackerSlot])
        {
            _isFired[attackerSlot] = true;
            _totalSGDamage[attackerSlot][victimSlot] = damage;

            // Store victim position for late display
            if (victim?.PlayerPawn?.IsValid == true && victim.PlayerPawn.Value?.AbsOrigin != null)
            {
                _playerPosLate[victimSlot] = victim.PlayerPawn.Value.AbsOrigin;
            }

            // Schedule damage display after 0.1 seconds
            Server.NextFrame(() =>
            {
                Server.NextWorldUpdate(() =>
                {
                    Task.Delay(100).ContinueWith(_ =>
                    {
                        Server.NextFrame(() =>
                        {
                            _isFired[attackerSlot] = false;

                            foreach (var kvp in _totalSGDamage[attackerSlot].ToList())
                            {
                                int victim = kvp.Key;
                                int totalDamage = kvp.Value;

                                if (totalDamage > 0)
                                {
                                    bool wasCrit = _isCrit[attackerSlot].GetValueOrDefault(victim, false);
                                    ShowDamageParticle(attackerSlot, victim, totalDamage, wasCrit, true);

                                    _totalSGDamage[attackerSlot][victim] = 0;
                                    _isCrit[attackerSlot][victim] = false;
                                }
                            }
                        });
                    });
                });
            });
        }
        else
        {
            _totalSGDamage[attackerSlot][victimSlot] += damage;
        }

        if (isHeadshot)
        {
            _isCrit[attackerSlot][victimSlot] = true;
        }
    }

    private void ShowDamageParticle(int attackerSlot, int victimSlot, int damage, bool isCrit, bool useLatePosition)
    {
        var attacker = Utilities.GetPlayerFromSlot(attackerSlot);
        var victim = Utilities.GetPlayerFromSlot(victimSlot);

        if (attacker?.PlayerPawn?.IsValid != true || victim?.PlayerPawn?.IsValid != true)
            return;

        var attackerPawn = attacker.PlayerPawn.Value;
        var victimPawn = victim.PlayerPawn.Value;

        if (attackerPawn?.AbsOrigin == null || victimPawn?.AbsOrigin == null)
            return;

        Vector victimPos;
        if (useLatePosition && _playerPosLate.ContainsKey(victimSlot))
        {
            victimPos = _playerPosLate[victimSlot];
        }
        else
        {
            victimPos = victimPawn.AbsOrigin;
        }

        // Convert damage to individual digits
        var digits = GetDamageDigits(damage);
        var digitCount = digits.Count;

        // Calculate positioning
        var attackerPos = attackerPawn.AbsOrigin;
        var distance = CalculateDistance(attackerPos, victimPos);
        var spacing = distance > 700.0f ? (distance / 700.0f * 6.0f) : 6.0f;

        // Get random positioning offsets
        var random = new Random();
        var baseOffset = new Vector(
            (float)(random.NextDouble() - 0.5) * spacing,
            (float)(random.NextDouble() - 0.5) * spacing,
            0
        );

        // Check if victim is ducking - convert to PlayerFlags enum for HasFlag
        bool isDucking = ((PlayerFlags)victimPawn.Flags).HasFlag(PlayerFlags.FL_DUCKING);
        float heightOffset = isCrit ? (isDucking ? 45.0f : 60.0f) : (isDucking ? 25.0f : 35.0f);
        heightOffset += (float)(random.NextDouble() * (isCrit ? 10.0f : 20.0f));

        // Create particles for each digit
        var halfCount = (float)Math.Ceiling(digitCount / 2.0);

        for (int i = 0; i < digitCount; i++)
        {
            var digitPos = new Vector(
                victimPos.X + baseOffset.X + (i - halfCount) * spacing,
                victimPos.Y + baseOffset.Y + (i - halfCount) * spacing,
                victimPos.Z + heightOffset
            );

            CreateDamageParticle(attackerSlot, digits[digitCount - 1 - i], digitPos, isCrit, i > halfCount);
        }
    }

    private static List<int> GetDamageDigits(int damage)
    {
        var digits = new List<int>();
        if (damage == 0)
        {
            digits.Add(0);
        }
        else
        {
            while (damage > 0)
            {
                digits.Add(damage % 10);
                damage /= 10;
            }
        }
        return digits;
    }

    private void CreateDamageParticle(int attackerSlot, int digit, Vector position, bool isCrit, bool isRight)
    {
        try
        {
            var particleSystem = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
            if (particleSystem == null)
                return;

            string particleName = $"particles/kolka/fortnite_dmg_v2/kolka_damage_{digit}_{(isRight ? "fr" : "fl")}{(isCrit ? "_crit" : "")}.vpcf";

            particleSystem.EffectName = particleName;
            particleSystem.StartActive = true;
            particleSystem.Teleport(position);
            particleSystem.DispatchSpawn();

            // Track particle for cleanup
            if (!_damageParticles.ContainsKey(attackerSlot))
                _damageParticles[attackerSlot] = new List<CParticleSystem>();

            _damageParticles[attackerSlot].Add(particleSystem);

            // Schedule particle cleanup and child particle creation
            Task.Delay(500).ContinueWith(_ =>
            {
                Server.NextFrame(() =>
                {
                    try
                    {
                        if (particleSystem?.IsValid == true)
                        {
                            particleSystem.StartActive = false;
                            particleSystem.Remove();
                        }

                        // Safe removal from list
                        if (particleSystem != null && _damageParticles.ContainsKey(attackerSlot))
                        {
                            _damageParticles[attackerSlot].Remove(particleSystem);
                        }

                        // Create child particle
                        CreateChildParticle(attackerSlot, digit, position, isCrit, isRight);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[FortniteHits] Error in particle cleanup: {ex.Message}");
                    }
                });
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FortniteHits] Error creating damage particle: {ex.Message}");
        }
    }

    private void CreateChildParticle(int attackerSlot, int digit, Vector position, bool isCrit, bool isRight)
    {
        try
        {
            var childParticle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
            if (childParticle == null)
                return;

            string childParticleName = $"particles/kolka/fortnite_dmg_v2/kolka_damage_{digit}_{(isRight ? "fr" : "fl")}{(isCrit ? "_crit" : "")}_child.vpcf";

            childParticle.EffectName = childParticleName;
            childParticle.StartActive = true;
            childParticle.Teleport(position);
            childParticle.DispatchSpawn();

            if (!_damageParticles.ContainsKey(attackerSlot))
                _damageParticles[attackerSlot] = new List<CParticleSystem>();

            _damageParticles[attackerSlot].Add(childParticle);

            // Schedule child particle cleanup
            Task.Delay(2500).ContinueWith(_ =>
            {
                Server.NextFrame(() =>
                {
                    try
                    {
                        if (childParticle?.IsValid == true)
                        {
                            childParticle.Remove();
                        }

                        // Safe removal from list
                        if (childParticle != null && _damageParticles.ContainsKey(attackerSlot))
                        {
                            _damageParticles[attackerSlot].Remove(childParticle);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[FortniteHits] Error in child particle cleanup: {ex.Message}");
                    }
                });
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FortniteHits] Error creating child particle: {ex.Message}");
        }
    }

    private static float CalculateDistance(Vector pos1, Vector pos2)
    {
        float dx = pos1.X - pos2.X;
        float dy = pos1.Y - pos2.Y;
        float dz = pos1.Z - pos2.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}