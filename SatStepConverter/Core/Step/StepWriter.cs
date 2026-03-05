using System.Globalization;
using System.Text;
using SatStepConverter.Core.Model;

namespace SatStepConverter.Core.Step;

public sealed class StepWriter
{
    public void Write(SatDocument document, Stream output)
    {
        using var writer = new StreamWriter(output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);

        var context = new StepContext();

        writer.WriteLine("ISO-10303-21;");
        writer.WriteLine("HEADER;");
        writer.WriteLine("FILE_DESCRIPTION(('SAT TO STEP CONVERTER'), '2;1');");
        writer.WriteLine(
            $"FILE_NAME('converted.step','{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}',(),(), 'SatStepConverter', '', '');");
        writer.WriteLine("FILE_SCHEMA(('CONFIG_CONTROL_DESIGN'));");
        writer.WriteLine("ENDSEC;");
        writer.WriteLine("DATA;");

        foreach (var body in document.Bodies)
        {
            context.WriteBody(body);
        }

        writer.Write(context.Data.ToString());

        writer.WriteLine("ENDSEC;");
        writer.WriteLine("END-ISO-10303-21;");
    }

    private sealed class StepContext
    {
        private int _nextId = 1;
        public StringBuilder Data { get; } = new();

        private readonly Dictionary<Point3D, int> _points = new();
        private readonly Dictionary<Vector3D, int> _directions = new();
        private readonly Dictionary<AxisPlacement3D, int> _placements = new();
        private readonly Dictionary<Curve3D, int> _curves = new(ReferenceEqualityComparer<Curve3D>.Instance);
        private readonly Dictionary<Surface, int> _surfaces = new(ReferenceEqualityComparer<Surface>.Instance);
        private readonly Dictionary<Vertex, int> _vertices = new(ReferenceEqualityComparer<Vertex>.Instance);
        private readonly Dictionary<Edge, int> _edges = new(ReferenceEqualityComparer<Edge>.Instance);
        private readonly Dictionary<OrientedEdge, int> _orientedEdges = new(ReferenceEqualityComparer<OrientedEdge>.Instance);
        private readonly Dictionary<EdgeLoop, int> _edgeLoops = new(ReferenceEqualityComparer<EdgeLoop>.Instance);
        private readonly Dictionary<Face, int> _faces = new(ReferenceEqualityComparer<Face>.Instance);
        private readonly Dictionary<Shell, int> _shells = new(ReferenceEqualityComparer<Shell>.Instance);
        private readonly Dictionary<Body, int> _bodies = new(ReferenceEqualityComparer<Body>.Instance);

        private int NextId() => _nextId++;

        private static string Format(double value) =>
            value.ToString("G17", CultureInfo.InvariantCulture);

        public void WriteBody(Body body)
        {
            if (_bodies.ContainsKey(body))
            {
                return;
            }

            if (body.Shells.Count == 0)
            {
                return;
            }

            var shellId = WriteShell(body.Shells[0]);
            var id = NextId();
            _bodies[body] = id;
            Data.AppendLine($"#{id}=MANIFOLD_SOLID_BREP('',#{shellId});");
        }

        private int WriteShell(Shell shell)
        {
            if (_shells.TryGetValue(shell, out var existing))
            {
                return existing;
            }

            var faceIds = new List<int>(shell.Faces.Count);
            foreach (var face in shell.Faces)
            {
                faceIds.Add(WriteFace(face));
            }

            var id = NextId();
            _shells[shell] = id;
            Data.AppendLine($"#{id}=CLOSED_SHELL('',({string.Join(",", faceIds.Select(i => $"#{i}"))}));");
            return id;
        }

        private int WriteFace(Face face)
        {
            if (_faces.TryGetValue(face, out var existing))
            {
                return existing;
            }

            if (face.OuterLoop is null)
            {
                throw new InvalidOperationException("Face must have an outer loop to be written to STEP.");
            }

            var loopId = WriteEdgeLoop(face.OuterLoop);
            var boundId = NextId();
            Data.AppendLine($"#{boundId}=FACE_OUTER_BOUND('',#{loopId},.T.);");

            var surfaceId = WriteSurface(face.Surface);

            var id = NextId();
            _faces[face] = id;
            Data.AppendLine($"#{id}=ADVANCED_FACE('',(#{boundId}),#{surfaceId},.T.);");
            return id;
        }

        private int WriteEdgeLoop(EdgeLoop loop)
        {
            if (_edgeLoops.TryGetValue(loop, out var existing))
            {
                return existing;
            }

            var orientedIds = new List<int>(loop.Edges.Count);
            foreach (var oriented in loop.Edges)
            {
                orientedIds.Add(WriteOrientedEdge(oriented));
            }

            var id = NextId();
            _edgeLoops[loop] = id;
            Data.AppendLine($"#{id}=EDGE_LOOP('',({string.Join(",", orientedIds.Select(i => $"#{i}"))}));");
            return id;
        }

        private int WriteOrientedEdge(OrientedEdge oriented)
        {
            if (_orientedEdges.TryGetValue(oriented, out var existing))
            {
                return existing;
            }

            var edgeId = WriteEdge(oriented.Edge);
            var id = NextId();
            _orientedEdges[oriented] = id;
            var sameSense = oriented.SameSense ? ".T." : ".F.";
            Data.AppendLine($"#{id}=ORIENTED_EDGE('',*,*,#{edgeId},{sameSense});");
            return id;
        }

        private int WriteEdge(Edge edge)
        {
            if (_edges.TryGetValue(edge, out var existing))
            {
                return existing;
            }

            var v1Id = WriteVertex(edge.Start);
            var v2Id = WriteVertex(edge.End);

            if (edge.Curve is null)
            {
                throw new InvalidOperationException("Edge must have an associated curve to be written to STEP.");
            }

            var curveId = WriteCurve(edge.Curve);
            var id = NextId();
            _edges[edge] = id;
            Data.AppendLine($"#{id}=EDGE_CURVE('',#{v1Id},#{v2Id},#{curveId},.T.);");
            return id;
        }

        private int WriteVertex(Vertex vertex)
        {
            if (_vertices.TryGetValue(vertex, out var existing))
            {
                return existing;
            }

            var pointId = WritePoint(vertex.Point);
            var id = NextId();
            _vertices[vertex] = id;
            Data.AppendLine($"#{id}=VERTEX_POINT('',#{pointId});");
            return id;
        }

        private int WritePoint(Point3D point)
        {
            if (_points.TryGetValue(point, out var existing))
            {
                return existing;
            }

            var id = NextId();
            _points[point] = id;
            Data.AppendLine($"#{id}=CARTESIAN_POINT('',({Format(point.X)},{Format(point.Y)},{Format(point.Z)}));");
            return id;
        }

        private int WriteDirection(Vector3D vector)
        {
            if (_directions.TryGetValue(vector, out var existing))
            {
                return existing;
            }

            var id = NextId();
            _directions[vector] = id;
            Data.AppendLine($"#{id}=DIRECTION('',({Format(vector.X)},{Format(vector.Y)},{Format(vector.Z)}));");
            return id;
        }

        private int WriteAxisPlacement(AxisPlacement3D placement)
        {
            if (_placements.TryGetValue(placement, out var existing))
            {
                return existing;
            }

            var originId = WritePoint(placement.Origin);
            var axisId = WriteDirection(placement.Axis);
            var refDirId = WriteDirection(placement.RefDirection);

            var id = NextId();
            _placements[placement] = id;
            Data.AppendLine($"#{id}=AXIS2_PLACEMENT_3D('',#{originId},#{axisId},#{refDirId});");
            return id;
        }

        private int WriteCurve(Curve3D curve)
        {
            if (_curves.TryGetValue(curve, out var existing))
            {
                return existing;
            }

            var id = NextId();
            _curves[curve] = id;

            switch (curve)
            {
                case LineCurve line:
                {
                    var originId = WritePoint(line.Origin);
                    var dirId = WriteDirection(line.Direction);
                    var vectorId = NextId();
                    Data.AppendLine($"#{vectorId}=VECTOR('',#{dirId},1.0);");
                    Data.AppendLine($"#{id}=LINE('',#{originId},#{vectorId});");
                    break;
                }
                case CircleCurve circle:
                {
                    var placementId = WriteAxisPlacement(circle.Position);
                    Data.AppendLine($"#{id}=CIRCLE('',#{placementId},{Format(circle.Radius)});");
                    break;
                }
                case NurbsCurve nurbs:
                {
                    var pointIds = nurbs.ControlPoints.Select(WritePoint).ToArray();
                    var weightsList = nurbs.Weights.Count == pointIds.Length
                        ? nurbs.Weights
                        : Enumerable.Repeat(1.0, pointIds.Length).ToArray();

                    var degree = nurbs.Degree;
                    var knotValues = nurbs.Knots.ToArray();
                    var multiplicities = new int[knotValues.Length];
                    Array.Fill(multiplicities, 1);

                    var controlPointRefs = string.Join(",", pointIds.Select(i => $"#{i}"));
                    var weights = string.Join(",", weightsList.Select(Format));
                    var mults = string.Join(",", multiplicities);
                    var knots = string.Join(",", knotValues.Select(Format));

                    Data.AppendLine(
                        $"#{id}=B_SPLINE_CURVE_WITH_KNOTS('',{degree},({controlPointRefs}),.UNSPECIFIED.,.F.,.F.,({mults}),({knots}),.UNSPECIFIED.);");
                    break;
                }
                default:
                    throw new NotSupportedException($"Unsupported curve type: {curve.GetType().Name}");
            }

            return id;
        }

        private int WriteSurface(Surface surface)
        {
            if (_surfaces.TryGetValue(surface, out var existing))
            {
                return existing;
            }

            var id = NextId();
            _surfaces[surface] = id;

            switch (surface)
            {
                case PlaneSurface plane:
                {
                    var placementId = WriteAxisPlacement(plane.Position);
                    Data.AppendLine($"#{id}=PLANE('',#{placementId});");
                    break;
                }
                case CylindricalSurface cylinder:
                {
                    var placementId = WriteAxisPlacement(cylinder.Position);
                    Data.AppendLine($"#{id}=CYLINDRICAL_SURFACE('',#{placementId},{Format(cylinder.Radius)});");
                    break;
                }
                case NurbsSurface nurbs:
                {
                    var uDegree = nurbs.UDegree;
                    var vDegree = nurbs.VDegree;

                    var rows = new List<string>();
                    foreach (var row in nurbs.ControlPoints)
                    {
                        var ids = row.Select(WritePoint).ToArray();
                        rows.Add($"({string.Join(",", ids.Select(i => $"#{i}"))})");
                    }

                    var uKnots = string.Join(",", nurbs.UKnots.Select(Format));
                    var vKnots = string.Join(",", nurbs.VKnots.Select(Format));

                    var uMults = string.Join(",", Enumerable.Repeat(1, nurbs.UKnots.Count));
                    var vMults = string.Join(",", Enumerable.Repeat(1, nurbs.VKnots.Count));

                    Data.AppendLine(
                        $"#{id}=B_SPLINE_SURFACE_WITH_KNOTS('',{uDegree},{vDegree},({string.Join(",", rows)}),.UNSPECIFIED.,.UNSPECIFIED.,.F.,.F.,({uMults}),({vMults}),({uKnots}),({vKnots}),.UNSPECIFIED.);");
                    break;
                }
                default:
                    throw new NotSupportedException($"Unsupported surface type: {surface.GetType().Name}");
            }

            return id;
        }
    }
}

internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
    where T : class
{
    public static ReferenceEqualityComparer<T> Instance { get; } = new();

    public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

    public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}

