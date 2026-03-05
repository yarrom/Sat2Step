
using System.Globalization;

namespace SimpleSatParser
{

    public class SatParser
    {
        // Опираемся на простые эвристики, поддерживаем forward refs через Registry.WhenAvailable
        public ModelRoot Parse(string text)
        {
            var root = new ModelRoot();

            foreach (var line in SatLexer.Lines(text))
            {
                var tokens = SatLexer.Tokens(line);
                if (tokens.Count < 2) continue;

                // Часто строки начинаются с идентификатора (целое) затем имя типа
                if (!int.TryParse(tokens[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                {
                    // Возможно некоторые строки имеют префикс типа "/* ... */" или прочее — пропускаем
                    continue;
                }

                var type = tokens[1].ToLowerInvariant();
                var rest = line.Substring(line.IndexOf(tokens[1]) + tokens[1].Length).Trim();

                switch (type)
                {
                    case "straight-curve":
                    case "straight_curve":
                        ParseStraightCurve(id, rest, root);
                        break;
                    case "plane-surface":
                    case "plane_surface":
                        ParsePlaneSurface(id, rest, root);
                        break;
                    case "vertex":
                        ParseVertex(id, rest, root);
                        break;
                    case "edge":
                        ParseEdge(id, rest, root);
                        break;
                    case "coedge":
                        ParseCoedge(id, rest, root);
                        break;
                    case "loop":
                        ParseLoop(id, rest, root);
                        break;
                    case "face":
                        ParseFace(id, rest, root);
                        break;
                    default:
                        // попытаться угадать based on keywords
                        if (type.Contains("curve"))
                            ParseStraightCurve(id, rest, root);
                        else if (type.Contains("surface") || type.Contains("plane"))
                            ParsePlaneSurface(id, rest, root);
                        else if (type.Contains("vertex"))
                            ParseVertex(id, rest, root);
                        else
                        {
                            // игнор						
                        }
                        break;
                }
            }

            return root;
        }

        private static double[] ParseDoubles(string s)
        {
            var parts = s.Split(new[] { ' ', '\t', ',', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<double>();
            foreach (var p in parts)
            {
                if (double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) list.Add(v);
            }
            return list.ToArray();
        }

        private void ParseStraightCurve(int id, string rest, ModelRoot root)
        {
            var nums = ParseDoubles(rest);
            if (nums.Length >= 6)
            {
                var o = new GPoint(nums[0], nums[1], nums[2]);
                var d = new GDirection(nums[3], nums[4], nums[5]);
                var sc = new StraightCurve(id, o, d);
                root.Add(sc);
            }
            else
            {
                // placeholder — создаём с нуля
                root.Add(new StraightCurve(id, new GPoint(0, 0, 0), new GDirection(1, 0, 0)));
            }
        }

        private void ParsePlaneSurface(int id, string rest, ModelRoot root)
        {
            var nums = ParseDoubles(rest);
            if (nums.Length >= 6)
            {
                var origin = new GPoint(nums[0], nums[1], nums[2]);
                var normal = new GDirection(nums[3], nums[4], nums[5]);
                var fv = rest.Contains("forward_v") || rest.Contains("forward");
                root.Add(new PlaneSurface(id, origin, normal, fv));
            }
            else
            {
                root.Add(new PlaneSurface(id, new GPoint(0, 0, 0), new GDirection(0, 0, 1), true));
            }
        }

        private void ParseVertex(int id, string rest, ModelRoot root)
        {
            var nums = ParseDoubles(rest);
            if (nums.Length >= 3)
                root.Add(new Vertex(id, new GPoint(nums[0], nums[1], nums[2])));
            else
                root.Add(new Vertex(id, new GPoint(0, 0, 0)));
        }

        private void ParseEdge(int id, string rest, ModelRoot root)
        {
            var tokens = rest.Split(new[] { ' ', '\t', ',', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            var e = new Edge(id);

            // пытаемся найти ссылку на curve — обычно целое число
            foreach (var t in tokens)
            {
                if (int.TryParse(t, out var iv))
                {
                    // heuristics: если число отрицательное в SAT оно тоже может быть ссылкой, принимаем любой ненулевой
                    if (iv != 0 && e.CurveId == 0)
                        e.CurveId = Math.Abs(iv);
                }
            }

            var nums = ParseDoubles(rest);
            if (nums.Length >= 2)
            {
                e.ParamA = nums[0];
                e.ParamB = nums[1];
            }

            // попробуем обнаружить вершины в паттертах like "vertex 123" или простые целые после параметров
            var ints = new List<int>();
            foreach (var t in tokens)
                if (int.TryParse(t, out var iv)) ints.Add(Math.Abs(iv));
            if (ints.Count >= 3)
            {
                // heuristics: last two ints might be vertex ids
                e.VStartId = ints[ints.Count - 2];
                e.VEndId = ints[ints.Count - 1];
                root.Reg.WhenAvailable(e.VStartId.Value, obj => { if (obj is Vertex v) e.VStart = v; });
                root.Reg.WhenAvailable(e.VEndId.Value, obj => { if (obj is Vertex v) e.VEnd = v; });
            }

            root.Add(e);

            if (e.CurveId != 0)
            {
                root.Reg.WhenAvailable(e.CurveId, obj =>
                {
                    if (obj is StraightCurve sc) e.Curve = sc;
                });
            }
        }

        private void ParseCoedge(int id, string rest, ModelRoot root)
        {
            var tokens = rest.Split(new[] { ' ', '\t', ',', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            var ce = new Coedge(id);
            ce.Reversed = rest.Contains("reversed") || rest.Contains("reverse");
            var ints = new List<int>();
            foreach (var t in tokens)
                if (int.TryParse(t, out var iv)) ints.Add(Math.Abs(iv));
            if (ints.Count >= 1) ce.EdgeId = ints[0];
            if (ints.Count >= 2) ce.LoopId = ints[ints.Count - 1];

            root.Add(ce);

            root.Reg.WhenAvailable(ce.EdgeId, obj => { if (obj is Edge ed) ce.Edge = ed; });
            root.Reg.WhenAvailable(ce.LoopId, obj => { if (obj is LoopEnt l) l.CoedgeIds.Add(ce.Id); });
        }

        private void ParseLoop(int id, string rest, ModelRoot root)
        {
            var l = new LoopEnt(id);
            var tokens = rest.Split(new[] { ' ', '\t', ',', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tokens)
            {
                if (int.TryParse(t, out var iv)) l.CoedgeIds.Add(Math.Abs(iv));
            }
            root.Add(l);

            foreach (var cid in l.CoedgeIds)
                root.Reg.WhenAvailable(cid, obj => { if (obj is Coedge co) l.Coedges.Add(co); });
        }

        private void ParseFace(int id, string rest, ModelRoot root)
        {
            var f = new Face(id);
            var tokens = rest.Split(new[] { ' ', '\t', ',', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            var ints = new List<int>();
            foreach (var t in tokens)
                if (int.TryParse(t, out var iv)) ints.Add(Math.Abs(iv));
            if (ints.Count >= 1) f.SurfaceId = ints[0];
            if (ints.Count >= 2) f.OuterLoopId = ints[ints.Count - 1];
            f.Reversed = rest.Contains("reversed") || rest.Contains("reverse");
            root.Add(f);

            if (f.SurfaceId != 0)
                root.Reg.WhenAvailable(f.SurfaceId, obj => { if (obj is PlaneSurface ps) f.Surface = ps; });
            if (f.OuterLoopId != 0)
                root.Reg.WhenAvailable(f.OuterLoopId, obj => { if (obj is LoopEnt l) f.OuterLoop = l; });
        }
    }

}