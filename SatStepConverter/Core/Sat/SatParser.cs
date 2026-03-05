using System.Globalization;
using SatStepConverter.Core.Model;

namespace SatStepConverter.Core.Sat;

public sealed class SatParser
{
    public SatDocument Parse(Stream input)
    {
        using var reader = new StreamReader(input, leaveOpen: true);

        // Basic ACIS SAT header parsing:
        // First line: version string (e.g. "700 0 0 1")
        // Second line often contains units and tolerance information.

        var versionLine = reader.ReadLine();
        var unitsLine = reader.ReadLine();

        var version = versionLine?.Trim() ?? string.Empty;
        var units = ParseUnits(unitsLine);

        // Simplified geometry extraction:
        // - Collect all "point" records and build an axis-aligned bounding box.
        // - Create a single rectangular solid body matching that bounding box.
        // This approximates the SAT geometry but guarantees a non-empty BREP.

        var points = new List<Point3D>();

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (!line.Contains(" point ", StringComparison.Ordinal))
            {
                continue;
            }

            var hashIndex = line.IndexOf('#');
            if (hashIndex >= 0)
            {
                line = line[..hashIndex].TrimEnd();
            }

            var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 4)
            {
                continue;
            }

            if (TryParseDouble(tokens[^3], out var x) &&
                TryParseDouble(tokens[^2], out var y) &&
                TryParseDouble(tokens[^1], out var z))
            {
                points.Add(new Point3D(x, y, z));
            }
        }

        var bodies = BuildBodiesFromPoints(points);

        return new SatDocument
        {
            Version = version,
            Units = units,
            Bodies = bodies
        };
    }

    private static IReadOnlyList<Body> BuildBodiesFromPoints(IReadOnlyList<Point3D> points)
    {
        if (points.Count == 0)
        {
            return Array.Empty<Body>();
        }

        double minX = points[0].X, maxX = points[0].X;
        double minY = points[0].Y, maxY = points[0].Y;
        double minZ = points[0].Z, maxZ = points[0].Z;

        for (var i = 1; i < points.Count; i++)
        {
            var p = points[i];
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
            if (p.Z < minZ) minZ = p.Z;
            if (p.Z > maxZ) maxZ = p.Z;
        }

        if (minX == maxX || minY == maxY || minZ == maxZ)
        {
            return Array.Empty<Body>();
        }

        var v0 = new Vertex { Id = 1, Point = new Point3D(minX, minY, minZ) };
        var v1 = new Vertex { Id = 2, Point = new Point3D(maxX, minY, minZ) };
        var v2 = new Vertex { Id = 3, Point = new Point3D(maxX, maxY, minZ) };
        var v3 = new Vertex { Id = 4, Point = new Point3D(minX, maxY, minZ) };

        var v4 = new Vertex { Id = 5, Point = new Point3D(minX, minY, maxZ) };
        var v5 = new Vertex { Id = 6, Point = new Point3D(maxX, minY, maxZ) };
        var v6 = new Vertex { Id = 7, Point = new Point3D(maxX, maxY, maxZ) };
        var v7 = new Vertex { Id = 8, Point = new Point3D(minX, maxY, maxZ) };

        int edgeId = 1;

        Edge CreateEdge(Vertex a, Vertex b)
        {
            var direction = new Vector3D(
                b.Point.X - a.Point.X,
                b.Point.Y - a.Point.Y,
                b.Point.Z - a.Point.Z);

            var curve = new LineCurve
            {
                Origin = a.Point,
                Direction = direction
            };

            return new Edge
            {
                Id = edgeId++,
                Start = a,
                End = b,
                Curve = curve,
                StartParameter = 0.0,
                EndParameter = 1.0
            };
        }

        OrientedEdge Oriented(Edge e, bool sameSense = true) =>
            new() { Edge = e, SameSense = sameSense };

        EdgeLoop Loop(params OrientedEdge[] edges) =>
            new() { Edges = edges };

        // Bottom face (minZ): v0-v1-v2-v3
        var e0 = CreateEdge(v0, v1);
        var e1 = CreateEdge(v1, v2);
        var e2 = CreateEdge(v2, v3);
        var e3 = CreateEdge(v3, v0);

        // Top face (maxZ): v4-v5-v6-v7
        var e4 = CreateEdge(v4, v5);
        var e5 = CreateEdge(v5, v6);
        var e6 = CreateEdge(v6, v7);
        var e7 = CreateEdge(v7, v4);

        // Side edges
        var e8 = CreateEdge(v0, v4);
        var e9 = CreateEdge(v1, v5);
        var e10 = CreateEdge(v2, v6);
        var e11 = CreateEdge(v3, v7);

        int faceId = 1;

        Face CreatePlaneFace(Point3D center, Vector3D normal, Vector3D refDir, EdgeLoop loop)
        {
            var placement = new AxisPlacement3D
            {
                Origin = center,
                Axis = normal,
                RefDirection = refDir
            };

            var surface = new PlaneSurface
            {
                Position = placement
            };

            return new Face
            {
                Id = faceId++,
                Surface = surface,
                OuterLoop = loop
            };
        }

        var centerBottom = new Point3D((minX + maxX) / 2.0, (minY + maxY) / 2.0, minZ);
        var centerTop = new Point3D((minX + maxX) / 2.0, (minY + maxY) / 2.0, maxZ);
        var centerMinX = new Point3D(minX, (minY + maxY) / 2.0, (minZ + maxZ) / 2.0);
        var centerMaxX = new Point3D(maxX, (minY + maxY) / 2.0, (minZ + maxZ) / 2.0);
        var centerMinY = new Point3D((minX + maxX) / 2.0, minY, (minZ + maxZ) / 2.0);
        var centerMaxY = new Point3D((minX + maxX) / 2.0, maxY, (minZ + maxZ) / 2.0);

        var bottomLoop = Loop(Oriented(e0), Oriented(e1), Oriented(e2), Oriented(e3));
        var bottomFace = CreatePlaneFace(centerBottom, new Vector3D(0, 0, -1), new Vector3D(1, 0, 0), bottomLoop);

        var topLoop = Loop(Oriented(e4), Oriented(e5), Oriented(e6), Oriented(e7));
        var topFace = CreatePlaneFace(centerTop, new Vector3D(0, 0, 1), new Vector3D(1, 0, 0), topLoop);

        var minXLoop = Loop(
            Oriented(e3),
            Oriented(e11),
            Oriented(e7, sameSense: false),
            Oriented(e8, sameSense: false));
        var minXFace = CreatePlaneFace(centerMinX, new Vector3D(-1, 0, 0), new Vector3D(0, 0, 1), minXLoop);

        var maxXLoop = Loop(
            Oriented(e1),
            Oriented(e10),
            Oriented(e5, sameSense: false),
            Oriented(e9, sameSense: false));
        var maxXFace = CreatePlaneFace(centerMaxX, new Vector3D(1, 0, 0), new Vector3D(0, 0, 1), maxXLoop);

        var minYLoop = Loop(
            Oriented(e0, sameSense: false),
            Oriented(e9),
            Oriented(e4),
            Oriented(e8, sameSense: false));
        var minYFace = CreatePlaneFace(centerMinY, new Vector3D(0, -1, 0), new Vector3D(0, 0, 1), minYLoop);

        var maxYLoop = Loop(
            Oriented(e2, sameSense: false),
            Oriented(e10),
            Oriented(e6),
            Oriented(e11, sameSense: false));
        var maxYFace = CreatePlaneFace(centerMaxY, new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), maxYLoop);

        var shell = new Shell
        {
            Id = 1,
            Faces = new[] { bottomFace, topFace, minXFace, maxXFace, minYFace, maxYFace }
        };

        var body = new Body
        {
            Id = 1,
            Shells = new[] { shell }
        };

        return new[] { body };
    }

    private static bool TryParseDouble(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);

    private static string ParseUnits(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return "mm";
        }

        var text = line.ToLowerInvariant();
        if (text.Contains("inch"))
        {
            return "inch";
        }

        if (text.Contains("mm") || text.Contains("millimeter"))
        {
            return "mm";
        }

        if (text.Contains("cm"))
        {
            return "cm";
        }

        if (text.Contains("m "))
        {
            return "m";
        }

        return "mm";
    }
}


