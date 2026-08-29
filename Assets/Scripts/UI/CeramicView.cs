using DG.Tweening;
using UnityEngine;

/// <summary>
/// Renders the current ceramic above the grid (CLAUDE.md §3.4): silhouette
/// placeholder, crack lines (grey unrepaired, gold+glow repaired), a
/// progress bar, gold-flow fill animation, and the completion celebration.
///
/// No real ceramic art exists yet (Figma work is explicitly Phase 5/8
/// scope in CLAUDE.md §3.4) — the silhouette is a flat placeholder shape
/// and "celebration" is a pulse + text rather than the spec's literal
/// "slides into the gallery" animation. The underlying data (gallery
/// entry recorded, next ceramic loaded) is fully correct either way; only
/// the visual flourish is simplified, same pattern as every other
/// placeholder in this build.
/// </summary>
public sealed class CeramicView : MonoBehaviour
{
    private const int MaxCracks = 12;
    private static readonly System.Collections.Generic.List<Vector2> PartialPoints =
        new System.Collections.Generic.List<Vector2>();
    // The mockup strokes cracks at 3 SVG units when repaired and 2 when
    // unrepaired -- repaired cracks are deliberately heavier, which is
    // what makes gold read as filling the break. Both were previously a
    // single 0.05f, which is also ~4.2 SVG units, so every crack was about
    // 40%% too thick and carried no repaired/unrepaired distinction.
    private const float RepairedLineWidth = 3f * Constants.CeramicWorldScale;
    private const float UnrepairedLineWidth = 2f * Constants.CeramicWorldScale;

    // Approximates the mockup's drop-shadow(0 0 3px rgba(240,216,144,0.8))
    // on repaired cracks: a wider, translucent line drawn underneath.
    private const float GlowWidthMultiplier = 2.6f;
    private static readonly Color GoldGlowColour = new Color(240f / 255f, 216f / 255f, 144f / 255f, 0.38f);
    // 0.09 is the mockup value; it becomes the midpoint of the live range
    // rather than a constant.
    // Mockup: 220x180 at 9%% gold. Kept as a live range so the wash rises
    // with the repair instead of sitting at one static value.
    private const float AmbientGlowWidth = 220f;
    private const float AmbientGlowHeight = 180f;
    private const float AmbientGlowOffsetY = 0.1f;
    private const float AmbientGlowMinAlpha = 0.05f;
    private const float AmbientGlowMaxAlpha = 0.16f;

    private const float ProgressBarWidth = 1.6f;
    private const float ProgressBarHeight = 0.08f;

    private static readonly Color UnrepairedCrackColour = new Color(0.29f, 0.28f, 0.41f); // #4a4768

    private SpriteRenderer _silhouetteRenderer;
    private int _cracksRepaired;
    private LineRenderer[] _crackRenderers;
    private LineRenderer[] _crackGlowRenderers;
    private Transform _progressBarFill;
    private SpriteRenderer _ambientGlowRenderer;
    private CeramicDefinition _definition;
    private int _colourVariant;

    public Transform ProgressBarFill => _progressBarFill;

    public LineRenderer GetCrackRenderer(int index) => _crackRenderers[index];

    private void Awake()
    {
        Initialize();
    }

    // Separated from Awake() for the same reason as GridManager.BuildGrid
    // — AddComponent doesn't reliably invoke Awake() synchronously outside
    // Play Mode (see PROGRESS.md), so tests call this explicitly.
    public void Initialize()
    {
        if (_crackRenderers != null)
        {
            return;
        }

        var silhouetteObject = new GameObject("Silhouette");
        silhouetteObject.transform.SetParent(transform, false);
        silhouetteObject.transform.localPosition = new Vector3(0f, 0f, 0.1f);
        silhouetteObject.transform.localScale = Vector3.one;
        _silhouetteRenderer = silhouetteObject.AddComponent<SpriteRenderer>();

        _silhouetteRenderer.sprite = CeramicSilhouetteSprite.Get(CeramicShapeArchetype.SimpleBowl);

        BuildAmbientGlow();

        _crackRenderers = new LineRenderer[MaxCracks];
        _crackGlowRenderers = new LineRenderer[MaxCracks];
        for (int i = 0; i < MaxCracks; i++)
        {
            var crackObject = new GameObject($"Crack_{i}");
            crackObject.transform.SetParent(transform, false);

            // The crack line stays on the crack object itself so callers
            // (and CeramicViewTests) can keep toggling it via
            // GetCrackRenderer(i).gameObject; the halo is the child.
            _crackRenderers[i] = ConfigureCrackLine(crackObject.AddComponent<LineRenderer>(), UnrepairedLineWidth, sortingOrder: 1);

            _crackGlowRenderers[i] = CreateCrackLine(crackObject.transform, "Glow", UnrepairedLineWidth * GlowWidthMultiplier, sortingOrder: 0);
            _crackGlowRenderers[i].startColor = GoldGlowColour;
            _crackGlowRenderers[i].endColor = GoldGlowColour;
            _crackGlowRenderers[i].gameObject.SetActive(false);

            crackObject.SetActive(false);
        }

        BuildProgressBar();
    }

    // Round caps and corners match the mockup's strokeLinecap/strokeLinejoin
    // of "round"; LineRenderer defaults to 0 cap vertices, which left every
    // crack ending in a hard chisel edge.
    private static LineRenderer CreateCrackLine(Transform parent, string name, float width, int sortingOrder)
    {
        var lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);
        return ConfigureCrackLine(lineObject.AddComponent<LineRenderer>(), width, sortingOrder);
    }

    private static LineRenderer ConfigureCrackLine(LineRenderer line, float width, int sortingOrder)
    {
        line.useWorldSpace = false;
        line.startWidth = width;
        line.endWidth = width;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.positionCount = 0;
        line.sortingOrder = sortingOrder;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.alignment = LineAlignment.TransformZ;

        return line;
    }

    // The mockup places a soft radial wash behind the ceramic
    // (220x180 at 9%% gold). Sized off the same viewBox the silhouette uses.
    private void BuildAmbientGlow()
    {
        var glowObject = new GameObject("AmbientGlow");
        glowObject.transform.SetParent(transform, false);
        glowObject.transform.localPosition = new Vector3(0f, AmbientGlowOffsetY, 0.3f);

        _ambientGlowRenderer = glowObject.AddComponent<SpriteRenderer>();
        _ambientGlowRenderer.sprite = RadialGlowSprite.Get();
        _ambientGlowRenderer.sortingOrder = -1;

        // Every mockup screen draws this as a plain circular wash behind the
        // bowl -- radial-gradient(circle, rgba(232,192,96,0.09), transparent
        // 70%) at 220x180 -- not as a halo tracing the silhouette. A
        // shape-following glow was the wrong target.
        //
        // Scale is derived from the sprite's own world size. Assigning the
        // target size straight to localScale (the original bug) overshot by
        // the sprite's 1.28-unit size, making the wash ~30%% too large.
        Vector2 spriteWorld = _ambientGlowRenderer.sprite.bounds.size;
        glowObject.transform.localScale = new Vector3(
            (AmbientGlowWidth * Constants.CeramicWorldScale) / spriteWorld.x,
            (AmbientGlowHeight * Constants.CeramicWorldScale) / spriteWorld.y,
            1f);

        SetAmbientGlowStrength(0f);
    }

    // The mockup fixes the ambient wash at 9%% gold, but that is a single
    // static frame. Across a repair run the glow rises with the gold: an
    // untouched ceramic sits nearly dark and a finished one is fully lit,
    // so the screen visibly warms as the player fills cracks.
    private void SetAmbientGlowStrength(float fraction)
    {
        if (_ambientGlowRenderer == null)
        {
            return;
        }

        float alpha = Mathf.Lerp(AmbientGlowMinAlpha, AmbientGlowMaxAlpha, Mathf.Clamp01(fraction));
        Color gold = UiPalette.GoldFill;
        _ambientGlowRenderer.color = new Color(gold.r, gold.g, gold.b, alpha);
    }

    private void BuildProgressBar()
    {
        var trackObject = new GameObject("ProgressBarTrack");
        trackObject.transform.SetParent(transform, false);
        trackObject.transform.localPosition = new Vector3(0f, -1.4f, 0f);
        trackObject.transform.localScale = new Vector3(ProgressBarWidth, ProgressBarHeight, 1f);
        var trackRenderer = trackObject.AddComponent<SpriteRenderer>();
        trackRenderer.sprite = PlaceholderSprite.GetSolid(UiPalette.EmptyCellFill);

        // Positioned at the track's left edge (localPosition x=-0.5 in the
        // track's own unit-square space) so scaling on X grows the fill
        // rightward from that edge, not from the track's centre.
        var fillObject = new GameObject("ProgressBarFill");
        fillObject.transform.SetParent(trackObject.transform, false);
        fillObject.transform.localPosition = new Vector3(-0.5f, 0f, -0.05f);
        fillObject.transform.localScale = new Vector3(0f, 1f, 1f);
        var fillRenderer = fillObject.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = PlaceholderSprite.GetSolid(UiPalette.GoldFill);

        _progressBarFill = fillObject.transform;
    }

    public void SetCeramic(CeramicDefinition definition, int cracksRepaired, int colourVariant)
    {
        Initialize();
        _definition = definition;
        _colourVariant = colourVariant;

        _cracksRepaired = cracksRepaired;
        RefreshComposite();

        // Every crack is already painted into the composite sprite, so the
        // LineRenderers stay off until one actually animates. They exist
        // only to draw the gold travelling along a crack during a fill —
        // the resting state is the rasterized sprite, identical to the one
        // the Home card and Gallery use, so all three match the design.
        for (int i = 0; i < MaxCracks; i++)
        {
            _crackRenderers[i].gameObject.SetActive(false);
            _crackGlowRenderers[i].gameObject.SetActive(false);
        }

        UpdateProgressBar(cracksRepaired, definition != null ? definition.totalCracks : 0);
    }

    public void AnimateCrackFill(int crackIndex, System.Action onComplete)
    {
        SetCrackColour(crackIndex, repaired: true);
        _crackRenderers[crackIndex].gameObject.SetActive(true);

        var tipDot = CreateGoldTipDot();
        tipDot.transform.SetParent(transform, false);

        float progress = 0f;
        DOTween.To(() => progress, p =>
            {
                progress = p;
                DrawCrackPartial(crackIndex, progress);
                UpdateTipDot(tipDot, crackIndex, progress);
            }, 1f, Constants.GoldFlowAnimationSeconds)
            .OnComplete(() =>
            {
                Object.Destroy(tipDot.gameObject);

                // Hand this crack back to the composite now that it is
                // fully gold, and drop the line that was drawing it. A
                // combo animates several cracks at once, so take the
                // highest finished index rather than assuming this one is
                // the newest — they complete independently.
                _cracksRepaired = Mathf.Max(_cracksRepaired, crackIndex + 1);
                RefreshComposite();
                _crackRenderers[crackIndex].gameObject.SetActive(false);
                _crackGlowRenderers[crackIndex].gameObject.SetActive(false);

                SpawnCeramicParticles();
                onComplete?.Invoke();
            });
    }

    private void RefreshComposite()
    {
        if (_definition == null)
        {
            return;
        }

        _silhouetteRenderer.sprite = CeramicSilhouetteSprite.GetComposite(
            _definition.shape, _definition.cracks, _cracksRepaired, _colourVariant);
    }

    private SpriteRenderer CreateGoldTipDot()
    {
        var obj = new GameObject("GoldTipDot");
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = PlaceholderSprite.GetSolid(Color.white);
        sr.color = new Color(0.94f, 0.85f, 0.56f, 1f);
        sr.sortingOrder = 2;
        obj.transform.localScale = Vector3.one * 0.1f;

        DOTween.Sequence()
            .Append(obj.transform.DOScale(Vector3.one * 0.16f, 0.35f).SetEase(Ease.InOutSine))
            .Append(obj.transform.DOScale(Vector3.one * 0.1f, 0.35f).SetEase(Ease.InOutSine))
            .SetLoops(-1);

        return sr;
    }

    private void UpdateTipDot(SpriteRenderer tipDot, int crackIndex, float t)
    {
        if (_definition == null || crackIndex >= _definition.cracks.Length) return;
        Vector2 point = PolylineUtility.Evaluate(_definition.cracks[crackIndex].points, t) * Constants.CeramicWorldScale;
        tipDot.transform.localPosition = new Vector3(point.x, point.y, -0.1f);
    }

    private void SpawnCeramicParticles()
    {
        for (int i = 0; i < 3; i++)
        {
            var obj = new GameObject("CeramicParticle");
            obj.transform.SetParent(transform, false);
            float xOffset = (i - 1) * 0.3f + Random.Range(-0.05f, 0.05f);
            obj.transform.localPosition = new Vector3(xOffset, -1.0f, -0.1f);
            obj.transform.localScale = Vector3.one * 0.06f;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSprite.GetSolid(Color.white);
            sr.color = new Color(0.94f, 0.85f, 0.56f, 0.9f);
            sr.sortingOrder = 3;

            float delay = i * 0.15f;
            float riseHeight = 0.8f + Random.Range(0f, 0.4f);

            DOTween.Sequence()
                .AppendInterval(delay)
                .Append(obj.transform.DOLocalMoveY(-1.0f + riseHeight, 0.9f).SetEase(Ease.OutQuad))
                .Join(obj.transform.DOScale(Vector3.one * 0.02f, 0.9f))
                .Join(DOTween.ToAlpha(() => sr.color, c => sr.color = c, 0f, 0.9f).SetDelay(delay))
                .OnComplete(() => Object.Destroy(obj));
        }
    }

    public void UpdateProgressBar(int cracksRepaired, int totalCracks)
    {
        float fraction = totalCracks > 0 ? (float)cracksRepaired / totalCracks : 0f;
        fraction = Mathf.Clamp01(fraction);

        SetAmbientGlowStrength(fraction);

        // The fill sprite has a centre pivot, so scaling it alone grows it
        // symmetrically in both directions -- it needs repositioning every
        // time too, or it bleeds past the track's left edge instead of
        // growing rightward from a pinned left edge. At fraction=0 this
        // reduces to the same -0.5 position BuildProgressBar starts with.
        _progressBarFill.localScale = new Vector3(fraction, 1f, 1f);
        _progressBarFill.localPosition = new Vector3(-0.5f + (fraction * 0.5f), 0f, -0.05f);
    }

    public void PlayCompletionCelebration(System.Action onComplete)
    {
        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(Constants.CeramicCompletionPauseSeconds);
        sequence.Append(transform.DOPunchScale(Vector3.one * 0.15f, Constants.CeramicCelebrationDurationSeconds, vibrato: 4, elasticity: 0.6f));
        sequence.OnComplete(() => onComplete?.Invoke());
    }

    // Emits the crack's real vertices up to t rather than a fixed number of
    // evenly spaced samples: the authored paths turn a corner, and sampling
    // would round that corner off wherever it fell between samples.
    private void DrawCrackPartial(int crackIndex, float t)
    {
        CrackPath path = _definition.cracks[crackIndex];
        PolylineUtility.BuildPartial(path.points, t, PartialPoints);

        LineRenderer line = _crackRenderers[crackIndex];
        LineRenderer glow = _crackGlowRenderers[crackIndex];
        line.positionCount = PartialPoints.Count;
        glow.positionCount = PartialPoints.Count;

        for (int s = 0; s < PartialPoints.Count; s++)
        {
            Vector2 point = PartialPoints[s] * Constants.CeramicWorldScale;
            var position = new Vector3(point.x, point.y, 0f);
            line.SetPosition(s, position);
            glow.SetPosition(s, position);
        }
    }

    private void SetCrackColour(int crackIndex, bool repaired)
    {
        Color colour = repaired ? GoldColourForVariant(_colourVariant) : UnrepairedCrackColour;
        LineRenderer line = _crackRenderers[crackIndex];
        line.startColor = colour;
        line.endColor = colour;

        float width = repaired ? RepairedLineWidth : UnrepairedLineWidth;
        line.startWidth = width;
        line.endWidth = width;

        // Only repaired cracks glow -- an unrepaired crack is a dark seam,
        // so a halo on it would read as light coming from a hole.
        LineRenderer glow = _crackGlowRenderers[crackIndex];
        glow.gameObject.SetActive(repaired);
        if (repaired)
        {
            glow.startWidth = width * GlowWidthMultiplier;
            glow.endWidth = width * GlowWidthMultiplier;

            Color glowColour = new Color(colour.r, colour.g, colour.b, GoldGlowColour.a);
            glow.startColor = glowColour;
            glow.endColor = glowColour;
        }
    }

    // CLAUDE.md §3.4: "10+: repeat 6-9 with colour variants" — no specific
    // variant hues are given, so each variant gets a deterministic hue
    // shift off the base gold rather than reusing an identical colour,
    // which would make variants visually indistinguishable.
    private static Color GoldColourForVariant(int colourVariant)
    {
        return CeramicGold.ForVariant(colourVariant);
    }
}
