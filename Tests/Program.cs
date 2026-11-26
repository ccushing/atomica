using System.Numerics;
using Atomica.Core;

int passed=0;
void Check(string name,Action test){test();Console.WriteLine("PASS  "+name);passed++;}
void Assert(bool condition,string message="Assertion failed"){if(!condition)throw new Exception(message);}
void Near(float a,float b,float tolerance=.01f){Assert(Math.Abs(a-b)<tolerance,$"Expected {b}; got {a}");}
string Grid(int w=30,int h=24)
{
    var rows=new List<string>();
    for(int y=0;y<h;y++){var cells=new List<string>();for(int x=0;x<w;x++)cells.Add(x==0||y==0||x==w-1||y==h-1?"1":x==4&&y==4?"2":x==w-5&&y==h-5?"3":"0");rows.Add(string.Join(',',cells));}
    return string.Join('\n',rows);
}
Arena Map()=>Arena.Parse(Grid());
void Invalid(string text){try{Arena.Parse(text);}catch(FormatException){return;}throw new Exception("Invalid map was accepted");}
Simulation Quiet(){var s=new Simulation(Map(),new Tuning{CoastDrag=0});s.Enemies[0].Position=new(800,600);s.Enemies[0].Cooldown=10000;return s;}

Check("CSV first row and spawn centers",()=>{var m=Map();Assert(m.Walls[1,0]);Near(m.PlayerStart.X,144);Assert(m.EnemyStarts.Count==1);});
Check("CSV whitespace and BOM",()=>{var m=Arena.Parse("\uFEFF"+Grid().Replace(",",", ").Replace("\n","\r\n"));Assert(m.Width==30);});
Check("CSV rejects ragged, unknown, open perimeter, missing spawns",()=>{Invalid(Grid()+"\n1,1");Invalid(Grid().Replace("2","9"));Invalid(Grid().Replace("2","0"));Invalid(Grid().Replace("3","0"));Invalid("0"+Grid()[1..]);});
Check("CSV rejects duplicate player and disconnected enemies",()=>{Invalid(Grid().Replace("3","2"));Invalid("1,1,1,1,1\n1,2,1,3,1\n1,1,1,1,1");});
Check("CSV rejects dimensions above 512",()=>Invalid(Grid(513,4)));
Check("Inertia unchanged for ten seconds",()=>{var s=Quiet();s.Player.Position=new(300,300);s.Player.Velocity=new(7,3);for(int i=0;i<600;i++)s.Step(1f/60,new());Near(s.Player.Velocity.X,7);Near(s.Player.Velocity.Y,3);Near(s.Player.Position.X,370,.02f);});
Check("Rotation does not redirect velocity",()=>{var s=Quiet();s.Player.Velocity=new(20,10);for(int i=0;i<30;i++)s.Step(1f/60,new(1));Near(s.Player.Velocity.X,20);Near(s.Player.Velocity.Y,10);});
Check("Forward and reverse thrust cancel",()=>{var s=Quiet();s.Step(1f/60,new(0,1));Assert(s.Player.Velocity.Y<0);s.Step(1f/60,new(0,-1));Near(s.Player.Velocity.Length(),0);});
Check("High speed collision does not tunnel",()=>{var p=new Vector2(100,100);var v=new Vector2(-10000,0);Collision.Move(Map(),ref p,ref v,.016f,12);Assert(p.X>=44);Near(v.Length(),10000);Assert(v.X>0);});
Check("Glancing collision retains tangent and speed",()=>{var p=new Vector2(46,200);var v=new Vector2(-200,80);Collision.Move(Map(),ref p,ref v,.05f,12);Near(v.X,200);Near(v.Y,80);});
Check("Corner collision preserves energy and clearance",()=>{var p=new Vector2(50,50);var v=new Vector2(-120,-120);Collision.Move(Map(),ref p,ref v,.1f,12);Assert(p.X>=44 && p.Y>=44);Near(v.Length(),MathF.Sqrt(28800));});
Check("Repeated wall contacts cause no damage",()=>{var s=Quiet();s.Player.Position=new(60,60);s.Player.Velocity=new(-500,0);for(int i=0;i<200;i++)s.Step(1f/60,new());Assert(s.Player.Health==100);});
Check("Normal bullet dies at wall",()=>{var s=Quiet();s.Bullets.Add(new(){Position=new(50,100),Velocity=new(-1000,0),Owner=0,Damage=20});s.Step(.05f,new());Assert(s.Bullets.Count==0);});
Check("Ricochet retains half damage after bouncing",()=>{var s=Quiet();s.Bullets.Add(new(){Position=new(50,100),Velocity=new(-1000,0),Owner=0,Damage=10,Bouncing=true});s.Step(.05f,new());Assert(s.Bullets.Count==1);Assert(s.Bullets[0].Bounces==1 && s.Bullets[0].Damage==10 && s.Bullets[0].Velocity.X>0);});
Check("Fourth wall contact destroys bouncing bullet",()=>{var s=Quiet();s.Bullets.Add(new(){Position=new(50,100),Velocity=new(-1000,0),Owner=0,Damage=10,Bouncing=true,Bounces=3});s.Step(.05f,new());Assert(s.Bullets.Count==0);});
Check("Projectile lifetime is bounded",()=>{var s=Quiet();s.Bullets.Add(new(){Position=new(300,300),Velocity=Vector2.Zero,Age=3});s.Step(1f/60,new());Assert(s.Bullets.Count==0);});
Check("Normal and ricochet hit once at full and half damage",()=>{foreach(int damage in new[]{20,10}){var s=Quiet();var e=s.Enemies[0];e.Position=new(300,200);s.Bullets.Add(new(){Position=new(250,200),Velocity=new(1000,0),Owner=0,Damage=damage,Bouncing=damage==10});s.Step(.06f,new());Assert(e.Health==100-damage);Assert(s.Bullets.Count==0);}});
Check("Wall shields ships from projectiles",()=>{var s=Quiet();s.Player.Position=new(100,100);s.Bullets.Add(new(){Position=new(50,100),Velocity=new(-1000,0),Enemy=true,Owner=1,Damage=20});s.Step(.1f,new());Assert(s.Player.Health==100);});
Check("Owner immunity and enemy friendly fire",()=>{var s=Quiet();s.Bullets.Add(new(){Position=s.Player.Position,Velocity=Vector2.Zero,Owner=0,Damage=20});s.Step(.01f,new());Assert(s.Player.Health==100);s.Bullets.Clear();s.Bullets.Add(new(){Position=s.Enemies[0].Position,Velocity=Vector2.Zero,Owner=100,Enemy=true,Damage=20});s.Step(.01f,new());Assert(s.Enemies[0].Health==100);});
Check("Muzzle inherits ship velocity and correct damage",()=>{var s=Quiet();s.Player.Velocity=new(50,0);s.Step(1f/60,new(0,0,true,1));Near(s.Bullets[0].Velocity.X,50);Near(s.Bullets[0].Velocity.Y,-680);Assert(s.Bullets[0].Damage==10);});
Check("Fire rate and opposite input neutrality",()=>{var s=Quiet();for(int i=0;i<60;i++)s.Step(1f/60,new(0,0,true));Assert(s.Bullets.Count<=4);Near(s.Player.Angle,0);});
Check("Victory, defeat and restart",()=>{var s=Quiet();s.Enemies[0].Health=0;s.Step(.01f,new());Assert(s.State==MatchState.Victory);var r=new Simulation(s.Map);Assert(r.State==MatchState.Playing && r.Player.Health==100 && r.Kills==0);r.Player.Health=0;r.Step(.01f,new());Assert(r.State==MatchState.Defeat);});
Check("Radar corners and center",()=>{var m=Map();var a=m.Radar(m.Size/2,new(10,20),new(200,100));Near(a.X,110);Near(a.Y,70);});
Check("Bundled maps are valid and enemies reachable",()=>{foreach(var name in new[]{"kepler","foundry"}){var m=Arena.Parse(File.ReadAllText($"Prototype/Maps/{name}.csv"));foreach(var spawn in m.EnemyStarts)Assert(m.Path(spawn,m.PlayerStart).Count>0);}});
Check("Invalid tuning rejected",()=>{try{Tuning.Parse("{\"ShotsPerSecond\":0}");}catch(FormatException){return;}throw new Exception("Invalid tuning accepted");});
Check("Overlapping ships do not create invalid velocity",()=>{var s=Quiet();s.Enemies[0].Position=s.Player.Position;s.Step(.01f,new());Assert(float.IsFinite(s.Enemies[0].Velocity.X));});
Check("Nonfinite tuning rejected",()=>{try{Tuning.Parse("{\"Acceleration\":1e39}");}catch(FormatException){return;}throw new Exception("Infinite acceleration accepted");});
Check("Thirty minute simulation soak with repeated restarts",()=>
{
    var map=Arena.Parse(File.ReadAllText("Prototype/Maps/kepler.csv"));var s=new Simulation(map);
    for(int i=0;i<108000;i++)
    {
        s.Step(1f/60,new(.33f,MathF.Sin(i*.01f),i%2==0,i/600%2));
        Assert(float.IsFinite(s.Player.Position.X) && float.IsFinite(s.Player.Position.Y));
        Assert(s.Bullets.Count<300,"Projectile count grew without bound");
        if(s.State!=MatchState.Playing || i%3600==3599)s=new Simulation(map);
    }
});
Check("Gentle coasting drag reduces speed and reaches an exact stop",()=>
{
    var s=Quiet();s.Settings.CoastDrag=24;s.Player.Velocity=new(300,0);
    for(int i=0;i<120;i++)s.Step(1f/60,new());
    Near(s.Player.Velocity.Length(),252,.02f);
    for(int i=0;i<660;i++)s.Step(1f/60,new());
    Assert(s.Player.Velocity==Vector2.Zero);
});
Check("Coast drag is time based and does not oppose active thrust",()=>
{
    foreach(float dt in new[]{1f/30,1f/60,1f/120})
    {var s=Quiet();s.Settings.CoastDrag=24;s.Player.Velocity=new(100,0);for(int i=0;i<(int)Math.Round(1/dt);i++)s.Step(dt,new());Near(s.Player.Velocity.Length(),76,.01f);}
    var a=Quiet();a.Settings.CoastDrag=24;a.Step(1f/60,new(0,1));Near(a.Player.Velocity.Length(),260f/60);
});
Check("Player rotation is fifty percent faster",()=>{var s=Quiet();s.Step(.5f,new(1));Near(s.Player.Angle,2.55f);});
Check("Enemy selector supports every count and unique reachable spawns",()=>
{
    foreach(string name in new[]{"kepler","foundry"})
    {
        var m=Arena.Parse(File.ReadAllText($"Prototype/Maps/{name}.csv"));
        Assert(m.Width==(name=="kepler"?216:288) && m.Height==(name=="kepler"?162:216));
        for(int n=1;n<=15;n++)
        {
            var s=new Simulation(m,null,n);Assert(s.EnemyCount==n && s.Kills==0);
            Assert(s.Enemies.Select(e=>e.Position).Distinct().Count()==n);
            Assert(s.Enemies.All(e=>!m.IsWall((int)e.Position.X/32,(int)e.Position.Y/32)));
            foreach(var e in s.Enemies)e.Health=0;
            s.Step(.01f,new());Assert(s.Kills==n && s.State==MatchState.Victory);
            Assert(new Simulation(m,null,n).EnemyCount==n);
        }
    }
});
Check("Custom map supplements a single enemy marker",()=>{var s=new Simulation(Map(),null,15);Assert(s.EnemyCount==15);});
Check("Tiny maps and out of range counts fail clearly",()=>
{
    var m=Arena.Parse("1,1,1,1\n1,2,3,1\n1,1,1,1");
    try{new Simulation(m,null,2);throw new Exception("Too many enemies accepted");}catch(FormatException){}
    foreach(int n in new[]{0,16})try{new Simulation(m,null,n);throw new Exception("Bad count accepted");}catch(ArgumentOutOfRangeException){}
    try{Tuning.Parse("{\"CoastDrag\":-1}");throw new Exception("Negative drag accepted");}catch(FormatException){}
});
Console.WriteLine($"\n{passed} checks passed.");
