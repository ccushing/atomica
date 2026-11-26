using Godot;
using Atomica.Core;
using V = System.Numerics.Vector2;

public partial class Game : Node2D
{
    readonly Color bg = new("070e19"), panel = new("101c2c"), line = new("263b50"), ink = new("e7f0f5"), muted = new("7891a6"), mint = new("6cebd1"), red = new("ff797c"), amber = new("f9c46b");
    readonly List<(Rect2 Bounds,Action Action)> buttons = new();
    readonly List<Particle> particles = new();
    readonly Random random = new(42);
    readonly string[] mapNames={"KEPLER OUTPOST","THE FOUNDRY"};
    readonly string[] mapFiles={"kepler.csv","foundry.csv"};
    Arena arena=null!;
    Simulation sim=null!;
    Tuning tuning=new();
    Font font=null!;
    Texture2D? oldPlayer,oldEnemy;
    ImageTexture? radarTexture;
    Vector2 camera;
    bool menu=true, paused, help, legacy, mutedAudio;
    int selected, weapon, frame, enemyCount=6;
    float clock, hitPulse;
    string error="", customName="";
    FileDialog picker=null!;
    AudioStreamPlayer shotSound=null!, hitSound=null!;
    string? capture;
    bool demo;
    Vector2 Size=>GetViewportRect().Size;
    sealed class Particle { public Vector2 P,V; public float Life,Max; public Color Color; }

    public override void _Ready()
    {
        font=ThemeDB.FallbackFont;
        oldPlayer=GD.Load<Texture2D>("res://Assets/Warbird.png");
        oldEnemy=GD.Load<Texture2D>("res://Assets/Terrier.png");
        try { tuning=Tuning.Parse(ReadData("tuning.json")); } catch(Exception e) {error="Tuning could not be read. Using defaults. "+e.Message;}
        LoadBuiltIn(0);
        picker=new FileDialog { FileMode=FileDialog.FileModeEnum.OpenFile,Access=FileDialog.AccessEnum.Filesystem,Title="Load an Atomica CSV arena",Size=new Vector2I(900,600),UseNativeDialog=true };
        picker.AddFilter("*.csv","Atomica maps");
        picker.FileSelected+=LoadCustom;
        AddChild(picker);
        shotSound=new AudioStreamPlayer{Stream=Tone(false),VolumeDb=-23};AddChild(shotSound);
        hitSound=new AudioStreamPlayer{Stream=Tone(true),VolumeDb=-15};AddChild(hitSound);
        foreach(string arg in OS.GetCmdlineUserArgs())
        {
            if(arg.StartsWith("--capture=")) capture=arg[10..];
            if(arg=="--demo") demo=true;
        }
        if(demo) Start();
        GetWindow().FocusExited+=()=> {if(!menu && sim.State==MatchState.Playing) paused=true;};
    }

    string ReadData(string file)
    {
        string local=System.IO.Path.Combine(OS.GetExecutablePath().GetBaseDir(),file);
        if(!OS.HasFeature("editor") && System.IO.File.Exists(local)) return System.IO.File.ReadAllText(local);
        return Godot.FileAccess.GetFileAsString("res://"+file);
    }

    void LoadBuiltIn(int index)
    {
        try
        {
            var next=Arena.Parse(ReadData("Maps/"+mapFiles[index]),mapNames[index]);
            arena=next;selected=index;customName="";Reset();BuildRadar();
        }
        catch(Exception e) {error=e.Message;}
    }
    void LoadCustom(string path)
    {
        try
        {
            var next=Arena.Parse(System.IO.File.ReadAllText(path),System.IO.Path.GetFileNameWithoutExtension(path));
            arena=next;customName=next.Name;Reset();BuildRadar();menu=true;error="";
        }
        catch(Exception e) {error="Map not loaded: "+e.Message;menu=true;}
    }
    void Reset() {sim=new Simulation(arena,tuning);camera=G(sim.Player.Position);weapon=0;particles.Clear();paused=false;help=false;hitPulse=0;}
    void Start()
    {
        try { var next=new Simulation(arena,tuning,enemyCount);Reset();sim=next;menu=false;error=""; }
        catch(Exception e) {menu=true;error=e.Message;}
    }
    void BuildRadar()
    {
        using var image=Image.CreateEmpty(arena.Width,arena.Height,false,Image.Format.Rgba8);
        image.Fill(new Color("0b1522"));
        for(int y=0;y<arena.Height;y++)for(int x=0;x<arena.Width;x++)if(arena.Walls[x,y])image.SetPixel(x,y,new Color("35566b"));
        radarTexture=ImageTexture.CreateFromImage(image);
    }
    public override void _PhysicsProcess(double delta)
    {
        if(sim==null || menu || paused || help || sim.State!=MatchState.Playing) return;
        float turn=(Input.IsPhysicalKeyPressed(Key.Right)?1:0)-(Input.IsPhysicalKeyPressed(Key.Left)?1:0);
        float thrust=(Input.IsPhysicalKeyPressed(Key.Up)?1:0)-(Input.IsPhysicalKeyPressed(Key.Down)?1:0);
        int health=sim.Player.Health;
        sim.Step((float)delta,new Command(turn,thrust,Input.IsPhysicalKeyPressed(Key.Space),weapon));
        if(sim.Player.Health<health) hitPulse=.25f;
        foreach(var effect in sim.Effects)
        {
            if(effect.Kind=="shot") {if(!effect.Enemy && !mutedAudio)shotSound.Play();continue;}
            int count=effect.Kind=="destroy"?35:7;
            Color c=effect.Enemy?red:mint;
            for(int i=0;i<count;i++)
            {
                float a=(float)random.NextDouble()*MathF.Tau;
                float speed=30+(float)random.NextDouble()*180;
                particles.Add(new(){P=G(effect.Position),V=new Vector2(MathF.Cos(a),MathF.Sin(a))*speed,Life=effect.Kind=="destroy"?.8f:.3f,Max=effect.Kind=="destroy"?.8f:.3f,Color=c});
            }
            if(!mutedAudio && effect.Kind is "hit" or "destroy") hitSound.Play();
        }
    }
    public override void _Process(double delta)
    {
        clock+=(float)delta;frame++;hitPulse=Math.Max(0,hitPulse-(float)delta);
        if(!paused && !help)
        {
            foreach(var p in particles){p.Life-=(float)delta;p.P+=p.V*(float)delta;}
            particles.RemoveAll(p=>p.Life<=0);
        }
        if(sim!=null)camera=camera.Lerp(G(sim.Player.Position),1-MathF.Exp(-(float)delta*10));
        QueueRedraw();
        if(capture!=null && frame==15) Capture();
    }
    async void Capture()
    {
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        var image=GetViewport().GetTexture().GetImage();
        image.SavePng(capture!);GD.Print("CAPTURE "+capture);GetTree().Quit();
    }

    public override void _Input(InputEvent ev)
    {
        if(ev is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex==MouseButton.Left)
        {
            foreach(var b in buttons.ToArray())if(b.Bounds.HasPoint(mouse.Position)){b.Action();break;}
        }
        if(ev is not InputEventKey key || !key.Pressed || key.Echo) return;
        if(help && key.PhysicalKeycode is not (Key.F1 or Key.Escape or Key.F11 or Key.M)) return;
        switch(key.PhysicalKeycode)
        {
            case Key.F11: DisplayServer.WindowSetMode(DisplayServer.WindowGetMode()==DisplayServer.WindowMode.Fullscreen?DisplayServer.WindowMode.Windowed:DisplayServer.WindowMode.Fullscreen);break;
            case Key.M: mutedAudio=!mutedAudio;break;
            case Key.V:legacy=!legacy;break;
            case Key.F1:help=!help;break;
            case Key.Escape:
                if(help)help=false;
                else if(!menu && sim.State==MatchState.Playing)paused=!paused;
                else if(!menu)menu=true;
                break;
            case Key.Key1:if(menu){LoadBuiltIn(0);error="";}else weapon=0;break;
            case Key.Key2:if(menu){LoadBuiltIn(1);error="";}else weapon=1;break;
            case Key.Enter:if(menu)Start();else if(paused)paused=false;else if(sim.State!=MatchState.Playing)Start();break;
            case Key.R:if(!menu && (paused || sim.State!=MatchState.Playing))Start();break;
        }
    }

    public override void _Draw()
    {
        buttons.Clear();DrawRect(new Rect2(Vector2.Zero,Size),bg);
        if(sim==null)return;
        DrawWorld();
        if(menu)DrawMenu();else DrawHud();
        if(!menu && (paused || sim.State!=MatchState.Playing))DrawOverlay();
        if(help)DrawHelp();
    }

    static Vector2 G(V p)=>new(p.X,p.Y);
    Vector2 World(Vector2 p)=>p-camera+Size/2;
    void Text(string s,Vector2 at,int size,Color color)=>DrawString(font,at,s,HorizontalAlignment.Left,-1,size,color);
    void Small(string s,float x,float y)=>Text(s,new(x,y),13,muted);
    void Box(Rect2 r,Color color,bool border=true){DrawRect(r,color);if(border)DrawRect(r,line,false,1);}
    void Button(string label,Rect2 r,Action action,bool primary=false)
    {
        bool hover=r.HasPoint(GetGlobalMousePosition());
        Box(r,primary?(hover?new Color("98ffe6"):mint):(hover?new Color("1c3144"):panel),!primary);
        Vector2 textSize=font.GetStringSize(label,HorizontalAlignment.Left,-1,17);
        Text(label,new(r.Position.X+(r.Size.X-textSize.X)/2,r.Position.Y+r.Size.Y/2+6),17,primary?bg:ink);
        buttons.Add((r,action));
    }

    void DrawWorld()
    {
        // Camera is clamped to arena extents; small arenas are centered.
        var half=Size/2;
        camera=new Vector2(arena.Size.X<Size.X?arena.Size.X/2:Math.Clamp(camera.X,half.X,arena.Size.X-half.X),arena.Size.Y<Size.Y?arena.Size.Y/2:Math.Clamp(camera.Y,half.Y,arena.Size.Y-half.Y));
        for(int i=0;i<210;i++)
        {
            float x=((i*7919%1709)-camera.X*.12f)%Size.X; if(x<0)x+=Size.X;
            float y=((i*3571%1213)-camera.Y*.12f)%Size.Y;if(y<0)y+=Size.Y;
            DrawCircle(new(x,y),i%5==0?1.3f:.7f,new Color(.35f,.54f,.67f,i%3==0?.45f:.2f));
        }
        for(int x=0;x<=arena.Width;x+=4)
        {float sx=World(new(x*32,0)).X;if(sx>=0 && sx<=Size.X)DrawLine(new(sx,0),new(sx,Size.Y),new Color("10202e"));}
        for(int y=0;y<=arena.Height;y+=4)
        {float sy=World(new(0,y*32)).Y;if(sy>=0 && sy<=Size.Y)DrawLine(new(0,sy),new(Size.X,sy),new Color("10202e"));}
        int left=Math.Max(0,(int)(camera.X-Size.X/2)/32-1),right=Math.Min(arena.Width-1,(int)(camera.X+Size.X/2)/32+1);
        int top=Math.Max(0,(int)(camera.Y-Size.Y/2)/32-1),bottom=Math.Min(arena.Height-1,(int)(camera.Y+Size.Y/2)/32+1);
        for(int y=top;y<=bottom;y++)for(int x=left;x<=right;x++)
        {
            if(!arena.Walls[x,y])continue;
            Vector2 p=World(new(x*32,y*32));
            DrawRect(new Rect2(p,new(32,32)),new Color("182939"));
            DrawRect(new Rect2(p+new Vector2(2,2),new(28,28)),new Color("213648"));
            if(!arena.IsWall(x,y-1))DrawLine(p,p+new Vector2(32,0),new Color("557d90"),2);
            if(!arena.IsWall(x-1,y))DrawLine(p,p+new Vector2(0,32),new Color("365768"),2);
            if((x+y)%5==0)DrawRect(new Rect2(p+new Vector2(12,12),new(8,3)),new Color("567382"));
        }
        foreach(var b in sim.Bullets)
        {
            Vector2 p=World(G(b.Position));Color c=b.Enemy?red:b.Bouncing?amber:mint;
            Vector2 dir=G(b.Velocity).Normalized();
            DrawLine(p-dir*12,p,c,3,true);DrawCircle(p,2,Colors.White);
        }
        foreach(var enemy in sim.Enemies)if(enemy.Health>0)DrawShip(enemy);
        if(sim.Player.Health>0)DrawShip(sim.Player);
        foreach(var part in particles)DrawCircle(World(part.P),2,new Color(part.Color,part.Life/part.Max));
        if(hitPulse>0)DrawRect(new Rect2(Vector2.Zero,Size),new Color(1,.2f,.25f,hitPulse*.22f));
    }
    void DrawShip(Ship ship)
    {
        Vector2 p=World(G(ship.Position));if(p.X< -60 || p.Y< -60 || p.X>Size.X+60 || p.Y>Size.Y+60)return;
        Color color=ship.Enemy?red:mint;
        if(!ship.Enemy)
        {
            DrawArc(p,27,0,MathF.Tau,40,new Color(mint,.18f),1,true);
            DrawLine(p+new Vector2(-34,0),p+new Vector2(-29,0),mint);
            DrawLine(p+new Vector2(29,0),p+new Vector2(34,0),mint);
        }
        DrawSetTransform(p,ship.Angle);
        bool thrust=!ship.Enemy && !menu && !paused && Input.IsPhysicalKeyPressed(Key.Up);
        if(thrust || ship.Enemy && ship.Velocity.Length()>30)
        {
            float flame=12+MathF.Sin(clock*65)*5;
            DrawColoredPolygon(new[]{new Vector2(-5,10),new Vector2(0,10+flame),new Vector2(5,10)},new Color(color,.7f));
        }
        if(legacy && (ship.Enemy?oldEnemy:oldPlayer) is { } texture)DrawTextureRect(texture,new Rect2(-20,-20,40,40),false,ship.Flash>0?Colors.White:Colors.White);
        else
        {
            var points=ship.Enemy?new[]{new Vector2(0,-17),new Vector2(14,8),new Vector2(6,13),new Vector2(0,7),new Vector2(-6,13),new Vector2(-14,8)}:new[]{new Vector2(0,-20),new Vector2(14,13),new Vector2(0,7),new Vector2(-14,13)};
            DrawColoredPolygon(points,ship.Flash>0?Colors.White:new Color("1d3745"));
            var outline=points.Concat(new[]{points[0]}).ToArray();DrawPolyline(outline,color,1.8f,true);
            DrawColoredPolygon(new[]{new Vector2(0,-10),new Vector2(4,3),new Vector2(-4,3)},color);
        }
        DrawSetTransform(Vector2.Zero);
        if(ship.Enemy && ship.Health<tuning.Health)
        {DrawRect(new Rect2(p+new Vector2(-15,25),new(30,3)),line);DrawRect(new Rect2(p+new Vector2(-15,25),new(30f*ship.Health/tuning.Health,3)),red);}
    }

    void DrawMenu()
    {
        DrawRect(new Rect2(Vector2.Zero,Size),new Color(bg,.89f));
        float x=Size.X*.08f,y=Size.Y*.13f;
        Small("FLIGHT SYSTEMS ONLINE     /     SINGLE PLAYER",x,y);
        Text("ATOMICA",new(x,y+83),76,ink);
        Text("Momentum is your weapon.",new(x,y+126),25,mint);
        Text("Pilot a ship through a hostile orbital arena. Drift, ricochet, survive.",new(x,y+165),17,muted);
        float cardsY=y+210, cardW=Math.Min(390,(Size.X-2*x-24)/2);
        for(int i=0;i<2;i++)
        {
            int index=i;Rect2 r=new(x+i*(cardW+20),cardsY,cardW,138);
            Box(r,selected==i && customName==""?new Color("152b39"):panel);
            if(selected==i && customName=="")DrawRect(r,mint,false,1.5f);
            Small("0"+(i+1)+"  /  "+(i==0?"OPEN ARENA":"INDUSTRIAL MAZE"),r.Position.X+22,r.Position.Y+28);
            Text(mapNames[i],r.Position+new Vector2(22,64),24,ink);
            Text(i==0?"216 × 162 cells  ·  Open space":"288 × 216 cells  ·  Industrial corridors",r.Position+new Vector2(22,99),15,muted);
            buttons.Add((r,()=>{LoadBuiltIn(index);error="";}));
        }
        float countY=cardsY+164;
        Small("ENEMY SHIPS",x,countY+29);
        Button("−",new(x+140,countY,46,44),()=>enemyCount=Math.Max(1,enemyCount-1));
        Text(enemyCount.ToString("00"),new(x+206,countY+30),25,mint);
        Button("+",new(x+260,countY,46,44),()=>enemyCount=Math.Min(15,enemyCount+1));
        Small("1–15  /  YOUR CHOICE",x+326,countY+29);
        float actionY=cardsY+230;
        Button("LAUNCH  /  ENTER",new(x,actionY,260,52),Start,true);
        Button("LOAD CSV",new(x+278,actionY,150,52),()=>picker.PopupCentered());
        Button("FLIGHT MANUAL",new(x+446,actionY,190,52),()=>help=true);
        if(customName!="")Text("CUSTOM ARENA  /  "+customName,new(x,actionY+85),16,mint);
        if(error!="")Wrapped(error,new(x,actionY+90),Size.X-2*x,16,red);
        float foot=Size.Y-75;
        Small("ARROWS  Rotate & thrust     SPACE  Fire     1 / 2  Weapons",x,foot);
        Small("F11  Fullscreen     M  Sound "+(mutedAudio?"off":"on")+"     V  "+(legacy?"Original ship art":"Vector ship art"),x,foot+25);
        Small("PROTOTYPE  0.2",Size.X-190,foot+25);
    }
    void DrawHud()
    {
        Box(new Rect2(22,22,310,91),new Color(bg,.94f));
        Text("ATOMICA",new(40,51),22,ink);Small(arena.Name.ToUpperInvariant(),40,73);
        Small("CLEAR ALL HOSTILES",40,96);
        Box(new Rect2(Size.X/2-140,22,280,66),new Color(bg,.94f));
        Text($"{sim.Kills:00} / {sim.EnemyCount:00}  ELIMINATED",new(Size.X/2-117,49),19,mint);
        Small(TimeSpan.FromSeconds(sim.Elapsed).ToString(@"mm\:ss")+"  ELAPSED",Size.X/2-117,72);
        Button("II  PAUSE",new(Size.X-148,22,126,44),()=>paused=true);
        float bottom=Size.Y-106;
        Box(new Rect2(22,bottom,284,84),new Color(bg,.95f));
        Small("HULL INTEGRITY",40,bottom+25);
        Text((100*sim.Player.Health/tuning.Health)+"%",new(242,bottom+26),18,sim.Player.Health<35?red:mint);
        DrawRect(new Rect2(40,bottom+40,246,7),line);
        DrawRect(new Rect2(40,bottom+40,246f*sim.Player.Health/tuning.Health,7),sim.Player.Health<35?red:mint);
        Small($"VELOCITY  {sim.Player.Velocity.Length():0000} px/s",40,bottom+69);
        Box(new Rect2(322,bottom,308,84),new Color(bg,.95f));
        Small("ACTIVE WEAPON  /  "+(weapon==0?"01":"02"),340,bottom+25);
        Text(weapon==0?"NORMAL":"RICOCHET",new(340,bottom+52),23,weapon==0?mint:amber);
        Small(weapon==0?$"{tuning.NormalDamage} DAMAGE  /  DIRECT FIRE":$"{tuning.BouncingDamage} DAMAGE  /  {tuning.MaxBounces} BOUNCES",340,bottom+72);
        Small("1 / 2  SWITCH     F1  HELP",650,Size.Y-41);
        DrawRadar(new Rect2(Size.X-284,Size.Y-238,262,216));
    }
    void DrawRadar(Rect2 r)
    {
        Box(r,new Color(bg,.96f));Small("SECTOR RADAR",r.Position.X+14,r.Position.Y+25);
        float scale=Math.Min((r.Size.X-28)/arena.Width,(r.Size.Y-56)/arena.Height);
        Vector2 dims=new(arena.Width*scale,arena.Height*scale);
        Vector2 origin=r.Position+new Vector2((r.Size.X-dims.X)/2,39+(r.Size.Y-56-dims.Y)/2);
        if(radarTexture!=null)DrawTextureRect(radarTexture,new Rect2(origin,dims),false);
        var vOrigin=G(arena.Radar(new V(camera.X-Size.X/2,camera.Y-Size.Y/2),new V(origin.X,origin.Y),new V(dims.X,dims.Y)));
        Vector2 vSize=Size/G(arena.Size)*dims;
        DrawRect(new Rect2(vOrigin,vSize).Intersection(new Rect2(origin,dims)),new Color(mint,.22f),false);
        foreach(var enemy in sim.Enemies)if(enemy.Health>0)DrawCircle(G(arena.Radar(enemy.Position,new(origin.X,origin.Y),new(dims.X,dims.Y))),2.7f,red);
        Vector2 p=G(arena.Radar(sim.Player.Position,new(origin.X,origin.Y),new(dims.X,dims.Y)));
        DrawCircle(p,4,mint);DrawArc(p,7,0,MathF.Tau,18,new Color(mint,.45f));
    }
    void DrawOverlay()
    {
        DrawRect(new Rect2(Vector2.Zero,Size),new Color(bg,.8f));
        var r=new Rect2(Size/2-new Vector2(270,205),new Vector2(540,410));Box(r,panel);
        string title=paused?"FLIGHT PAUSED":sim.State==MatchState.Victory?"SECTOR SECURED":"SIGNAL LOST";
        Color c=sim.State==MatchState.Defeat?red:mint;
        Small("ATOMICA  /  MISSION CONTROL",r.Position.X+36,r.Position.Y+42);
        Text(title,r.Position+new Vector2(36,99),34,c);
        Text(paused?"Take a breath. Your ship will wait.":sim.State==MatchState.Victory?"All hostiles eliminated. Excellent flying.":"Your hull failed. Regroup and launch again.",r.Position+new Vector2(36,138),17,ink);
        Small(sim.Kills+" ELIMINATED   /   "+TimeSpan.FromSeconds(sim.Elapsed).ToString(@"mm\:ss")+" ELAPSED",r.Position.X+36,r.Position.Y+171);
        Button(paused?"RESUME  /  ENTER":"LAUNCH AGAIN  /  ENTER",new(r.Position+new Vector2(36,204),new Vector2(468,50)),()=>{if(paused)paused=false;else Start();},true);
        Button("RESTART",new(r.Position+new Vector2(36,271),new Vector2(224,46)),Start);
        Button("ARENA SELECT",new(r.Position+new Vector2(280,271),new Vector2(224,46)),()=>{menu=true;paused=false;});
        Small("F1  Flight manual     M  "+(mutedAudio?"Sound off":"Sound on"),r.Position.X+36,r.Position.Y+366);
    }
    void DrawHelp()
    {
        buttons.Clear();DrawRect(new Rect2(Vector2.Zero,Size),new Color(bg,.96f));
        Vector2 p=Size/2-new Vector2(350,300);
        Small("PILOT BRIEFING",p.X,p.Y);Text("THE FLIGHT MANUAL",p+new Vector2(0,51),36,ink);
        string[] rows={"LEFT / RIGHT       Rotate the ship","UP / DOWN          Forward / reverse thrust","SPACE              Fire  ·  Hold for continuous fire","1 / 2              Normal / ricochet weapon","ESC                Pause / resume","F11                Fullscreen","M / V              Toggle sound / original ship art"};
        for(int i=0;i<rows.Length;i++)Text(rows[i],p+new Vector2(0,104+i*36),19,i%2==0?ink:muted);
        Wrapped("Release thrust to coast gently to a stop. Turn to face your direction of travel and use reverse thrust to brake faster. Walls bounce you safely; enemy bullets do not.",p+new Vector2(0,386),700,19,mint);
        Wrapped("Clear every red contact to win. Ricochet rounds deal half damage from launch and bounce up to three times. The radar always shows every living hostile.",p+new Vector2(0,472),700,17,muted);
        Button("READY TO FLY  /  ESC",new(p+new Vector2(0,545),new Vector2(300,50)),()=>help=false,true);
    }
    void Wrapped(string text,Vector2 p,float width,int size,Color color)
    {
        string current="";
        foreach(string word in text.Split(' '))
        {
            if(font.GetStringSize(current+word,HorizontalAlignment.Left,-1,size).X>width){Text(current,p,size,color);p.Y+=size*1.5f;current="";}
            current+=word+" ";
        }
        Text(current,p,size,color);
    }
    AudioStreamWav Tone(bool impact)
    {
        const int rate=22050;int samples=(int)(rate*(impact?.14f:.075f));byte[] data=new byte[samples*2];
        var noise=new Random(23);
        for(int i=0;i<samples;i++)
        {
            float t=(float)i/samples;
            double signal=impact?(noise.NextDouble()*2-1)*.5+Math.Sin(i*.05)*.5:Math.Sin(MathF.Tau*(1000-650*t)*i/rate);
            short value=(short)(signal*(1-t)*(1-t)*16000);data[i*2]=(byte)value;data[i*2+1]=(byte)(value>>8);
        }
        return new AudioStreamWav{Format=AudioStreamWav.FormatEnum.Format16Bits,MixRate=rate,Data=data};
    }
}
