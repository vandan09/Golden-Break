using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Parses the subset of SVG path syntax the Claude Design ceramic artwork
/// uses — absolute M, L, C and Z — into a flat point list.
///
/// The ceramic shapes are stored in this project as the design's own `d`
/// strings, verbatim, and turned into geometry here rather than being
/// hand-transcribed into C# calls. Nine silhouettes and seventy-two crack
/// paths is far too much coordinate data to retype without introducing a
/// silent error, and keeping the original strings means a design revision
/// is a copy-paste rather than a re-derivation.
/// </summary>
public static class SvgPath
{
    // Enough segments that a curve reads as smooth at 4x raster scale.
    //
    // Kept deliberately low because the polygon fill tests every pixel
    // against every vertex, so this multiplies directly into rasterization
    // cost: at 40 the nine ceramics took long enough that opening the
    // gallery visibly stalled on first use. At 14 the curve is still
    // smooth once downsampled from 4x, and the fill is roughly three times
    // cheaper.
    private const int CubicSamples = 14;

    public static Vector2[] ToPoints(string d)
    {
        var points = new List<Vector2>();
        if (string.IsNullOrEmpty(d))
        {
            return points.ToArray();
        }

        var numbers = new List<float>();
        var token = new System.Text.StringBuilder();
        char command = ' ';
        Vector2 current = Vector2.zero;
        Vector2 start = Vector2.zero;

        // A command letter ends the previous command, so the numbers
        // gathered so far belong to it. Z has no operands and closes back
        // to the subpath start.
        for (int i = 0; i <= d.Length; i++)
        {
            char c = i < d.Length ? d[i] : 'Z';

            if (char.IsDigit(c) || c == '.' || c == '-' || c == '+' || c == 'e' || c == 'E')
            {
                // A minus that is not an exponent sign starts a new number.
                if (c == '-' && token.Length > 0 && token[token.Length - 1] != 'e' && token[token.Length - 1] != 'E')
                {
                    FlushNumber(token, numbers);
                }

                token.Append(c);
                continue;
            }

            FlushNumber(token, numbers);

            if (c == ',' || char.IsWhiteSpace(c))
            {
                continue;
            }

            Apply(command, numbers, points, ref current, ref start);
            numbers.Clear();
            command = c;

            if (c == 'Z' || c == 'z')
            {
                if (points.Count > 0)
                {
                    current = start;
                }
            }
        }

        Apply(command, numbers, points, ref current, ref start);
        return points.ToArray();
    }

    private static void FlushNumber(System.Text.StringBuilder token, List<float> numbers)
    {
        if (token.Length == 0)
        {
            return;
        }

        if (float.TryParse(token.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            numbers.Add(value);
        }

        token.Length = 0;
    }

    private static void Apply(char command, List<float> numbers, List<Vector2> points, ref Vector2 current, ref Vector2 start)
    {
        switch (command)
        {
            case 'M':
                for (int i = 0; i + 1 < numbers.Count; i += 2)
                {
                    current = new Vector2(numbers[i], numbers[i + 1]);
                    if (i == 0)
                    {
                        start = current;
                    }

                    points.Add(current);
                }

                break;

            case 'L':
                for (int i = 0; i + 1 < numbers.Count; i += 2)
                {
                    current = new Vector2(numbers[i], numbers[i + 1]);
                    points.Add(current);
                }

                break;

            case 'C':
                // Six operands per curve, and a C may carry several in a row.
                for (int i = 0; i + 5 < numbers.Count; i += 6)
                {
                    var c1 = new Vector2(numbers[i], numbers[i + 1]);
                    var c2 = new Vector2(numbers[i + 2], numbers[i + 3]);
                    var end = new Vector2(numbers[i + 4], numbers[i + 5]);
                    AppendCubic(points, current, c1, c2, end);
                    current = end;
                }

                break;
        }
    }

    private static void AppendCubic(List<Vector2> points, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        for (int i = 1; i <= CubicSamples; i++)
        {
            float t = i / (float)CubicSamples;
            float mt = 1f - t;
            Vector2 point = (mt * mt * mt * p0)
                            + (3f * mt * mt * t * p1)
                            + (3f * mt * t * t * p2)
                            + (t * t * t * p3);
            points.Add(point);
        }
    }
}
