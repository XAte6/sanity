using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text;

namespace Sanity
{
    internal static class SvgPath
    {
        public static GraphicsPath ToGraphicsPath(string pathData, float viewBoxSize, float targetSize)
        {
            var scale = targetSize / viewBoxSize;
            var path = new GraphicsPath();
            var tokens = Tokenize(pathData);
            var i = 0;
            float cx = 0, cy = 0;
            float startX = 0, startY = 0;
            float lastCtrlX = 0, lastCtrlY = 0;
            char command = 'M';

            while (i < tokens.Count)
            {
                if (char.IsLetter(tokens[i][0]) && tokens[i].Length == 1)
                {
                    command = tokens[i][0];
                    i++;
                }

                var relative = char.IsLower(command);
                var cmd = char.ToUpperInvariant(command);

                switch (cmd)
                {
                    case 'M':
                    {
                        var x = Read(tokens, ref i);
                        var y = Read(tokens, ref i);
                        if (relative) { x += cx; y += cy; }
                        cx = x; cy = y;
                        startX = cx; startY = cy;
                        path.StartFigure();
                        command = relative ? 'l' : 'L';
                        break;
                    }
                    case 'L':
                    {
                        var x = Read(tokens, ref i);
                        var y = Read(tokens, ref i);
                        if (relative) { x += cx; y += cy; }
                        path.AddLine(cx * scale, cy * scale, x * scale, y * scale);
                        cx = x; cy = y;
                        break;
                    }
                    case 'H':
                    {
                        var x = Read(tokens, ref i);
                        if (relative) x += cx;
                        path.AddLine(cx * scale, cy * scale, x * scale, cy * scale);
                        cx = x;
                        break;
                    }
                    case 'V':
                    {
                        var y = Read(tokens, ref i);
                        if (relative) y += cy;
                        path.AddLine(cx * scale, cy * scale, cx * scale, y * scale);
                        cy = y;
                        break;
                    }
                    case 'C':
                    {
                        var x1 = Read(tokens, ref i);
                        var y1 = Read(tokens, ref i);
                        var x2 = Read(tokens, ref i);
                        var y2 = Read(tokens, ref i);
                        var x = Read(tokens, ref i);
                        var y = Read(tokens, ref i);
                        if (relative)
                        {
                            x1 += cx; y1 += cy;
                            x2 += cx; y2 += cy;
                            x += cx; y += cy;
                        }
                        path.AddBezier(
                            cx * scale, cy * scale,
                            x1 * scale, y1 * scale,
                            x2 * scale, y2 * scale,
                            x * scale, y * scale);
                        lastCtrlX = x2; lastCtrlY = y2;
                        cx = x; cy = y;
                        break;
                    }
                    case 'S':
                    {
                        var x2 = Read(tokens, ref i);
                        var y2 = Read(tokens, ref i);
                        var x = Read(tokens, ref i);
                        var y = Read(tokens, ref i);
                        var x1 = cx * 2 - lastCtrlX;
                        var y1 = cy * 2 - lastCtrlY;
                        if (relative)
                        {
                            x2 += cx; y2 += cy;
                            x += cx; y += cy;
                        }
                        path.AddBezier(
                            cx * scale, cy * scale,
                            x1 * scale, y1 * scale,
                            x2 * scale, y2 * scale,
                            x * scale, y * scale);
                        lastCtrlX = x2; lastCtrlY = y2;
                        cx = x; cy = y;
                        break;
                    }
                    case 'Z':
                        path.CloseFigure();
                        cx = startX;
                        cy = startY;
                        i++;
                        break;
                    case 'A':
                    {
                        // Approximate arcs as lines to the end point (PayPal fallback).
                        Read(tokens, ref i); Read(tokens, ref i); Read(tokens, ref i);
                        Read(tokens, ref i); Read(tokens, ref i);
                        var x = Read(tokens, ref i);
                        var y = Read(tokens, ref i);
                        if (relative) { x += cx; y += cy; }
                        path.AddLine(cx * scale, cy * scale, x * scale, y * scale);
                        cx = x; cy = y;
                        break;
                    }
                    default:
                        i++;
                        break;
                }
            }

            return path;
        }

        public static Bitmap Render(string pathData, int size, Color fill, float viewBox = 24f)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            using (var graphicsPath = ToGraphicsPath(pathData, viewBox, size))
            using (var brush = new SolidBrush(fill))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                g.FillPath(brush, graphicsPath);
            }
            return bmp;
        }

        private static List<string> Tokenize(string data)
        {
            var tokens = new List<string>();
            var current = new StringBuilder();
            for (var i = 0; i < data.Length; i++)
            {
                var c = data[i];
                if (char.IsLetter(c) && c != 'e' && c != 'E')
                {
                    Flush(tokens, current);
                    tokens.Add(c.ToString());
                }
                else if (c == ',' || char.IsWhiteSpace(c))
                {
                    Flush(tokens, current);
                }
                else if (c == '-' && current.Length > 0 && current[current.Length - 1] != 'e' && current[current.Length - 1] != 'E')
                {
                    Flush(tokens, current);
                    current.Append(c);
                }
                else
                {
                    current.Append(c);
                }
            }
            Flush(tokens, current);
            return tokens;
        }

        private static void Flush(List<string> tokens, StringBuilder current)
        {
            if (current.Length == 0)
                return;
            tokens.Add(current.ToString());
            current.Clear();
        }

        private static float Read(List<string> tokens, ref int i)
        {
            while (i < tokens.Count && (tokens[i].Length == 1 && char.IsLetter(tokens[i][0])))
                i++;
            if (i >= tokens.Count)
                return 0;
            float value;
            float.TryParse(tokens[i], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            i++;
            return value;
        }
    }
}
