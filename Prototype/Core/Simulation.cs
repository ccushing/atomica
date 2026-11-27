using System.Numerics;
using System.Text.Json;

namespace Atomica.Core;

public sealed class Tuning
{
    public float Acceleration {get;set;}=260;
    public float TurnRate {get;set;}=5.1f;
    public float CoastDrag {get;set;}=12;
    public float LaserReload {get;set;}=8;
    public float RailReload {get;set;}=3;
    public float BombReload {get;set;}=60;
    public float BombSpeed {get;set;}=68;
    public int RailMagazineSize {get;set;}=12;
    public float MineLifetime {get;set;}=120;
    public float EnergyRefillSeconds {get;set;}=300;
    public int Health {get;set;}=100;
    public int NormalDamage {get;set;}=20;
    public int BouncingDamage {get;set;}=10;
    public float ShotsPerSecond {get;set;}=4;
    public float MuzzleSpeed {get;set;}=680;
    public float ProjectileLifetime {get;set;}=3;
    public int MaxBounces {get;set;}=3;
    public float EnemySpeed {get;set;}=105;
    public float EnemyFireInterval {get;set;}=1.1f;
    public float SpeedCap {get;set;}=0;
    public static Tuning Parse(string json)
    {
        var t=JsonSerializer.Deserialize<Tuning>(json) ?? throw new FormatException("Empty tuning file.");
        if(!float.IsFinite(t.CoastDrag) || t.CoastDrag<0) throw new FormatException("CoastDrag must be finite and nonnegative.");
        if(!float.IsFinite(t.RailReload) || t.RailReload<=0 || t.RailMagazineSize<1 || t.RailMagazineSize>256) throw new FormatException("RailReload must be positive and finite; RailMagazineSize must be 1 to 256.");
        if(new[]{t.LaserReload,t.BombReload,t.BombSpeed,t.MineLifetime,t.EnergyRefillSeconds}.Any(v=>!float.IsFinite(v) || v<=0)) throw new FormatException("Weapon reloads, bomb speed, mine lifetime and energy refill interval must be positive and finite.");
        if(new[]{t.Acceleration,t.TurnRate,t.ShotsPerSecond,t.MuzzleSpeed,t.ProjectileLifetime,t.EnemySpeed,t.EnemyFireInterval,t.SpeedCap}.Any(v=>!float.IsFinite(v)) || t.Acceleration<=0 || t.TurnRate<=0 || t.Health<=0 || t.NormalDamage<=0 || t.BouncingDamage<=0 || t.ShotsPerSecond<=0 || t.MuzzleSpeed<=0 || t.ProjectileLifetime<=0 || t.MaxBounces<0 || t.EnemySpeed<=0 || t.EnemyFireInterval<=0 || t.SpeedCap<0)
            throw new FormatException("Tuning values must be positive; SpeedCap and MaxBounces may be zero.");
        return t;
    }
}

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
public sealed class BombBlast { public Vector2 Position; public float Life=.6f; }
public sealed class LaserBeam { public Vector2 Start,End; public float Life=.3f; }

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
    // Nominal ship length is one 32-pixel map cell.
    public const float BombRadius=5*Arena.Cell;
    public float BombCooldown {get;private set;}
    double bombReadyAt;
    public List<LaserBeam> Lasers {get;}=new();
    public List<Vector2> EnergyBoosts {get;}=new();
    public const int MaxMines=4, MaxEnergyBoosts=15;
    public float LaserCooldown {get;private set;}
    public float MineCooldown {get;private set;}
    public float RailCooldown {get;private set;}
    public float RailShotCooldown {get;private set;}
    public int RailAmmo {get;private set;}
    public float WeaponReadyIn => Weapon==2?LaserCooldown:Weapon==3?MineCooldown:Weapon==4?Math.Max(RailCooldown,RailShotCooldown):Weapon==5?BombCooldown:Player.Cooldown;
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
        foreach(var spawn in enemyCount is { } count ? map.SelectEnemySpawns(count) : map.EnemyStarts) Enemies.Add(new(){Id=id++,Enemy=true,Position=spawn,Health=Settings.Health,Cooldown=1.5f+id*.13f,PatrolIndex=id%map.EnemyStarts.Count});
        random=randomSeed is { } seed?new Random(seed):new Random();
        pickupCells=map.ReachableCenters();nextEnergyRefill=Settings.EnergyRefillSeconds;
        RefillEnergy();
    }

    public void Step(float dt,Command command)
    {
        Effects.Clear();
        if(State!=MatchState.Playing) return;
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
        Weapon=Math.Clamp(command.Weapon,0,5);
        Player.Cooldown=Math.Max(0,Player.Cooldown-dt); Player.Flash=Math.Max(0,Player.Flash-dt);
        Player.Angle+=Math.Clamp(command.Turn,-1,1)*Settings.TurnRate*dt;
        Player.Velocity+=Player.Forward*Math.Clamp(command.Thrust,-1,1)*Settings.Acceleration*dt;
        if(Math.Abs(command.Thrust)<.001f && Settings.CoastDrag>0)
        {
            float speed=Player.Velocity.Length();
            Player.Velocity=speed<=Settings.CoastDrag*dt?Vector2.Zero:Player.Velocity*((speed-Settings.CoastDrag*dt)/speed);
        }
        if(Settings.SpeedCap>0 && Player.Velocity.Length()>Settings.SpeedCap) Player.Velocity=Vector2.Normalize(Player.Velocity)*Settings.SpeedCap;
        Collision.Move(Map,ref Player.Position,ref Player.Velocity,dt,12,CollectEnergy);
        if(command.Fire && Player.Health>0)
        {
            if(Weapon<2 && Player.Cooldown<=0) Fire(Player,Weapon==1);
            else if(Weapon==2 && LaserCooldown<=0) FireLaser();
            else if(Weapon==3 && MineCooldown<=0) LayMine();
            else if(Weapon==4 && RailCooldown<=0 && RailShotCooldown<=.000001f) FireRail();
            else if(Weapon==5 && BombCooldown<=0 && Bomb==null) FireBomb();
        }
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
        target.Health=Math.Max(0,target.Health-damage);target.Flash=.16f;
        Effects.Add(new(target.Position,target.Health==0?"destroy":"hit",target.Enemy));
    }

    void FireLaser()
    {
        LaserCooldown=Settings.LaserReload;
        Vector2 start=Player.Position,delta=Player.Forward*(Map.Size.Length()+32);
        float nearest=Collision.Wall(Map,start,delta,0)?.Time??1;
        Ship? target=null;
        foreach(var enemy in Enemies)
        {
            if(enemy.Health<=0)continue;
            if(Collision.Circle(start,delta,enemy.Position,12) is { } t && t<nearest){nearest=t;target=enemy;}
        }
        Vector2 end=start+delta*nearest;
        Lasers.Add(new(){Start=start,End=end});
        Effects.Add(new(start,"laser",false));Effects.Add(new(end,"spark",true));
        if(target!=null)Damage(target,Settings.NormalDamage*3);
    }

    void FireRail()
    {
        if(RailAmmo<=0)return;
        RailAmmo--;RailShotCooldown=1/(Settings.ShotsPerSecond*3);
        Bullets.Add(new(){Position=Player.Position+Player.Forward*9,Velocity=Player.Velocity+Player.Forward*Settings.MuzzleSpeed,Owner=Player.Id,Damage=Math.Max(1,(int)MathF.Round(Settings.NormalDamage*.5f)),Rail=true,Lifetime=Settings.ProjectileLifetime*.5f});
        Effects.Add(new(Player.Position+Player.Forward*16,"rail",false));
        if(RailAmmo==0){RailCooldown=Settings.RailReload;railRechargeAt=gameplayTime+Settings.RailReload;}
    }

    void FireBomb()
    {
        // Fixed world speed keeps the bomb slow even when launched at high ship speed.
        Bomb=new(){Position=Player.Position+Player.Forward*8,Velocity=Player.Forward*Settings.BombSpeed};
        BombCooldown=Settings.BombReload;bombReadyAt=gameplayTime+Settings.BombReload;
        Effects.Add(new(Bomb.Position,"bomb-launch",false));
    }

    void StepBomb(float dt,Dictionary<int,Vector2> starts)
    {
        var bomb=Bomb!;Vector2 delta=bomb.Velocity*dt;
        float nearest=Collision.Wall(Map,bomb.Position,delta,4)?.Time??2;
        foreach(var enemy in Enemies)
        {
            if(enemy.Health<=0)continue;
            // Relative sweep catches enemies crossing the slow projectile between frames.
            if(Collision.Circle(bomb.Position,delta-(enemy.Position-starts[enemy.Id]),starts[enemy.Id],16) is { } t && t<nearest)nearest=t;
        }
        if(nearest>1){bomb.Position+=delta;return;}
        bomb.Position+=delta*nearest;Bomb=null;
        BombBlasts.Add(new(){Position=bomb.Position});Effects.Add(new(bomb.Position,"bomb",false));
        foreach(var enemy in Enemies)
        {
            Vector2 atImpact=Vector2.Lerp(starts[enemy.Id],enemy.Position,nearest);
            if(enemy.Health>0 && Vector2.DistanceSquared(bomb.Position,atImpact)<=BombRadius*BombRadius && Collision.Wall(Map,bomb.Position,atImpact-bomb.Position,0)==null)
                Damage(enemy,Settings.NormalDamage*4);
        }
    }

    void LayMine()
    {
        if(Mines.Count>=MaxMines || Mines.Any(m=>Vector2.DistanceSquared(m.Position,Player.Position)<24*24))return;
        Mines.Add(new(){Position=Player.Position});MineCooldown=.5f;
        Effects.Add(new(Player.Position,"plant",false));
    }

    void TriggerMines(Ship enemy,Vector2 from,Vector2 to)
    {
        foreach(var mine in Mines.ToArray())
        {
            if(enemy.Health<=0)break;
            if(Collision.Circle(from,to-from,mine.Position,22)==null)continue;
            Mines.Remove(mine);Effects.Add(new(mine.Position,"mine",true));Damage(enemy,Settings.NormalDamage*2);
        }
    }

    void CollectEnergy(Vector2 from,Vector2 to)
    {
        if(Player.Health<=0 || Player.Health>=Settings.Health)return;
        // Pickups are processed in travel order, including fast movement between frames.
        var crossed=EnergyBoosts.Select(p=>(Position:p,Time:Collision.Circle(from,to-from,p,22))).Where(p=>p.Time.HasValue).OrderBy(p=>p.Time).ToArray();
        foreach(var pickup in crossed)
        {
            if(Player.Health>=Settings.Health)break;
            Player.Health=Math.Min(Settings.Health,Player.Health+Math.Max(1,(int)MathF.Round(Settings.Health*.1f)));
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
            if(Vector2.DistanceSquared(p,Player.Position)<48*48 || Enemies.Any(e=>e.Health>0 && Vector2.DistanceSquared(p,e.Position)<32*32) || Mines.Any(m=>Vector2.DistanceSquared(p,m.Position)<24*24) || EnergyBoosts.Contains(p))continue;
            EnergyBoosts.Add(p);
        }
    }

    void Fire(Ship ship,bool bouncing)
    {
        ship.Cooldown=ship.Enemy?Settings.EnemyFireInterval:1/Settings.ShotsPerSecond;
        // Start inside the hull, immune to owner; sweep from here to avoid firing through a wall.
        Bullets.Add(new(){Position=ship.Position+ship.Forward*9,Velocity=ship.Velocity+ship.Forward*Settings.MuzzleSpeed,Owner=ship.Id,Enemy=ship.Enemy,Bouncing=bouncing,Damage=bouncing?Settings.BouncingDamage:Settings.NormalDamage});
        Effects.Add(new(ship.Position+ship.Forward*16,"shot",ship.Enemy));
    }

    void EnemyStep(Ship enemy,float dt)
    {
        Vector2 toPlayer=Player.Position-enemy.Position;
        float distance=toPlayer.Length();
        bool clear=Collision.Wall(Map,enemy.Position,toPlayer,1)==null;
        Vector2 desired;
        if(clear && distance<720)
        {
            var aim=Player.Position+Player.Velocity*Math.Min(.55f,distance/Settings.MuzzleSpeed)-enemy.Position;
            enemy.Angle=MathF.Atan2(aim.X,-aim.Y);
            desired=distance>280?Vector2.Normalize(toPlayer)*Settings.EnemySpeed:distance is >.001f and <170?-Vector2.Normalize(toPlayer)*Settings.EnemySpeed*.5f:Vector2.Zero;
            if(enemy.Cooldown<=0) Fire(enemy,false);
        }
        else
        {
            enemy.PathTimer-=dt;
            Vector2 target=distance<1000?Player.Position:Map.EnemyStarts[enemy.PatrolIndex];
            if(Vector2.Distance(enemy.Position,target)<48) enemy.PatrolIndex=(enemy.PatrolIndex+1)%Map.EnemyStarts.Count;
            if(enemy.PathTimer<=0) {enemy.Path=Map.Path(enemy.Position,target);enemy.PathTimer=1.0f+enemy.Id*.03f;}
            while(enemy.Path.Count>0 && Vector2.Distance(enemy.Position,enemy.Path[0])<9) enemy.Path.RemoveAt(0);
            var offset=enemy.Path.Count>0?enemy.Path[0]-enemy.Position:Vector2.Zero;
            desired=offset.Length()>1?Vector2.Normalize(offset)*Math.Min(Settings.EnemySpeed,offset.Length()*5):Vector2.Zero;
            if(desired.LengthSquared()>1) enemy.Angle=MathF.Atan2(desired.X,-desired.Y);
        }
        var change=desired-enemy.Velocity;
        float max=Settings.Acceleration*dt;
        enemy.Velocity+=change.Length()>max?Vector2.Normalize(change)*max:change;
        Collision.Move(Map,ref enemy.Position,ref enemy.Velocity,dt,12,(from,to)=>TriggerMines(enemy,from,to));
    }

    void ProjectileStep(Bullet bullet,float dt)
    {
        float left=dt;
        for(int iteration=0;iteration<8 && left>1e-7f && bullet.Alive;iteration++)
        {
            var delta=bullet.Velocity*left;
            var wall=Collision.Wall(Map,bullet.Position,delta,2);
            float earliest=wall?.Time??2;
            Ship? target=null;
            IEnumerable<Ship> candidates=bullet.Enemy?new[]{Player}:Enemies;
            foreach(var ship in candidates)
            {
                if(ship.Health<=0 || ship.Id==bullet.Owner) continue;
                var t=Collision.Circle(bullet.Position,delta,ship.Position,14);
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
