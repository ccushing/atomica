using System.Numerics;

namespace Atomica.Core;

public sealed class Arena
{
    public const int Cell = 32;
    public int Width { get; }
    public int Height { get; }
    public bool[,] Walls { get; }
    public Vector2 PlayerStart { get; private set; }
    public List<Vector2> EnemyStarts { get; } = new();
    public string Name { get; }
    public Vector2 Size => new(Width * Cell, Height * Cell);

    private Arena(int width, int height, string name)
    { Width = width; Height = height; Name = name; Walls = new bool[width, height]; }

    public static Arena Parse(string csv, string name = "Custom arena")
    {
        var rows = csv.Trim().Split('\n');
        if (rows.Length < 3 || rows.Length > 512) throw new FormatException("Map must have 3 to 512 rows.");
        int width = rows[0].Split(',').Length;
        if (width < 3 || width > 512) throw new FormatException("Map must have 3 to 512 columns.");
        var map = new Arena(width, rows.Length, name);
        int starts = 0;
        for (int y = 0; y < rows.Length; y++)
        {
            var cells = rows[y].Split(',');
            if (cells.Length != width) throw new FormatException($"Row {y+1}: expected {width} columns, found {cells.Length}.");
            for (int x = 0; x < width; x++)
            {
                string token = cells[x].Trim().TrimStart('\uFEFF');
                if (token is not ("0" or "1" or "2" or "3")) throw new FormatException($"Row {y+1}, column {x+1}: unknown token '{token}'. Use 0, 1, 2 or 3.");
                if ((x == 0 || y == 0 || x == width-1 || y == rows.Length-1) && token != "1")
                    throw new FormatException($"Row {y+1}, column {x+1}: the outer perimeter must be walls (1).");
                map.Walls[x,y] = token == "1";
                var center = new Vector2((x+.5f)*Cell, (y+.5f)*Cell);
                if (token == "2") { map.PlayerStart = center; starts++; }
                if (token == "3") map.EnemyStarts.Add(center);
            }
        }
        if (starts != 1) throw new FormatException($"Expected exactly one player start (2); found {starts}.");
        if (map.EnemyStarts.Count == 0) throw new FormatException("Add at least one enemy start (3).");
        // Ship radius is 12, so a centered ship fits inside a 32-pixel empty cell.
        var reachable = map.Reachable(map.PlayerStart);
        foreach (var spawn in map.EnemyStarts)
            if (!reachable.Contains(map.Key(spawn)))
                throw new FormatException($"Row {(int)spawn.Y/Cell+1}, column {(int)spawn.X/Cell+1}: enemy spawn is unreachable from the player.");
        return map;
    }

    public bool IsWall(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height || Walls[x,y];
    public int Key(Vector2 p) => (int)(p.Y/Cell)*Width + (int)(p.X/Cell);
    public Vector2 Center(int key) => new((key%Width+.5f)*Cell, (key/Width+.5f)*Cell);
    public Vector2 Radar(Vector2 point, Vector2 origin, Vector2 size) => origin + point / Size * size;

    public List<Vector2> SelectEnemySpawns(int count)
    {
        if(count<1 || count>15) throw new ArgumentOutOfRangeException(nameof(count),"Choose 1 to 15 enemies.");
        var chosen=new List<Vector2>();
        // Prefer authored spawn markers; use distinct reachable cell centers for additional ships.
        var candidates=Reachable(PlayerStart).Where(k=>k!=Key(PlayerStart)).OrderBy(k=>k).Select(Center).ToList();
        if(candidates.Count<count) throw new FormatException($"This arena has room for only {candidates.Count} enemies. Choose fewer ships.");
        var authored=new List<Vector2>(EnemyStarts);
        while(chosen.Count<count)
        {
            var pool=authored.Count>0?authored:candidates;
            Vector2 best=pool[0]; float bestDistance=-1;
            foreach(var p in pool)
            {
                float distance=Vector2.DistanceSquared(p,PlayerStart);
                foreach(var used in chosen) distance=Math.Min(distance,Vector2.DistanceSquared(p,used));
                if(distance>bestDistance){best=p;bestDistance=distance;}
            }
            chosen.Add(best);authored.Remove(best);candidates.Remove(best);
        }
        return chosen;
    }

    private HashSet<int> Reachable(Vector2 start)
    {
        var visited = new HashSet<int> { Key(start) };
        var queue = new Queue<int>(); queue.Enqueue(Key(start));
        while (queue.TryDequeue(out int k))
            foreach (int n in Neighbors(k)) if (visited.Add(n)) queue.Enqueue(n);
        return visited;
    }

    public IEnumerable<int> Neighbors(int key)
    {
        int x = key % Width, y = key / Width;
        if (!IsWall(x+1,y)) yield return key+1;
        if (!IsWall(x-1,y)) yield return key-1;
        if (!IsWall(x,y+1)) yield return key+Width;
        if (!IsWall(x,y-1)) yield return key-Width;
    }

    public List<Vector2> Path(Vector2 from, Vector2 to)
    {
        int start = Key(from), goal = Key(to);
        var frontier = new PriorityQueue<int,float>(); frontier.Enqueue(start,0);
        var cost = new Dictionary<int,int> { [start] = 0 };
        var came = new Dictionary<int,int>();
        while (frontier.TryDequeue(out int k,out _) && cost.Count < 50000)
        {
            if (k == goal)
            {
                var path = new List<Vector2>();
                while (k != start) { path.Add(Center(k)); k = came[k]; }
                path.Reverse(); return path;
            }
            foreach (int n in Neighbors(k))
            {
                int next = cost[k]+1;
                if (cost.TryGetValue(n,out int old) && old <= next) continue;
                cost[n] = next; came[n] = k;
                float h = Math.Abs(n%Width-goal%Width)+Math.Abs(n/Width-goal/Width);
                frontier.Enqueue(n,next+h);
            }
        }
        return new();
    }
}
