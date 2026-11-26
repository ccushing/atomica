using System.Numerics;

namespace Atomica.Core;

public readonly record struct Hit(float Time, Vector2 Normal);

public static class Collision
{
    // Continuous sweep against expanded tile boxes. Ships use a conservative square hull.
    public static Hit? Wall(Arena map, Vector2 position, Vector2 delta, float radius)
    {
        Vector2 end = position+delta;
        int left = Math.Max(-1,(int)MathF.Floor((Math.Min(position.X,end.X)-radius)/Arena.Cell));
        int right = Math.Min(map.Width,(int)MathF.Floor((Math.Max(position.X,end.X)+radius)/Arena.Cell));
        int top = Math.Max(-1,(int)MathF.Floor((Math.Min(position.Y,end.Y)-radius)/Arena.Cell));
        int bottom = Math.Min(map.Height,(int)MathF.Floor((Math.Max(position.Y,end.Y)+radius)/Arena.Cell));
        Hit? closest = null;
        for (int y=top;y<=bottom;y++) for (int x=left;x<=right;x++)
        {
            if (!map.IsWall(x,y)) continue;
            var hit = Box(position,delta,new Vector2(x*Arena.Cell-radius,y*Arena.Cell-radius),new Vector2((x+1)*Arena.Cell+radius,(y+1)*Arena.Cell+radius));
            if (hit is not { } h) continue;
            if (closest is not { } previous || h.Time < previous.Time-0.00001f) closest=h;
            else if (Math.Abs(h.Time-previous.Time)<0.00001f)
            {
                var n = previous.Normal+h.Normal;
                if (n.LengthSquared()>0) closest = new(h.Time,Vector2.Normalize(n));
            }
        }
        return closest;
    }

    static Hit? Box(Vector2 p, Vector2 d, Vector2 min, Vector2 max)
    {
        float enter=float.NegativeInfinity, exit=float.PositiveInfinity;
        Vector2 normal=Vector2.Zero;
        for (int axis=0;axis<2;axis++)
        {
            float v=axis==0?d.X:d.Y, o=axis==0?p.X:p.Y, lo=axis==0?min.X:min.Y, hi=axis==0?max.X:max.Y;
            if (Math.Abs(v)<1e-8f) { if(o<=lo || o>=hi) return null; continue; }
            float a=(lo-o)/v,b=(hi-o)/v;
            Vector2 n=axis==0?new(-Math.Sign(v),0):new(0,-Math.Sign(v));
            if(a>b) (a,b)=(b,a);
            if(a>enter+0.000001f) {enter=a;normal=n;}
            else if(Math.Abs(a-enter)<0.000001f) normal=Vector2.Normalize(normal+n);
            exit=Math.Min(exit,b);
            if(enter>exit) return null;
        }
        if(enter < -0.00001f || enter>1 || exit<0 || normal==Vector2.Zero || Vector2.Dot(d,normal)>=0) return null;
        return new(Math.Max(0,enter),normal);
    }

    public static void Move(Arena map, ref Vector2 position, ref Vector2 velocity, float dt, float radius)
    {
        float left=dt;
        for(int i=0;i<16 && left>0.0000001f;i++)
        {
            Vector2 delta=velocity*left;
            var hit=Wall(map,position,delta,radius);
            if(hit is not { } h) { position+=delta; return; }
            position+=delta*h.Time+h.Normal*.01f;
            velocity=Vector2.Reflect(velocity,h.Normal);
            left*=1-h.Time;
        }
        // At pathological speeds discard unresolved travel instead of crossing a wall.
    }

    public static float? Circle(Vector2 p, Vector2 d, Vector2 center, float radius)
    {
        Vector2 m=p-center;
        float c=Vector2.Dot(m,m)-radius*radius;
        if(c<=0) return 0;
        float a=Vector2.Dot(d,d); if(a<1e-9f) return null;
        float b=Vector2.Dot(m,d), disc=b*b-a*c;
        if(disc<0) return null;
        float t=(-b-MathF.Sqrt(disc))/a;
        return t>=0 && t<=1?t:null;
    }
}
