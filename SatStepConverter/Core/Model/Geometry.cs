namespace SatStepConverter.Core.Model;

public readonly record struct Point3D(double X, double Y, double Z);

public readonly record struct Vector3D(double X, double Y, double Z)
{
    public static Vector3D UnitX => new(1, 0, 0);
    public static Vector3D UnitY => new(0, 1, 0);
    public static Vector3D UnitZ => new(0, 0, 1);
}

public sealed class AxisPlacement3D
{
    public Point3D Origin { get; init; }
    public Vector3D Axis { get; init; } = Vector3D.UnitZ;
    public Vector3D RefDirection { get; init; } = Vector3D.UnitX;
}

public sealed class Plane
{
    public AxisPlacement3D Position { get; init; } = new();
}

public abstract class Curve3D
{
}

public sealed class LineCurve : Curve3D
{
    public Point3D Origin { get; init; }
    public Vector3D Direction { get; init; } = Vector3D.UnitX;
}

public sealed class CircleCurve : Curve3D
{
    public AxisPlacement3D Position { get; init; } = new();
    public double Radius { get; init; }
}

public sealed class NurbsCurve : Curve3D
{
    public int Degree { get; init; }
    public IReadOnlyList<Point3D> ControlPoints { get; init; } = Array.Empty<Point3D>();
    public IReadOnlyList<double> Weights { get; init; } = Array.Empty<double>();
    public IReadOnlyList<double> Knots { get; init; } = Array.Empty<double>();
    public bool IsClosed { get; init; }
    public bool IsPeriodic { get; init; }
}

public abstract class Surface
{
}

public sealed class PlaneSurface : Surface
{
    public AxisPlacement3D Position { get; init; } = new();
}

public sealed class CylindricalSurface : Surface
{
    public AxisPlacement3D Position { get; init; } = new();
    public double Radius { get; init; }
}

public sealed class NurbsSurface : Surface
{
    public int UDegree { get; init; }
    public int VDegree { get; init; }
    public IReadOnlyList<IReadOnlyList<Point3D>> ControlPoints { get; init; } = Array.Empty<IReadOnlyList<Point3D>>();
    public IReadOnlyList<IReadOnlyList<double>> Weights { get; init; } = Array.Empty<IReadOnlyList<double>>();
    public IReadOnlyList<double> UKnots { get; init; } = Array.Empty<double>();
    public IReadOnlyList<double> VKnots { get; init; } = Array.Empty<double>();
    public bool UClosed { get; init; }
    public bool VClosed { get; init; }
    public bool UPeriodic { get; init; }
    public bool VPeriodic { get; init; }
}

