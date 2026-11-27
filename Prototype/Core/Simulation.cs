using System.Numerics;

namespace Atomica.Core;

public enum MatchState { Playing, Victory, Defeat }
public readonly record struct Command(float Turn=0,float Thrust=0,bool Fire=false,int Weapon=0);
public sealed class Ship
{
    public int Id;
    public bool Enemy;
    public Vector2 Position,Velocity;
    public float Angle, Cooldown, PathTimer, Flash;
    public int Health;
    public List<Vector2> Path=new();
    public int PatrolIndex;
    public Vector2 Forward => new(MathF.Sin(Angle),-MathF.Cos(Angle));
}
public sealed class Bullet
{
    public Vector2 Position,Velocity;
    public int Owner,Damage,Bounces;
    public bool Enemy,Bouncing,Rail,Alive=true;
    public float Age,Lifetime;
}
public readonly record struct Effect(Vector2 Position,string Kind,bool Enemy);
public sealed class Mine { public Vector2 Position; public float Age; }
public sealed class TacticalBomb { public Vector2 Position,Velocity; }
public sealed class BombBlast { public Vector2 Position; public float Life; }
public sealed class LaserBeam
{
    public Vector2 Start,End;
    public float Life;
}

public sealed class Simulation
{
    public Arena Map {get;}
    public Tuning Settings {get;}
    public Ship Player {get;}
    public List<Ship> Enemies {get;}=new();
    public List<Bullet> Bullets {get;}=new();
    public List<Effect> Effects {get;}=new();
    public List<Mine> Mines {get;}=new();
    public TacticalBomb? Bomb {get;private set;}
    public List<BombBlast> BombBlasts {get;}=new();
    public float BombRadius=>Settings.BombBlastRadius;
    public float BombCooldown {get;private set;}
    double bombReadyAt;
    bool fireWasHeld;
    public List<LaserBeam> Lasers {get;}=new();
    public List<Vector2> EnergyBoosts {get;}=new();
    public int MaxMines=>Settings.MaxMines;
    public int MaxEnergyBoosts=>Settings.MaxEnergyBoosts;
    public float LaserCooldown {get;private set;}
    public float MineCooldown {get;private set;}
    public float RailCooldown {get;private set;}
    public float RailShotCooldown {get;private set;}
    public int RailAmmo {get;private set;}
    public float SideGunCooldown {get;private set;}
    public float SideGunShotCooldown {get;private set;}
    public int SideGunShotsRemaining {get;private set;}
    double sideGunReadyAt;
    public float WeaponReadyIn => Weapon==2?LaserCooldown:Weapon==3?MineCooldown:Weapon==4?Math.Max(RailCooldown,RailShotCooldown):Weapon==5?BombCooldown:Weapon==6?Math.Max(SideGunCooldown,SideGunShotCooldown):Player.Cooldown;
    public float EnergyRefillIn => (float)Math.Max(0,nextEnergyRefill-gameplayTime);
    readonly Random random;
    readonly List<Vector2> pickupCells;
    double gameplayTime,nextEnergyRefill,railRechargeAt;
    public MatchState State {get;private set;}
    public int Weapon {get;private set;}
    public float Elapsed {get;private set;}
    public int EnemyCount => Enemies.Count;
    public int Kills => EnemyCount-Enemies.Count(e=>e.Health>0);
    public Simulation(Arena map,Tuning? tuning=null,int? enemyCount=null,int? randomSeed=null)
    {
        Map=map;Settings=tuning??new();
        RailAmmo=Settings.RailMagazineSize;
        Player=new(){Id=0,Position=map.PlayerStart,Health=Settings.Health};
        int id=1;
        foreach(var spawn in enemyCount is { } count ? map.SelectEnemySpawns(count,Settings.MaxEnemies) : map.EnemyStarts) Enemies.Add(new(){Id=id++,Enemy=true,Position=spawn,Health=Settings.Health,Cooldown=Settings.EnemyInitialFireDelay+id*Settings.EnemyInitialFireStagger,PatrolIndex=id%map.EnemyStarts.Count});
        random=randomSeed is { } seed?new Random(seed):new Random();
        pickupCells=map.ReachableCenters();nextEnergyRefill=Settings.EnergyRefillSeconds;
        RefillEnergy();
    }

    public void Step(float dt,Command command)
    {
        Effects.Clear();
        if(State!=MatchState.Playing) return;
        bool firePressed=command.Fire && !fireWasHeld;
        fireWasHeld=command.Fire;
        Elapsed+=dt;
        gameplayTime+=dt;
        BombCooldown=(float)Math.Max(0,bombReadyAt-gameplayTime);
        if(BombCooldown<.000001f)BombCooldown=0;
        foreach(var blast in BombBlasts)blast.Life-=dt;
        BombBlasts.RemoveAll(b=>b.Life<=0);
        foreach(var beam in Lasers)beam.Life-=dt;
        Lasers.RemoveAll(b=>b.Life<=0);
        foreach(var mine in Mines)mine.Age+=dt;
        Mines.RemoveAll(m=>m.Age>=Settings.MineLifetime);
        LaserCooldown=Math.Max(0,LaserCooldown-dt);MineCooldown=Math.Max(0,MineCooldown-dt);
        RailShotCooldown=Math.Max(0,RailShotCooldown-dt);
        if(RailCooldown>0)
        {
            RailCooldown=(float)Math.Max(0,railRechargeAt-gameplayTime);
            if(RailCooldown<=.000001f){RailCooldown=0;RailAmmo=Settings.RailMagazineSize;}
        }
        SideGunCooldown=(float)Math.Max(0,sideGunReadyAt-gameplayTime);
        if(SideGunCooldown<.000001f)SideGunCooldown=0;
        SideGunShotCooldown=Math.Max(0,SideGunShotCooldown-dt);
        Weapon=Math.Clamp(command.Weapon,0,6);
        if(Weapon!=6 && SideGunShotsRemaining>0)FinishSideGunBurst();
        Player.Cooldown=Math.Max(0,Player.Cooldown-dt); Player.Flash=Math.Max(0,Player.Flash-dt);
        Player.Angle+=Math.Clamp(command.Turn,-1,1)*Settings.TurnRate*dt;
        Player.Velocity+=Player.Forward*Math.Clamp(command.Thrust,-1,1)*Settings.Acceleration*dt;
        if(Math.Abs(command.Thrust)<.001f && Settings.CoastDrag>0)
        {
            float speed=Player.Velocity.Length();
            Player.Velocity=speed<=Settings.CoastDrag*dt?Vector2.Zero:Player.Velocity*((speed-Settings.CoastDrag*dt)/speed);
        }
        if(Settings.SpeedCap>0 && Player.Velocity.Length()>Settings.SpeedCap) Player.Velocity=Vector2.Normalize(Player.Velocity)*Settings.SpeedCap;
        Collision.Move(Map,ref Player.Position,ref Player.Velocity,dt,Settings.ShipRadius,CollectEnergy);
        if(command.Fire && Player.Health>0)
        {
            if(Weapon<2 && Player.Cooldown<=0) Fire(Player,Weapon==1);
            else if(Weapon==2 && LaserCooldown<=0) FireLaser();
            else if(Weapon==3 && MineCooldown<=0) LayMine();
            else if(Weapon==4 && RailCooldown<=0 && RailShotCooldown<=.000001f) FireRail();
            else if(Weapon==5 && firePressed)
            {
                if(Bomb!=null)DetonateBomb(Bomb.Position);
                else if(BombCooldown<=0)FireBomb();
            }
            else if(Weapon==6 && SideGunCooldown<=0 && SideGunShotsRemaining==0)
            {
                SideGunShotsRemaining=Settings.SideGunBurstSize;SideGunShotCooldown=0;
            }
        }
        if(Weapon==6 && Player.Health>0 && SideGunShotsRemaining>0 && SideGunShotCooldown<=.000001f)FireSideGuns();
        var enemyStarts=Bomb!=null?Enemies.ToDictionary(e=>e.Id,e=>e.Position):null;
        foreach(var enemy in Enemies)
        {
            if(enemy.Health<=0) continue;
            enemy.Cooldown=Math.Max(0,enemy.Cooldown-dt); enemy.Flash=Math.Max(0,enemy.Flash-dt);
            EnemyStep(enemy,dt);
        }
        if(Bomb!=null)StepBomb(dt,enemyStarts!);
        foreach(var bullet in Bullets)
        {
            float lifetime=bullet.Lifetime>0?bullet.Lifetime:Settings.ProjectileLifetime;
            float remaining=Math.Min(dt,lifetime-bullet.Age);
            if(remaining>0)ProjectileStep(bullet,remaining);
            bullet.Age+=dt;
            if(bullet.Age>=lifetime)bullet.Alive=false;
        }
        Bullets.RemoveAll(b=>!b.Alive);
        if(Player.Health<=0)
        {
            State=MatchState.Defeat;
            if(!Effects.Any(e=>e.Kind=="destroy" && !e.Enemy))Effects.Add(new(Player.Position,"destroy",false));
        }
        else if(Enemies.All(e=>e.Health<=0)) State=MatchState.Victory;
        if(State==MatchState.Playing && gameplayTime>=nextEnergyRefill)
        {
            RefillEnergy();
            nextEnergyRefill+=Settings.EnergyRefillSeconds*(Math.Floor((gameplayTime-nextEnergyRefill)/Settings.EnergyRefillSeconds)+1);
        }
    }

    void Damage(Ship target,int damage)
    {
        if(target.Health<=0)return;
        target.Health=Math.Max(0,target.Health-damage);target.Flash=Settings.ShipHitFlashSeconds;
        Effects.Add(new(target.Position,target.Health==0?"destroy":"hit",target.Enemy));
    }

    void FireLaser()
    {
        LaserCooldown=Settings.LaserReload;
        Vector2 start=Player.Position,delta=Player.Forward*(Map.Size.Length()+32);
        float nearest=Collision.Wall(Map,start,delta,Settings.LaserWidth/2)?.Time??1;
        Ship? target=null;
        foreach(var enemy in Enemies)
        {
            if(enemy.Health<=0)continue;
            if(Collision.Circle(start,delta,enemy.Position,Settings.ShipRadius+Settings.LaserWidth/2) is { } t && t<nearest){nearest=t;target=enemy;}
        }
        Vector2 end=start+delta*nearest;
        Lasers.Add(new(){Start=start,End=end,Life=Settings.LaserVisibleSeconds});
        Effects.Add(new(start,"laser",false));Effects.Add(new(end,"spark",true));
        if(target!=null)Damage(target,Settings.LaserDamage);
    }

    void FireRail()
    {
        if(RailAmmo<=0)return;
        RailAmmo--;RailShotCooldown=1/(Settings.ShotsPerSecond*Settings.RailFireRateMultiplier);
        Bullets.Add(new(){Position=Player.Position+Player.Forward*Settings.ProjectileMuzzleOffset,Velocity=Player.Velocity+Player.Forward*Settings.MuzzleSpeed,Owner=Player.Id,Damage=Settings.RailDamage,Rail=true,Lifetime=Settings.ProjectileLifetime*Settings.RailLifetimeMultiplier});
        Effects.Add(new(Player.Position+Player.Forward*Settings.MuzzleFlashOffset,"rail",false));
        if(RailAmmo==0){RailCooldown=Settings.RailReload;railRechargeAt=gameplayTime+Settings.RailReload;}
    }

    void FireSideGuns()
    {
        Vector2 right=new(Player.Forward.Y*-1,Player.Forward.X);
        foreach(float side in new[]{-1f,1f})
        {
            Vector2 direction=right*side;
            Bullets.Add(new(){Position=Player.Position+direction*Settings.ProjectileMuzzleOffset,Velocity=Player.Velocity+Player.Forward*Settings.MuzzleSpeed,Owner=Player.Id,Damage=Settings.NormalDamage});
            Effects.Add(new(Player.Position+direction*Settings.MuzzleFlashOffset,"shot",false));
        }
        SideGunShotsRemaining--;SideGunShotCooldown=1/Settings.SideGunShotsPerSecond;
        if(SideGunShotsRemaining==0)FinishSideGunBurst();
    }

    void FinishSideGunBurst()
    {
        SideGunShotsRemaining=0;SideGunShotCooldown=0;
        SideGunCooldown=Settings.RailReload;sideGunReadyAt=gameplayTime+Settings.RailReload;
    }

    void FireBomb()
    {
        // Add launch velocity to the ship's full current motion, including sideways drift.
        Bomb=new(){Position=Player.Position+Player.Forward*Settings.BombMuzzleOffset,Velocity=Player.Velocity+Player.Forward*Settings.BombSpeed};
        BombCooldown=Settings.BombReload;bombReadyAt=gameplayTime+Settings.BombReload;
        Effects.Add(new(Bomb.Position,"bomb-launch",false));
    }

    void StepBomb(float dt,Dictionary<int,Vector2> starts)
    {
        var bomb=Bomb!;Vector2 delta=bomb.Velocity*dt;
        float nearest=Collision.Wall(Map,bomb.Position,delta,Settings.BombProjectileRadius)?.Time??2;
        foreach(var enemy in Enemies)
        {
            if(enemy.Health<=0)continue;
            // Relative sweep catches enemies crossing the slow projectile between frames.
            if(Collision.Circle(bomb.Position,delta-(enemy.Position-starts[enemy.Id]),starts[enemy.Id],Settings.ShipRadius+Settings.BombProjectileRadius) is { } t && t<nearest)nearest=t;
        }
        if(nearest>1){bomb.Position+=delta;return;}
        bomb.Position+=delta*nearest;
        DetonateBomb(bomb.Position,starts,nearest);
    }

    void DetonateBomb(Vector2 position,Dictionary<int,Vector2>? starts=null,float impactTime=1)
    {
        Bomb=null;
        BombBlasts.Add(new(){Position=position,Life=Settings.BombBlastVisibleSeconds});Effects.Add(new(position,"bomb",false));
        foreach(var enemy in Enemies)
        {
            Vector2 atImpact=starts==null?enemy.Position:Vector2.Lerp(starts[enemy.Id],enemy.Position,impactTime);
            if(enemy.Health>0 && Vector2.DistanceSquared(position,atImpact)<=BombRadius*BombRadius && Collision.Wall(Map,position,atImpact-position,0)==null)
                Damage(enemy,Settings.BombDamage);
        }
    }

    void LayMine()
    {
        if(Mines.Count>=MaxMines || Mines.Any(m=>Vector2.DistanceSquared(m.Position,Player.Position)<Settings.MineSeparation*Settings.MineSeparation))return;
        Mines.Add(new(){Position=Player.Position});MineCooldown=Settings.MinePlantInterval;
        Effects.Add(new(Player.Position,"plant",false));
    }

    void TriggerMines(Ship enemy,Vector2 from,Vector2 to)
    {
        foreach(var mine in Mines.ToArray())
        {
            if(enemy.Health<=0)break;
            if(Collision.Circle(from,to-from,mine.Position,Settings.MineTriggerRadius)==null)continue;
            Mines.Remove(mine);Effects.Add(new(mine.Position,"mine",true));Damage(enemy,Settings.MineDamage);
        }
    }

    void CollectEnergy(Vector2 from,Vector2 to)
    {
        if(Player.Health<=0 || Player.Health>=Settings.Health)return;
        // Pickups are processed in travel order, including fast movement between frames.
        var crossed=EnergyBoosts.Select(p=>(Position:p,Time:Collision.Circle(from,to-from,p,Settings.EnergyPickupRadius))).Where(p=>p.Time.HasValue).OrderBy(p=>p.Time).ToArray();
        foreach(var pickup in crossed)
        {
            if(Player.Health>=Settings.Health)break;
            Player.Health=Math.Min(Settings.Health,Player.Health+Math.Max(1,(int)MathF.Round(Settings.Health*Settings.EnergyHealFraction)));
            EnergyBoosts.Remove(pickup.Position);Effects.Add(new(pickup.Position,"energy",false));
        }
    }

    void RefillEnergy()
    {
        // Partial Fisher-Yates shuffle; each candidate is considered at most once.
        for(int i=0;i<pickupCells.Count && EnergyBoosts.Count<MaxEnergyBoosts;i++)
        {
            int j=random.Next(i,pickupCells.Count);(pickupCells[i],pickupCells[j])=(pickupCells[j],pickupCells[i]);
            Vector2 p=pickupCells[i];
            if(Vector2.DistanceSquared(p,Player.Position)<Settings.EnergyPlayerClearance*Settings.EnergyPlayerClearance || Enemies.Any(e=>e.Health>0 && Vector2.DistanceSquared(p,e.Position)<Settings.EnergyEnemyClearance*Settings.EnergyEnemyClearance) || Mines.Any(m=>Vector2.DistanceSquared(p,m.Position)<Settings.EnergyMineClearance*Settings.EnergyMineClearance) || EnergyBoosts.Contains(p))continue;
            EnergyBoosts.Add(p);
        }
    }

    void Fire(Ship ship,bool bouncing)
    {
        ship.Cooldown=ship.Enemy?Settings.EnemyFireInterval:1/Settings.ShotsPerSecond;
        // Start inside the hull, immune to owner; sweep from here to avoid firing through a wall.
        Bullets.Add(new(){Position=ship.Position+ship.Forward*Settings.ProjectileMuzzleOffset,Velocity=ship.Velocity+ship.Forward*Settings.MuzzleSpeed,Owner=ship.Id,Enemy=ship.Enemy,Bouncing=bouncing,Damage=bouncing?Settings.BouncingDamage:Settings.NormalDamage});
        Effects.Add(new(ship.Position+ship.Forward*Settings.MuzzleFlashOffset,"shot",ship.Enemy));
    }

    void EnemyStep(Ship enemy,float dt)
    {
        Vector2 toPlayer=Player.Position-enemy.Position;
        float distance=toPlayer.Length();
        bool clear=Collision.Wall(Map,enemy.Position,toPlayer,Settings.EnemySightRadius)==null;
        Vector2 desired;
        if(clear && distance<Settings.EnemySightRange)
        {
            var aim=Player.Position+Player.Velocity*Math.Min(Settings.EnemyAimLeadSeconds,distance/Settings.MuzzleSpeed)-enemy.Position;
            enemy.Angle=MathF.Atan2(aim.X,-aim.Y);
            desired=distance>Settings.EnemyApproachDistance?Vector2.Normalize(toPlayer)*Settings.EnemySpeed:distance>.001f && distance<Settings.EnemyRetreatDistance?-Vector2.Normalize(toPlayer)*Settings.EnemySpeed*Settings.EnemyRetreatSpeedMultiplier:Vector2.Zero;
            if(enemy.Cooldown<=0) Fire(enemy,false);
        }
        else
        {
            enemy.PathTimer-=dt;
            Vector2 target=distance<Settings.EnemyChaseRange?Player.Position:Map.EnemyStarts[enemy.PatrolIndex];
            if(Vector2.Distance(enemy.Position,target)<Settings.EnemyPatrolArrivalDistance) enemy.PatrolIndex=(enemy.PatrolIndex+1)%Map.EnemyStarts.Count;
            if(enemy.PathTimer<=0) {enemy.Path=Map.Path(enemy.Position,target);enemy.PathTimer=Settings.EnemyPathInterval+enemy.Id*Settings.EnemyPathStagger;}
            while(enemy.Path.Count>0 && Vector2.Distance(enemy.Position,enemy.Path[0])<Settings.EnemyWaypointDistance) enemy.Path.RemoveAt(0);
            var offset=enemy.Path.Count>0?enemy.Path[0]-enemy.Position:Vector2.Zero;
            desired=offset.Length()>1?Vector2.Normalize(offset)*Math.Min(Settings.EnemySpeed,offset.Length()*Settings.EnemyApproachGain):Vector2.Zero;
            if(desired.LengthSquared()>1) enemy.Angle=MathF.Atan2(desired.X,-desired.Y);
        }
        var change=desired-enemy.Velocity;
        float max=Settings.Acceleration*dt;
        enemy.Velocity+=change.Length()>max?Vector2.Normalize(change)*max:change;
        Collision.Move(Map,ref enemy.Position,ref enemy.Velocity,dt,Settings.ShipRadius,(from,to)=>TriggerMines(enemy,from,to));
    }

    void ProjectileStep(Bullet bullet,float dt)
    {
        float left=dt;
        for(int iteration=0;iteration<8 && left>1e-7f && bullet.Alive;iteration++)
        {
            var delta=bullet.Velocity*left;
            var wall=Collision.Wall(Map,bullet.Position,delta,Settings.ProjectileRadius);
            float earliest=wall?.Time??2;
            Ship? target=null;
            IEnumerable<Ship> candidates=bullet.Enemy?new[]{Player}:Enemies;
            foreach(var ship in candidates)
            {
                if(ship.Health<=0 || ship.Id==bullet.Owner) continue;
                var t=Collision.Circle(bullet.Position,delta,ship.Position,Settings.ShipRadius+Settings.ProjectileRadius);
                if(t is { } f && f<earliest) {earliest=f;target=ship;}
            }
            if(target!=null)
            {
                bullet.Position+=delta*earliest; bullet.Alive=false;
                Damage(target,bullet.Damage);
            }
            else if(wall is { } h)
            {
                bullet.Position+=delta*h.Time+h.Normal*.01f;
                Effects.Add(new(bullet.Position,"spark",bullet.Enemy));
                if(!bullet.Bouncing || bullet.Bounces>=Settings.MaxBounces) {bullet.Alive=false;break;}
                bullet.Bounces++;bullet.Velocity=Vector2.Reflect(bullet.Velocity,h.Normal);left*=1-h.Time;
            }
            else {bullet.Position+=delta;break;}
        }
    }
}
