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
Check("Bundled maps are valid and enemies reachable",()=>{foreach(var name in new[]{"kepler","foundry","labyrinth"}){var m=Arena.Parse(File.ReadAllText($"Prototype/Maps/{name}.csv"));foreach(var spawn in m.EnemyStarts)Assert(m.Path(spawn,m.PlayerStart).Count>0);}});
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
    foreach(string name in new[]{"kepler","foundry","labyrinth"})
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
Check("Default drag is halved and still stops exactly",()=>
{
    Near(new Tuning().CoastDrag,12);var s=Quiet();s.Settings.CoastDrag=12;s.Player.Velocity=new(120,0);
    for(int i=0;i<60;i++)s.Step(1f/60,new());Near(s.Player.Velocity.Length(),108,.02f);
    for(int i=0;i<600;i++)s.Step(1f/60,new());Assert(s.Player.Velocity==Vector2.Zero);
});
Check("Death laser hits only the first enemy for triple damage",()=>
{
    var s=Quiet();var e=s.Enemies[0];e.Position=new(144,95);
    s.Enemies.Add(new(){Id=2,Enemy=true,Position=new(144,60),Health=100,Cooldown=10000});
    s.Step(.001f,new(0,0,true,2));Assert(e.Health==40 && s.Enemies[1].Health==100);
    Near(s.Lasers[0].End.Y,107,.1f);Assert(s.Bullets.Count==0);Near(s.LaserCooldown,8);
});
Check("Laser reaches a distant wall and cannot shoot through it",()=>
{
    var m=Arena.Parse(Grid(100,24));var s=new Simulation(m);s.Player.Angle=MathF.PI/2;s.Enemies[0].Cooldown=10000;
    s.Step(.001f,new(0,0,true,2));Near(s.Lasers[0].End.X,99*32,.1f);Assert(s.Lasers[0].End.X-s.Lasers[0].Start.X>2500);
    var blocked=Quiet();blocked.Map.Walls[4,3]=true;blocked.Enemies[0].Position=new(144,80);
    blocked.Step(.001f,new(0,0,true,2));Assert(blocked.Enemies[0].Health==100);Near(blocked.Lasers[0].End.Y,128,.1f);
});
Check("Laser cooldown survives switching and permits firing after reload",()=>
{
    var s=Quiet();s.Enemies[0].Position=new(144,95);s.Step(.001f,new(0,0,true,2));
    s.Step(.1f,new(0,0,false,0));s.Step(.1f,new(0,0,true,2));Assert(s.Enemies[0].Health==40);
    s.Enemies[0].Position=new(144,95);s.Step(8,new(0,0,true,2));Assert(s.Enemies[0].Health==0);
});
Check("Mine placement is stationary and limited to four",()=>
{
    var s=Quiet();for(int i=0;i<5;i++){s.Player.Position=new(144+i*40,300);s.Step(.6f,new(0,0,true,3));}
    Assert(s.Mines.Count==4);Near(s.Mines[0].Position.X,144);Near(s.Mines[0].Position.Y,300);
    var stationary=Quiet();for(int i=0;i<10;i++)stationary.Step(.6f,new(0,0,true,3));Assert(stationary.Mines.Count==1);
});
Check("Mines expire at two minutes and free a slot",()=>
{
    var s=Quiet();s.Player.Position=new(300,300);s.Step(.001f,new(0,0,true,3));
    s.Mines[0].Age=118;s.Step(1,new());Assert(s.Mines.Count==1);
    s.Step(1,new());Assert(s.Mines.Count==0);s.Step(.01f,new(0,0,true,3));Assert(s.Mines.Count==1);
});
Check("Mine delivers double damage once and cannot hurt its owner",()=>
{
    var s=Quiet();s.Player.Position=new(300,300);s.Step(.001f,new(0,0,true,3));Assert(s.Player.Health==100);
    s.Enemies[0].Position=new(300,300);s.Step(.001f,new());Assert(s.Enemies[0].Health==60 && s.Mines.Count==0);
    s.Step(.001f,new());Assert(s.Enemies[0].Health==60 && s.Player.Health==100);
});
Check("Fast enemies cannot skip over mines between ticks",()=>
{
    var s=Quiet();s.Mines.Add(new(){Position=new(300,300)});s.Enemies[0].Position=new(250,300);s.Enemies[0].Velocity=new(10000,0);
    s.Step(.01f,new());Assert(s.Enemies[0].Health==60 && s.Mines.Count==0);
});
Check("Energy starts with fifteen random distinct reachable placements",()=>
{
    var a=new Simulation(Map(),null,null,10);var b=new Simulation(Map(),null,null,11);
    Assert(a.EnergyBoosts.Count==15 && a.EnergyBoosts.Distinct().Count()==15);
    Assert(!a.EnergyBoosts.SequenceEqual(b.EnergyBoosts));
    foreach(var p in a.EnergyBoosts){Assert(!a.Map.IsWall((int)p.X/32,(int)p.Y/32));Assert(a.Map.Path(a.Player.Position,p).Count>0);}
});
Check("Energy heals ten percent of maximum and caps health",()=>
{
    var s=Quiet();s.EnergyBoosts.Clear();s.Player.Health=75;s.EnergyBoosts.Add(s.Player.Position);s.Step(.01f,new());Assert(s.Player.Health==85 && s.EnergyBoosts.Count==0);
    s.Player.Health=95;s.EnergyBoosts.Add(s.Player.Position);s.Step(.01f,new());Assert(s.Player.Health==100 && s.EnergyBoosts.Count==0);
    s.EnergyBoosts.Add(s.Player.Position);s.Step(.01f,new());Assert(s.EnergyBoosts.Count==1);
    s.Settings.Health=200;s.Player.Health=150;s.Step(.01f,new());Assert(s.Player.Health==170);
});
Check("Fast player movement collects crossed energy boosts",()=>
{
    var s=Quiet();s.EnergyBoosts.Clear();s.Player.Health=50;s.Player.Position=new(200,300);s.Player.Velocity=new(10000,0);s.EnergyBoosts.Add(new(250,300));
    s.Step(.01f,new());Assert(s.Player.Health==60 && s.EnergyBoosts.Count==0);
});
Check("Energy refills only on five minute boundaries and never exceeds fifteen",()=>
{
    var s=Quiet();s.EnergyBoosts.RemoveRange(0,5);var survivors=s.EnergyBoosts.ToArray();
    s.Step(299,new());Assert(s.EnergyBoosts.Count==10);s.Step(.9f,new());Assert(s.EnergyBoosts.Count==10);
    s.Step(.11f,new());Assert(s.EnergyBoosts.Count==15 && survivors.All(s.EnergyBoosts.Contains));
    s.EnergyBoosts.Clear();s.Step(299,new());Assert(s.EnergyBoosts.Count==0);s.Step(1,new());Assert(s.EnergyBoosts.Count==15);
});
Check("Death emits one explosion and restart resets weapons and pickups",()=>
{
    var s=Quiet();s.Player.Health=20;s.EnergyBoosts.Clear();s.Mines.Add(new(){Position=new(500,300)});
    s.Bullets.Add(new(){Position=s.Player.Position,Velocity=Vector2.Zero,Enemy=true,Owner=1,Damage=20});
    s.Step(.01f,new());Assert(s.State==MatchState.Defeat && s.Effects.Count(e=>e.Kind=="destroy" && !e.Enemy)==1);
    s.Step(.01f,new());Assert(s.Effects.Count==0);
    var reset=new Simulation(s.Map,s.Settings,s.EnemyCount);Assert(reset.State==MatchState.Playing && reset.Player.Health==100 && reset.Mines.Count==0 && reset.LaserCooldown==0 && reset.EnergyBoosts.Count==15);
});
Check("New timing settings reject zero and invalid values",()=>
{
    foreach(var key in new[]{"LaserReload","MineLifetime","EnergyRefillSeconds"})
    {try{Tuning.Parse("{\""+key+"\":0}");throw new Exception("Invalid timer accepted");}catch(FormatException){}}
});
Check("Rail Gun has half damage and does not bounce",()=>
{
    var s=Quiet();s.Player.Position=new(300,300);s.Enemies[0].Position=new(300,240);
    s.Step(1f/60,new(0,0,true,4));var bullet=s.Bullets.Single();
    Assert(bullet.Rail && !bullet.Bouncing && bullet.Damage==10);Near(bullet.Lifetime,1.5f);
    for(int i=0;i<8;i++)s.Step(1f/60,new(0,0,false,4));Assert(s.Enemies[0].Health==90);
    s.Bullets.Clear();s.Player.Position=new(100,50);s.Step(.1f,new(0,0,true,4));Assert(s.Bullets.Count==0);
});
Check("Rail Gun fires twelve shots per second then starts recharge",()=>
{
    var s=Quiet();int shots=0;
    for(int i=0;i<60;i++){s.Step(1f/60,new(0,0,true,4));shots+=s.Effects.Count(e=>e.Kind=="rail");}
    Assert(shots==12 && s.RailAmmo==0 && s.RailCooldown>2.9f);
});
Check("Rail recharge lasts three seconds and survives weapon switches",()=>
{
    var s=Quiet();for(int i=0;i<56;i++)s.Step(1f/60,new(0,0,true,4));
    Assert(s.RailAmmo==0);Near(s.RailCooldown,3);
    s.Step(1,new(0,0,false,0));s.Step(1.99f,new(0,0,true,4));Assert(s.RailAmmo==0 && s.RailCooldown>0);
    Assert(!s.Effects.Any(e=>e.Kind=="rail"));s.Step(.01f,new(0,0,false,4));
    Assert(s.RailAmmo==12 && s.RailCooldown==0);s.Step(1f/60,new(0,0,true,4));Assert(s.RailAmmo==11);
});
Check("Rail Gun travels exactly half the normal unobstructed range",()=>
{
    var map=Arena.Parse(Grid(120,24));
    float Travel(bool rail)
    {
        var s=new Simulation(map,new Tuning{CoastDrag=0});s.Enemies[0].Cooldown=10000;
        var b=new Bullet{Position=new(200,300),Velocity=new(680,0),Owner=0,Damage=rail?10:20,Rail=rail,Lifetime=rail?1.5f:0};s.Bullets.Add(b);
        for(int i=0;i<200 && b.Alive;i++)s.Step(1f/60,new());return b.Position.X-200;
    }
    float normal=Travel(false),rail=Travel(true);Near(normal,2040,.05f);Near(rail,1020,.05f);Near(rail,normal*.5f,.05f);
});
Check("Rail ammo persists when released and resets on a new match",()=>
{
    var s=Quiet();s.Step(1f/60,new(0,0,true,4));s.Step(4,new());Assert(s.RailAmmo==11);
    var fresh=new Simulation(s.Map);Assert(fresh.RailAmmo==12 && fresh.RailCooldown==0);
});
Check("Rail tuning validates reload and magazine size",()=>
{
    foreach(string json in new[]{"{\"RailReload\":0}","{\"RailReload\":1e39}","{\"RailMagazineSize\":0}","{\"RailMagazineSize\":257}"})
    {try{Tuning.Parse(json);throw new Exception("Invalid Rail Gun settings accepted");}catch(FormatException){}}
    var s=new Simulation(Map(),new Tuning{RailMagazineSize=3,RailReload=3});Assert(s.RailAmmo==3);
});
Check("Labyrinth has no isolated corridors or pickups",()=>
{
    var m=Arena.Parse(File.ReadAllText("Prototype/Maps/labyrinth.csv"));
    int floor=0;
    for(int y=0;y<m.Height;y++)for(int x=0;x<m.Width;x++)if(!m.IsWall(x,y))floor++;
    Assert(m.ReachableCenters().Count==floor);
    Assert(m.EnemyStarts.Count==15);
    Assert(floor<m.Width*m.Height*.5f,"Labyrinth should be predominantly walls and narrow corridors");
});
Check("Tactical Bomb launches slowly in the facing direction independent of ship velocity",()=>
{
    var s=Quiet();s.Player.Position=new(300,300);s.Player.Velocity=new(500,200);s.Player.Angle=MathF.PI/2;
    s.Step(0,new(0,0,true,5));Assert(s.Bomb!=null);Near(s.Bomb!.Velocity.X,68);Near(s.Bomb.Velocity.Y,0);
    Near(s.Bomb.Position.X,308);Near(s.BombCooldown,60);Near(s.WeaponReadyIn,60);
});
Check("Tactical Bomb applies quadruple area damage once within five ship lengths",()=>
{
    var s=Quiet();s.Player.Position=new(300,400);s.Step(0,new(0,0,true,5));s.Bomb!.Position=new(300,300);
    s.Enemies[0].Position=new(300,284);
    s.Enemies.Add(new(){Id=2,Enemy=true,Position=new(460,300),Health=100,Cooldown=10000});
    s.Enemies.Add(new(){Id=3,Enemy=true,Position=new(461,300),Health=100,Cooldown=10000});
    s.Step(0,new());Assert(s.Bomb==null && s.BombBlasts.Count==1);Near(Simulation.BombRadius,160);
    Assert(s.Enemies[0].Health==20 && s.Enemies[1].Health==20 && s.Enemies[2].Health==100 && s.Player.Health==100);
    s.Step(0,new());Assert(s.Enemies[0].Health==20 && s.Enemies[1].Health==20);
});
Check("Tactical Bomb detonates at walls and blast respects shielding",()=>
{
    var s=Quiet();s.Player.Position=new(300,300);s.Step(0,new(0,0,true,5));
    s.Bomb!.Position=new(100,40);s.Enemies[0].Position=new(100,100);
    s.Step(.1f,new());Assert(s.Bomb==null && s.Enemies[0].Health==20);Near(s.BombBlasts[0].Position.Y,36);
    s=Quiet();s.Player.Position=new(300,400);s.Step(0,new(0,0,true,5));s.Bomb!.Position=new(300,300);
    s.Enemies[0].Position=new(300,284);s.Enemies.Add(new(){Id=2,Enemy=true,Position=new(400,300),Health=100,Cooldown=10000});
    for(int y=1;y<s.Map.Height-1;y++)s.Map.Walls[11,y]=true;
    s.Step(0,new());Assert(s.Enemies[0].Health==20 && s.Enemies[1].Health==100);
});
Check("Tactical Bomb recharges for sixty seconds across weapon switches",()=>
{
    var s=Quiet();s.Player.Position=new(300,50);s.Step(0,new(0,0,true,5));s.Step(.1f,new());Assert(s.Bomb==null);
    s.Step(59.89f,new(0,0,true,5));Assert(s.Bomb==null && s.BombCooldown>0);
    s.Step(.01f,new(0,0,false,0));Near(s.BombCooldown,0,.00001f);
    s.Player.Position=new(300,300);s.Step(0,new(0,0,true,5));Assert(s.Bomb!=null);Near(s.BombCooldown,60);
});
Check("Tactical Bomb active limit persists even after recharge and resets on restart",()=>
{
    var s=new Simulation(Arena.Parse(Grid(120,120)),new Tuning{BombSpeed=1});s.Player.Position=new(2000,2000);s.Enemies[0].Cooldown=10000;
    s.Step(0,new(0,0,true,5));var first=s.Bomb;s.Step(60,new(0,0,true,5));
    Assert(ReferenceEquals(first,s.Bomb) && s.BombCooldown==0 && !s.Effects.Any(e=>e.Kind=="bomb-launch"));
    var fresh=new Simulation(s.Map,s.Settings);Assert(fresh.Bomb==null && fresh.BombCooldown==0 && fresh.BombBlasts.Count==0);
});
Check("Fast enemies cannot cross a Tactical Bomb between frames",()=>
{
    var s=new Simulation(Map(),new Tuning{Acceleration=.001f,CoastDrag=0});s.Player.Position=new(300,400);
    var e=s.Enemies[0];e.Position=new(200,300);e.Velocity=new(2000,0);e.Cooldown=10000;
    s.Step(0,new(0,0,true,5));s.Bomb!.Position=new(300,300);s.Step(.1f,new());
    Assert(s.Bomb==null && e.Health==20);
});
Check("Tactical Bomb tuning rejects invalid speed and recharge",()=>
{
    foreach(string key in new[]{"BombReload","BombSpeed"})foreach(string value in new[]{"0","-1","1e39"})
    {try{Tuning.Parse("{\""+key+"\":"+value+"}");throw new Exception("Invalid bomb setting accepted");}catch(FormatException){}}
});
Console.WriteLine($"\n{passed} checks passed.");
