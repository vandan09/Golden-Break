// GENERATED from the Claude Design project artboard "Ceramic Library - 9 tiers".
// Every string below is that file's own SVG, copied verbatim - do not hand-edit.
using System.Collections.Generic;
using UnityEngine;

public static class CeramicShapeData
{
    public struct Ellipse
    {
        public float cx, cy, rx, ry;
        public Ellipse(float cx, float cy, float rx, float ry) { this.cx = cx; this.cy = cy; this.rx = rx; this.ry = ry; }
    }

    public struct Shape
    {
        public Vector2 viewBox;
        public Ellipse rim;
        public Ellipse opening;
        public string body;
        public string[] cracks;
    }

    public static readonly Dictionary<CeramicShapeArchetype, Shape> Shapes =
        new Dictionary<CeramicShapeArchetype, Shape>
    {
        { CeramicShapeArchetype.SimpleBowl, new Shape
        {
            viewBox = new Vector2(200f, 150f),
            rim = new Ellipse(100f, 35f, 82f, 16f),
            opening = new Ellipse(100f, 33f, 72f, 12f),
            body = "M18,35 C18,35 22,120 100,132 C178,120 182,35 182,35 L172,38 C168,95 140,118 100,120 C60,118 32,95 28,38 Z",
            cracks = new[]
            {
                "M100,48 L84,72 L68,102",
                "M100,48 L118,70 L134,98",
                "M100,48 L100,82 L96,116",
                "M140,52 L130,76 L138,100",
            },
        } },
        { CeramicShapeArchetype.TeaCup, new Shape
        {
            viewBox = new Vector2(160f, 150f),
            rim = new Ellipse(80f, 28f, 52f, 12f),
            opening = new Ellipse(80f, 26f, 44f, 9f),
            body = "M28,28 C30,80 34,116 46,126 C64,136 96,136 114,126 C126,116 130,80 132,28 L120,31 C118,78 114,108 106,116 C92,124 68,124 54,116 C46,108 42,78 40,31 Z",
            cracks = new[]
            {
                "M80,42 L66,66 L58,98",
                "M80,42 L94,70 L102,102",
                "M80,42 L84,78 L76,112",
                "M50,44 L60,72 L52,100",
                "M112,46 L104,74 L112,104",
            },
        } },
        { CeramicShapeArchetype.Plate, new Shape
        {
            viewBox = new Vector2(200f, 100f),
            rim = new Ellipse(100f, 30f, 90f, 12f),
            opening = new Ellipse(100f, 28f, 78f, 9f),
            body = "M14,30 C14,30 20,70 100,78 C180,70 186,30 186,30 L176,33 C170,58 140,64 100,66 C60,64 30,58 24,33 Z",
            cracks = new[]
            {
                "M100,40 L86,54 L76,66",
                "M100,40 L114,52 L124,64",
                "M100,40 L100,58 L96,72",
                "M60,38 L52,50 L46,58",
                "M142,38 L150,50 L154,58",
                "M78,38 L68,54 L62,66",
            },
        } },
        { CeramicShapeArchetype.TallVase, new Shape
        {
            viewBox = new Vector2(140f, 200f),
            rim = new Ellipse(70f, 24f, 40f, 10f),
            opening = new Ellipse(70f, 22f, 32f, 7f),
            body = "M40,24 C30,90 30,150 70,180 C110,150 110,90 100,24 L92,26 C96,80 92,140 70,164 C48,140 44,80 48,26 Z",
            cracks = new[]
            {
                "M70,36 L58,84 L52,132",
                "M70,36 L80,88 L74,146",
                "M56,32 L48,70",
                "M84,32 L94,78 L88,120",
                "M46,34 L42,86 L50,128",
                "M98,30 L100,60",
                "M74,44 L66,104 L72,158",
            },
        } },
        { CeramicShapeArchetype.Teapot, new Shape
        {
            viewBox = new Vector2(200f, 170f),
            rim = new Ellipse(100f, 34f, 40f, 11f),
            opening = new Ellipse(100f, 32f, 32f, 8f),
            body = "M60,34 C20,52 12,110 46,140 C70,158 130,158 154,140 C188,110 180,52 140,34 L136,44 C168,60 172,106 144,130 C124,146 76,146 56,130 C28,106 32,60 64,44 Z",
            cracks = new[]
            {
                "M100,44 L86,80 L74,126",
                "M100,44 L114,78 L128,124",
                "M100,44 L100,88 L96,138",
                "M70,42 L52,76 L48,118",
                "M130,42 L150,74 L152,116",
                "M84,42 L70,84 L62,132",
                "M116,42 L132,82 L140,130",
                "M58,40 L38,72 L34,106",
            },
        } },
        { CeramicShapeArchetype.LargeBowl, new Shape
        {
            viewBox = new Vector2(220f, 160f),
            rim = new Ellipse(110f, 36f, 98f, 17f),
            opening = new Ellipse(110f, 34f, 86f, 13f),
            body = "M14,36 L46,132 C52,148 168,148 174,132 L206,36 L192,41 L164,124 C156,136 64,136 56,124 L28,41 Z",
            cracks = new[]
            {
                "M110,48 L92,84 L78,128",
                "M110,48 L128,82 L142,126",
                "M110,48 L110,92 L104,138",
                "M74,46 L62,86 L58,124",
                "M146,46 L158,84 L162,122",
                "M92,46 L76,90 L68,132",
                "M128,46 L144,88 L152,130",
                "M46,44 L34,74 L40,102",
                "M174,44 L186,74 L180,102",
            },
        } },
        { CeramicShapeArchetype.OrnatePlate, new Shape
        {
            viewBox = new Vector2(240f, 90f),
            rim = new Ellipse(120f, 32f, 116f, 15f),
            opening = new Ellipse(120f, 30f, 96f, 11f),
            body = "M4,32 C4,32 10,66 120,74 C230,66 236,32 236,32 L214,37 C206,54 176,62 120,64 C64,62 34,54 26,37 Z",
            cracks = new[]
            {
                "M120,44 L108,55 L98,64",
                "M120,44 L132,55 L142,64",
                "M120,44 L121,58 L116,68",
                "M96,42 L86,53 L76,60",
                "M144,42 L154,53 L164,60",
                "M74,40 L64,49 L58,56",
                "M166,40 L176,49 L182,56",
                "M108,43 L99,54 L92,62",
                "M132,43 L141,54 L149,62",
                "M42,36 L34,43 L30,49",
            },
        } },
        { CeramicShapeArchetype.SakeSet, new Shape
        {
            viewBox = new Vector2(150f, 190f),
            rim = new Ellipse(75f, 22f, 44f, 11f),
            opening = new Ellipse(75f, 20f, 35f, 8f),
            body = "M34,22 C38,36 40,42 32,54 C14,74 12,140 42,166 C58,180 92,180 108,166 C138,140 136,74 118,54 C110,42 112,36 116,22 L106,26 C103,38 102,44 110,60 C132,84 128,136 102,158 C88,170 62,170 48,158 C22,136 18,84 40,60 C48,44 47,38 44,26 Z",
            cracks = new[]
            {
                "M75,34 L66,72 L54,116",
                "M75,34 L86,76 L98,120",
                "M62,32 L50,64 L40,100",
                "M88,32 L102,66 L112,102",
                "M52,30 L38,56",
                "M98,30 L112,58",
                "M70,38 L72,92 L64,146",
                "M80,38 L82,100 L90,148",
                "M44,28 L30,64 L28,96",
                "M106,28 L120,64 L122,96",
                "M75,44 L74,104 L78,158",
            },
        } },
        { CeramicShapeArchetype.TempleBowl, new Shape
        {
            viewBox = new Vector2(220f, 190f),
            rim = new Ellipse(110f, 40f, 92f, 17f),
            opening = new Ellipse(110f, 38f, 80f, 13f),
            body = "M18,40 C22,110 50,136 96,140 L96,152 L70,168 L66,178 L154,178 L150,168 L124,152 L124,140 C170,136 198,110 202,40 L186,46 C180,104 152,124 110,126 C68,124 40,104 34,46 Z",
            cracks = new[]
            {
                "M110,54 L108,98 L112,132",
                "M110,54 L94,90 L84,122",
                "M110,54 L126,88 L138,120",
                "M88,52 L72,86 L68,120",
                "M136,52 L150,86 L152,114",
                "M66,50 L52,80 L50,108",
                "M154,50 L168,80 L170,106",
                "M46,46 L34,72",
                "M174,46 L188,74",
                "M99,52 L88,94 L74,124",
                "M122,52 L132,94 L146,124",
                "M80,50 L68,74 L58,96",
            },
        } },
    };
}
