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
    private const int BezierSamples = 20;
    private const float LineWidth = 0.05f;
    private const float ProgressBarWidth = 1.6f;
    private const float ProgressBarHeight = 0.08f;

    private static readonly Color UnrepairedCrackColour = new Color(0.29f, 0.28f, 0.41f); // #4a4768

    private SpriteRenderer _silhouetteRenderer;
    private LineRenderer[] _crackRenderers;
    private Transform _progressBarFill;
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

        // Real shape until SetCeramic() assigns the actual tier's archetype
        // — CeramicSilhouetteSprite.Get is cached, so this isn't wasted
        // work, just a sane default before the first real ceramic loads.
        _silhouetteRenderer.sprite = CeramicSilhouetteSprite.Get(CeramicShapeArchetype.Bowl);

        _crackRenderers = new LineRenderer[MaxCracks];
        for (int i = 0; i < MaxCracks; i++)
        {
            var crackObject = new GameObject($"Crack_{i}");
            crackObject.transform.SetParent(transform, false);

            var line = crackObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.startWidth = LineWidth;
            line.endWidth = LineWidth;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.positionCount = 0;
            line.sortingOrder = 1;

            _crackRenderers[i] = line;
            crackObject.SetActive(false);
        }

        BuildProgressBar();
    }

    private void BuildProgressBar()
    {
        var trackObject = new GameObject("ProgressBarTrack");
        trackObject.transform.SetParent(transform, false);
        trackObject.transform.localPosition = new Vector3(0f, -1.3f, 0f);
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

        if (definition != null)
        {
            _silhouetteRenderer.sprite = CeramicSilhouetteSprite.Get(definition.shape);
        }

        for (int i = 0; i < MaxCracks; i++)
        {
            bool isActive = definition != null && definition.cracks != null && i < definition.cracks.Length;
            _crackRenderers[i].gameObject.SetActive(isActive);
            if (!isActive)
            {
                continue;
            }

            bool repaired = i < cracksRepaired;
            DrawCrackPartial(i, 1f);
            SetCrackColour(i, repaired);
        }

        UpdateProgressBar(cracksRepaired, definition != null ? definition.totalCracks : 0);
    }

    public void AnimateCrackFill(int crackIndex, System.Action onComplete)
    {
        SetCrackColour(crackIndex, repaired: true);
        _crackRenderers[crackIndex].gameObject.SetActive(true);

        float progress = 0f;
        DOTween.To(() => progress, p =>
            {
                progress = p;
                DrawCrackPartial(crackIndex, progress);
            }, 1f, Constants.GoldFlowAnimationSeconds)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void UpdateProgressBar(int cracksRepaired, int totalCracks)
    {
        float fraction = totalCracks > 0 ? (float)cracksRepaired / totalCracks : 0f;
        fraction = Mathf.Clamp01(fraction);

        // The fill sprite has a centre pivot, so scaling it alone grows it
        // symmetrically in both directions — it needs repositioning every
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

    private void DrawCrackPartial(int crackIndex, float t)
    {
        CrackPath path = _definition.cracks[crackIndex];
        int sampleCount = Mathf.Max(2, Mathf.RoundToInt(BezierSamples * Mathf.Max(t, 0.01f)));
        LineRenderer line = _crackRenderers[crackIndex];
        line.positionCount = sampleCount;

        for (int s = 0; s < sampleCount; s++)
        {
            float sampleT = (sampleCount <= 1) ? 0f : (s / (float)(sampleCount - 1)) * t;
            Vector2 point = BezierUtility.Evaluate(path.controlPoints, sampleT) * Constants.CeramicWorldScale;
            line.SetPosition(s, new Vector3(point.x, point.y, 0f));
        }
    }

    private void SetCrackColour(int crackIndex, bool repaired)
    {
        Color colour = repaired ? GoldColourForVariant(_colourVariant) : UnrepairedCrackColour;
        _crackRenderers[crackIndex].startColor = colour;
        _crackRenderers[crackIndex].endColor = colour;
    }

    // CLAUDE.md §3.4: "10+: repeat 6-9 with colour variants" — no specific
    // variant hues are given, so each variant gets a deterministic hue
    // shift off the base gold rather than reusing an identical colour,
    // which would make variants visually indistinguishable.
    private static Color GoldColourForVariant(int colourVariant)
    {
        if (colourVariant <= 0)
        {
            return UiPalette.GoldFill;
        }

        Color.RGBToHSV(UiPalette.GoldFill, out float h, out float s, out float v);
        float hueShift = (colourVariant * 0.08f) % 1f;
        return Color.HSVToRGB((h + hueShift) % 1f, s, v);
    }
}
