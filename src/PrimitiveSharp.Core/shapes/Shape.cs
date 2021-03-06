﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace PrimitiveSharp.Core
{
    public enum ShapeType
    {
        Any = 0,
        Triangle,
        Rectangle,
        RotatedRectangle,
        Ellipse,
        RotatedEllipse,
        Circle,
        BezierQuadratic,
        Quadrilateral,
        Square,
        Pentagon,
        Hexagon,
        Octagon,
        FourPointedStar,
        Pentagram,
        Hexagram,
        Crescent
    }

    public interface IShape
    {
        WorkerModel Worker { get; set; }
        IShape Copy();
        IPath GetPath();
        void Draw(Image<Rgba32> image, Rgba32 color, double scale);
        void Mutate();
        string SVG(string attrs);
        List<ScanlineModel> GetScanlines();
    }

    public abstract class Shape : IShape
    {
        private static readonly Comparer<PointF> comparer = Comparer<PointF>.Create((a, b) => a.X.CompareTo(b.X));
        private List<ScanlineModel> scanlines;
        public WorkerModel Worker { get; set; }
        public abstract IShape Copy();
        public abstract IPath GetPath();
        public abstract string SVG(string attrs);
        protected abstract void MutateImpl();

        public void Mutate()
        {
            MutateImpl();
            scanlines?.Clear();
        }

        public virtual void Draw(Image<Rgba32> image, Rgba32 color, double scale)
        {
            image.Mutate(im => im
                .Fill(color, GetPath().Transform(Matrix3x2.CreateScale((float)scale))));
        }

        public List<ScanlineModel> GetScanlines()
        {
            if (scanlines == null || scanlines.Count == 0)
                scanlines = Rasterize();
            return scanlines;
        }

        protected virtual List<ScanlineModel> Rasterize()
        {
            List<ScanlineModel> lines = new List<ScanlineModel>();

            var w = Worker.W;
            var h = Worker.H;
            var path = GetPath();
            PointF[] interscertions = new PointF[path.MaxIntersections];

            var bounds = path.Bounds;
            var bot = (int)bounds.Bottom.Clamp(0, h - 1);
            var top = (int)bounds.Top.Clamp(0, h - 1);

            for (int y = bot; y >= top; y--)
            {
                var n = path.FindIntersections(new PointF(bounds.Left, y), new PointF(bounds.Right, y), interscertions, 0);
                Array.Sort<PointF>(interscertions, 0, n, comparer);
                if (n % 2 == 0 && n > 1)
                {
                    for (int i = 0; i < n; i += 2)
                    {
                        lines.Add(new ScanlineModel
                        {
                            Alpha = 0xffff,
                            X1 = (int)interscertions[i].X.Clamp(0, w - 1),
                            X2 = (int)interscertions[i + 1].X.Clamp(0, w - 1),
                            Y = y
                        });
                    }
                }
                else if (n == 1)
                {
                    var x = (int)interscertions[0].X.Clamp(0, w - 1);
                    lines.Add(new ScanlineModel
                    {
                        Alpha = 0xffff,
                        X1 = x,
                        X2 = x,
                        Y = y
                    });
                }
            }
            return lines;
        }
    }
}
