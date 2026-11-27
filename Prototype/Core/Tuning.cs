using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atomica.Core;

// Defaults live here. tuning.json may override any subset at startup.
public sealed class Tuning
{
    // Movement and collision (world pixels, seconds, radians).
    [Range(0.001,100000)] public float Acceleration {get;set;}=260;
    [Range(0.001,100)] public float TurnRate {get;set;}=5.1f;
    [Range(0,100000)] public float CoastDrag {get;set;}=12;
    [Range(0,100000)] public float SpeedCap {get;set;}=0;
    [Range(1,1000000)] public int Health {get;set;}=100;
    [Range(1,15)] public float ShipRadius {get;set;}=12;
    [Range(0.01,15)] public float ProjectileRadius {get;set;}=2;
    [Range(0,15)] public float ProjectileMuzzleOffset {get;set;}=9;
    [Range(0,100)] public float MuzzleFlashOffset {get;set;}=16;

    // Normal rounds and ricochets.
    [Range(1,1000000)] public int NormalDamage {get;set;}=20;
    [Range(1,1000000)] public int BouncingDamage {get;set;}=10;
    [Range(0.01,60)] public float ShotsPerSecond {get;set;}=4;
    [Range(0.01,100000)] public float MuzzleSpeed {get;set;}=680;
    [Range(0.001,3600)] public float ProjectileLifetime {get;set;}=3;
    [Range(0,256)] public int MaxBounces {get;set;}=3;

    // Death Laser: dimensions also control its rendering and collision.
    [Range(0.001,3600)] public float LaserReload {get;set;}=8;
    [Range(0.001,100)] public float LaserDamageMultiplier {get;set;}=3;
    [Range(0.01,30)] public float LaserWidth {get;set;}=21;
    [Range(0.01,1000)] public float LaserGlowWidth {get;set;}=60;
    [Range(0.01,30)] public float LaserCoreWidth {get;set;}=6;
    [Range(0.001,10)] public float LaserVisibleSeconds {get;set;}=.3f;

    // Rail Gun.
    [Range(0.001,3600)] public float RailReload {get;set;}=3;
    [Range(1,256)] public int RailMagazineSize {get;set;}=12;
    [Range(0.001,100)] public float RailDamageMultiplier {get;set;}=.5f;
    [Range(0.01,60)] public float RailFireRateMultiplier {get;set;}=3;
    [Range(0.001,100)] public float RailLifetimeMultiplier {get;set;}=.5f;

    // Side-mounted guns fire a forward pair per shot; recharge uses RailReload.
    [Range(1,32)] public int SideGunBurstSize {get;set;}=9;
    [Range(0.01,60)] public float SideGunShotsPerSecond {get;set;}=12;

    // Tactical Bomb.
    [Range(0.001,3600)] public float BombReload {get;set;}=60;
    [Range(0.001,100000)] public float BombSpeed {get;set;}=204;
    [Range(0.001,100)] public float BombDamageMultiplier {get;set;}=4;
    [Range(0.01,10000)] public float BombBlastRadius {get;set;}=160;
    [Range(0.01,15)] public float BombProjectileRadius {get;set;}=4;
    [Range(0,15)] public float BombMuzzleOffset {get;set;}=8;
    [Range(0.001,10)] public float BombBlastVisibleSeconds {get;set;}=.6f;

    // Mines and energy pickups.
    [Range(0,256)] public int MaxMines {get;set;}=4;
    [Range(0.001,3600)] public float MineLifetime {get;set;}=120;
    [Range(0.001,3600)] public float MinePlantInterval {get;set;}=.5f;
    [Range(0.001,100)] public float MineDamageMultiplier {get;set;}=2;
    [Range(0.01,10000)] public float MineSeparation {get;set;}=24;
    [Range(0.01,10000)] public float MineTriggerRadius {get;set;}=22;
    [Range(0,1000)] public int MaxEnergyBoosts {get;set;}=15;
    [Range(0.001,86400)] public float EnergyRefillSeconds {get;set;}=300;
    [Range(0.001,1)] public float EnergyHealFraction {get;set;}=.1f;
    [Range(0.01,10000)] public float EnergyPickupRadius {get;set;}=22;
    [Range(0,10000)] public float EnergyPlayerClearance {get;set;}=48;
    [Range(0,10000)] public float EnergyEnemyClearance {get;set;}=32;
    [Range(0,10000)] public float EnergyMineClearance {get;set;}=24;

    // Enemy AI and match setup.
    [Range(1,64)] public int MaxEnemies {get;set;}=15;
    [Range(1,64)] public int DefaultEnemyCount {get;set;}=6;
    [Range(0,2)] public int DefaultArenaIndex {get;set;}=0;
    [Range(0.001,100000)] public float EnemySpeed {get;set;}=105;
    [Range(0.001,3600)] public float EnemyFireInterval {get;set;}=1.1f;
    [Range(0,3600)] public float EnemyInitialFireDelay {get;set;}=1.5f;
    [Range(0,60)] public float EnemyInitialFireStagger {get;set;}=.13f;
    [Range(0.01,100000)] public float EnemySightRange {get;set;}=720;
    [Range(0,15)] public float EnemySightRadius {get;set;}=1;
    [Range(0.01,100000)] public float EnemyChaseRange {get;set;}=1000;
    [Range(0.01,100000)] public float EnemyApproachDistance {get;set;}=280;
    [Range(0.01,100000)] public float EnemyRetreatDistance {get;set;}=170;
    [Range(0,10)] public float EnemyRetreatSpeedMultiplier {get;set;}=.5f;
    [Range(0,60)] public float EnemyAimLeadSeconds {get;set;}=.55f;
    [Range(0.001,60)] public float EnemyPathInterval {get;set;}=1;
    [Range(0,60)] public float EnemyPathStagger {get;set;}=.03f;
    [Range(0.01,10000)] public float EnemyPatrolArrivalDistance {get;set;}=48;
    [Range(0.01,15)] public float EnemyWaypointDistance {get;set;}=9;
    [Range(0.001,100)] public float EnemyApproachGain {get;set;}=5;

    // Presentation and startup preferences.
    [Range(640,7680)] public int WindowWidth {get;set;}=1280;
    [Range(360,4320)] public int WindowHeight {get;set;}=800;
    [Range(30,240)] public int PhysicsTicksPerSecond {get;set;}=60;
    public bool Fullscreen {get;set;}=false;
    public bool MutedAudio {get;set;}=false;
    public bool OriginalShipArt {get;set;}=false;
    public bool RadarAimEnabled {get;set;}=false;
    [Range(-80,6)] public float ShotVolumeDb {get;set;}=-23;
    [Range(-80,6)] public float HitVolumeDb {get;set;}=-15;
    [Range(0.01,100)] public float CameraFollowRate {get;set;}=10;
    [Range(0,10)] public float DeathDelaySeconds {get;set;}=1.2f;
    [Range(0.001,10)] public float HitPulseSeconds {get;set;}=.25f;
    [Range(0.001,10)] public float ShipHitFlashSeconds {get;set;}=.16f;
    [Range(0,100)] public float LowHullPercent {get;set;}=35;
    [Range(0,1000)] public int ExplosionParticleCount {get;set;}=90;
    [Range(0,1000)] public int SparkParticleCount {get;set;}=7;
    [Range(0.001,10)] public float ExplosionParticleLifetime {get;set;}=1.2f;
    [Range(0.001,10)] public float SparkParticleLifetime {get;set;}=.3f;
    [Range(0,10000)] public float ParticleMinSpeed {get;set;}=30;
    [Range(0,10000)] public float ExplosionParticleSpeedSpread {get;set;}=320;
    [Range(0,10000)] public float SparkParticleSpeedSpread {get;set;}=180;

    [JsonIgnore] public int LaserDamage=>ScaledDamage(LaserDamageMultiplier);
    [JsonIgnore] public int RailDamage=>ScaledDamage(RailDamageMultiplier);
    [JsonIgnore] public int BombDamage=>ScaledDamage(BombDamageMultiplier);
    [JsonIgnore] public int MineDamage=>ScaledDamage(MineDamageMultiplier);
    int ScaledDamage(float multiplier)=>Math.Max(1,(int)MathF.Round(NormalDamage*multiplier));

    public static Tuning Load(string? json)=>json==null?new():Parse(json);

    public static Tuning Parse(string json)
    {
        var options=new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive=true, ReadCommentHandling=JsonCommentHandling.Skip,
            AllowTrailingCommas=true, UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow
        };
        Tuning t;
        try
        {
            using var doc=JsonDocument.Parse(json,new(){CommentHandling=JsonCommentHandling.Skip,AllowTrailingCommas=true});
            if(doc.RootElement.ValueKind!=JsonValueKind.Object)throw new FormatException("tuning.json must contain a JSON object.");
            var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var editable=typeof(Tuning).GetProperties().Where(p=>p.CanWrite).Select(p=>p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach(var setting in doc.RootElement.EnumerateObject())
            {
                if(!editable.Contains(setting.Name))throw new FormatException($"Unknown setting '{setting.Name}'.");
                if(!names.Add(setting.Name))throw new FormatException($"Duplicate setting '{setting.Name}'.");
            }
            t=JsonSerializer.Deserialize<Tuning>(json,options)!;
        }
        catch(JsonException e){throw new FormatException("Invalid tuning.json: "+e.Message,e);}
        foreach(var property in typeof(Tuning).GetProperties().Where(p=>p.CanWrite))
        {
            var value=property.GetValue(t);
            if(value is float f && !float.IsFinite(f))throw new FormatException($"{property.Name} must be finite.");
            if(property.GetCustomAttribute<RangeAttribute>() is { } range && !range.IsValid(value))
                throw new FormatException($"{property.Name} must be between {range.Minimum} and {range.Maximum}.");
        }
        if(t.DefaultEnemyCount>t.MaxEnemies)throw new FormatException("DefaultEnemyCount cannot exceed MaxEnemies.");
        if(t.ProjectileMuzzleOffset+t.ProjectileRadius>t.ShipRadius || t.BombMuzzleOffset+t.BombProjectileRadius>t.ShipRadius)
            throw new FormatException("Projectile muzzle offset plus radius must fit inside ShipRadius.");
        if(t.LaserCoreWidth>t.LaserWidth || t.LaserWidth>t.LaserGlowWidth || t.LaserWidth>2*t.ShipRadius)
            throw new FormatException("LaserCoreWidth <= LaserWidth <= LaserGlowWidth; LaserWidth must fit within the ship diameter.");
        if(t.EnemyRetreatDistance>t.EnemyApproachDistance)throw new FormatException("EnemyRetreatDistance cannot exceed EnemyApproachDistance.");
        if(t.ShotsPerSecond*t.RailFireRateMultiplier>t.PhysicsTicksPerSecond || t.ShotsPerSecond>t.PhysicsTicksPerSecond || t.SideGunShotsPerSecond>t.PhysicsTicksPerSecond)
            throw new FormatException("Fire rates cannot exceed PhysicsTicksPerSecond.");
        return t;
    }
}
