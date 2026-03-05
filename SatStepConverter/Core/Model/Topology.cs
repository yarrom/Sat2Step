namespace SatStepConverter.Core.Model;

public sealed class Vertex
{
    public int Id { get; init; }
    public Point3D Point { get; init; }
}

public sealed class Edge
{
    public int Id { get; init; }
    public Vertex Start { get; init; } = null!;
    public Vertex End { get; init; } = null!;
    public Curve3D? Curve { get; init; }
    public double? StartParameter { get; init; }
    public double? EndParameter { get; init; }
}

public sealed class OrientedEdge
{
    public Edge Edge { get; init; } = null!;
    public bool SameSense { get; init; } = true;
}

public sealed class EdgeLoop
{
    public IReadOnlyList<OrientedEdge> Edges { get; init; } = Array.Empty<OrientedEdge>();
}

public sealed class Face
{
    public int Id { get; init; }
    public Surface Surface { get; init; } = new PlaneSurface();
    public EdgeLoop? OuterLoop { get; init; }
}

public sealed class Shell
{
    public int Id { get; init; }
    public IReadOnlyList<Face> Faces { get; init; } = Array.Empty<Face>();
}

public sealed class Body
{
    public int Id { get; init; }
    public IReadOnlyList<Shell> Shells { get; init; } = Array.Empty<Shell>();
}

public sealed class SatDocument
{
    public string Version { get; init; } = string.Empty;
    public string Units { get; init; } = "mm";
    public IReadOnlyList<Body> Bodies { get; init; } = Array.Empty<Body>();
}

