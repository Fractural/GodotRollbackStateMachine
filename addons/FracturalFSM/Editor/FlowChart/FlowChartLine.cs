
using System;
using Godot;
using GDC = Godot.Collections;
using Fractural.Utils;
using System.Collections.Generic;

namespace Fractural.FlowChart
{
    [Tool]
    public class FlowChartLine : Container, ISelectable
    {
        // Flowchart Custom style normal, focus, arrow

        public bool selected = false;
        public bool Selected
        {
            get => selected;
            set
            {
                if (selected != value)
                {
                    selected = value;
                    Update();
                }
            }
        }

        public FlowChartLine()
        {
            FocusMode = FocusModeEnum.Click;
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Draw()
        {
            PivotAtLineStart();
            var from = Vector2.Zero;
            from.y += RectSize.y / 2f;
            var to = RectSize;
            to.y -= RectSize.y / 2f;
            var arrow = GetIcon("arrow", "FlowChartLine");
            var tint = Colors.White;
            if (selected)
            {
                tint = this.GetStylebox<StyleBoxFlat>("focus", "FlowChartLine").ShadowColor;
                DrawStyleBox(GetStylebox("focus", "FlowChartLine"), new Rect2(Vector2.Zero, RectSize));
            }
            else
            {
                DrawStyleBox(GetStylebox("normal", "FlowChartLine"), new Rect2(Vector2.Zero, RectSize));
            }
            DrawTexture(arrow, Vector2.Zero - arrow.GetSize() / 2 + RectSize / 2, tint);

        }

        public override Vector2 _GetMinimumSize() => new Vector2(0, 5);

        public void PivotAtLineStart()
        {
            RectPivotOffset = new Vector2(0, RectSize.y / 2f);
        }

        public void Join(Vector2 from, Vector2 to, Vector2 offset = default, IEnumerable<Rect2> clipRects = null)
        {
            // Offset along perpendicular direction
            var perpDir = from.DirectionTo(to).Rotated(Mathf.Deg2Rad(90)).Normalized();
            from -= perpDir * offset;
            to -= perpDir * offset;

            var dist = from.DistanceTo(to);
            var dir = from.DirectionTo(to);
            var center = from + dir * dist / 2;

            // Clip line with provided Rect2 array
            GDC.Array clipped = new GDC.Array() { new GDC.Array() { from, to } };
            if (clipRects != null)
                foreach (var clipRect in clipRects)
                {
                    if (clipped.Count == 0)
                        break;

                    GD.Print("clipped: ", clipped);

                    Vector2 lineFrom = clipped.ElementAt<Vector2>(0, 0);
                    Vector2 lineTo = clipped.ElementAt<Vector2>(0, 1);
                    clipped = Geometry.ClipPolylineWithPolygon2d(
                        new[] { lineFrom, lineTo },
                        new[]{
                            clipRect.Position,
                            new Vector2(clipRect.Position.x, clipRect.End.y),
                            clipRect.End,
                            new Vector2(clipRect.End.x, clipRect.Position.y)
                        }
                    );
                }
            if (clipped.Count > 0)
            {
                GD.Print("clipped count > 0 clipped: " + JSON.Print(clipped));
                from = clipped.ElementAt<Vector2>(0, 0);
                to = clipped.ElementAt<Vector2>(0, 1);
            }
            else
            {
                // Line is totally overlapped
                from = center;
                to = center + dir * 0.1f;
            }
            // Extends line by 2px to minimise ugly seam	
            from -= dir * 2f;
            to += dir * 2f;

            RectSize = new Vector2(to.DistanceTo(from), RectSize.y);
            // rectSize.y equals to the thickness of line
            RectPosition = new Vector2(from.x, from.y - RectSize.y / 2f);
            RectRotation = Mathf.Rad2Deg(Vector2.Right.AngleTo(dir));
            PivotAtLineStart();
        }

        public Vector2 GetFromPos() => GetTransform() * RectPosition;
        public Vector2 GetToPos() => GetTransform() * (RectPosition + RectSize);
    }
}