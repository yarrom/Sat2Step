
using System;
using System.Collections.Generic;

public record GPoint(double X, double Y, double Z);
public record GDirection(double X, double Y, double Z);

public abstract class Entity { public int Id; }

public class StraightCurve : Entity
{
    public GPoint Origin;
    public GDirection Dir;
    public StraightCurve(int id, GPoint o, GDirection d) { Id = id; Origin = o; Dir = d; }
}

public class PlaneSurface : Entity
{
    public GPoint Origin;
    public GDirection Normal;
    public bool ForwardV;
    public PlaneSurface(int id, GPoint o, GDirection n, bool fv) { Id = id; Origin = o; Normal = n; ForwardV = fv; }
}

public class Vertex : Entity
{
    public GPoint P;
    public Vertex(int id, GPoint p) { Id = id; P = p; }
}

public class Edge : Entity
{
    public int CurveId;
    public StraightCurve? Curve;
    public double ParamA, ParamB;
    public int? VStartId, VEndId;
    public Vertex? VStart, VEnd;
    public Edge(int id) { Id = id; }
}

public class Coedge : Entity
{
    public int EdgeId;
    public Edge? Edge;
    public bool Reversed;
    public int LoopId;
    public Coedge(int id) { Id = id; }
}

public class LoopEnt : Entity
{
    public List<int> CoedgeIds = new();
    public List<Coedge> Coedges = new();
    public LoopEnt(int id) { Id = id; }
}

public class Face : Entity
{
    public int SurfaceId;
    public PlaneSurface? Surface;
    public int OuterLoopId;
    public LoopEnt? OuterLoop;
    public bool Reversed;
    public Face(int id) { Id = id; }
}

public class ModelRoot
{
    public Registry Reg = new();
    public List<Entity> Entities = new();
    public void Add(Entity e) { Entities.Add(e); Reg.Add(e.Id, e); }
    public int EntitiesCount() => Entities.Count;
}

