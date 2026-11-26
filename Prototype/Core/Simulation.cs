using System.Numerics;
using System.Text.Json;

namespace Atomica.Core;

public sealed class Tuning
{
    public float Acceleration {get;set;}=260;
    public float TurnRate {get;set;}=5.1f;
    public float CoastDrag {get;set;}=24;
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
    public bool Enemy,Bouncing,Alive=true;
    public float Age;
}
public readonly record struct Effect(Vector2 Position,string Kind,bool Enemy);

public sealed class Simulation
{
    public Arena Map {get;}
    public Tuning Settings {get;}
    public Ship Player {get;}
    public List<Ship> Enemies {get;}=new();
    public List<Bullet> Bullets {get;}=new();
    public List<Effect> Effects {get;}=new();
    public MatchState State {get;private set;}
    public int Weapon {get;private set;}
    public float Elapsed {get;private set;}
    public int EnemyCount => Enemies.Count;
    public int Kills => EnemyCount-Enemies.Count(e=>e.Health>0);
    public Simulation(Arena map,Tuning? tuning=null,int? enemyCount=null)
    {
        Map=map;Settings=tuning??new();
        Player=new(){Id=0,Position=map.PlayerStart,Health=Settings.Health};
        int id=1;
        foreach(var spawn in enemyCount is { } count ? map.SelectEnemySpawns(count) : map.EnemyStarts) Enemies.Add(new(){Id=id++,Enemy=true,Position=spawn,Health=Settings.Health,Cooldown=1.5f+id*.13f,PatrolIndex=id%map.EnemyStarts.Count});
    }

    public void Step(float dt,Command command)
    {
        Effects.Clear();
        if(State!=MatchState.Playing) return;
        Elapsed+=dt;
        Weapon=Math.Clamp(command.Weapon,0,1);
        Player.Cooldown=Math.Max(0,Player.Cooldown-dt); Player.Flash=Math.Max(0,Player.Flash-dt);
        Player.Angle+=Math.Clamp(command.Turn,-1,1)*Settings.TurnRate*dt;
        Player.Velocity+=Player.Forward*Math.Clamp(command.Thrust,-1,1)*Settings.Acceleration*dt;
        if(Math.Abs(command.Thrust)<.001f && Settings.CoastDrag>0)
        {
            float speed=Player.Velocity.Length();
            Player.Velocity=speed<=Settings.CoastDrag*dt?Vector2.Zero:Player.Velocity*((speed-Settings.CoastDrag*dt)/speed);
        }
        if(Settings.SpeedCap>0 && Player.Velocity.Length()>Settings.SpeedCap) Player.Velocity=Vector2.Normalize(Player.Velocity)*Settings.SpeedCap;
        Collision.Move(Map,ref Player.Position,ref Player.Velocity,dt,12);
        if(command.Fire && Player.Cooldown<=0) Fire(Player,Weapon==1);
        foreach(var enemy in Enemies)
        {
            if(enemy.Health<=0) continue;
            enemy.Cooldown=Math.Max(0,enemy.Cooldown-dt); enemy.Flash=Math.Max(0,enemy.Flash-dt);
            EnemyStep(enemy,dt);
        }
        foreach(var bullet in Bullets)
        {
            bullet.Age+=dt;
            if(bullet.Age>Settings.ProjectileLifetime) {bullet.Alive=false;continue;}
            ProjectileStep(bullet,dt);
        }
        Bullets.RemoveAll(b=>!b.Alive);
        if(Player.Health<=0) State=MatchState.Defeat;
        else if(Enemies.All(e=>e.Health<=0)) State=MatchState.Victory;
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
        Collision.Move(Map,ref enemy.Position,ref enemy.Velocity,dt,12);
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
                target.Health=Math.Max(0,target.Health-bullet.Damage);target.Flash=.16f;
                Effects.Add(new(target.Position,target.Health==0?"destroy":"hit",target.Enemy));
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
