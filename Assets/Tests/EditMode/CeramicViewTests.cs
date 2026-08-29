using NUnit.Framework;
using UnityEngine;

public class CeramicViewTests
{
    private CeramicView _view;
    private CeramicDefinition _fourCrackDefinition;

    [SetUp]
    public void CreateView()
    {
        var go = new GameObject("CeramicViewTestInstance");
        _view = go.AddComponent<CeramicView>();
        _view.Initialize();

        _fourCrackDefinition = ScriptableObject.CreateInstance<CeramicDefinition>();
        _fourCrackDefinition.tier = 1;
        _fourCrackDefinition.displayName = "Test bowl";
        _fourCrackDefinition.totalCracks = 4;
        _fourCrackDefinition.cracks = new CrackPath[4];
        for (int i = 0; i < 4; i++)
        {
            _fourCrackDefinition.cracks[i] = new CrackPath
            {
                points = new[]
                {
                    new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(2f, 1f), new Vector2(3f, 0f)
                }
            };
        }
    }

    [TearDown]
    public void DestroyView()
    {
        if (_view != null)
        {
            Object.DestroyImmediate(_view.gameObject);
        }
    }

    [Test]
    // The resting ceramic is one rasterized sprite carrying every crack, so
    // the LineRenderers are idle until a crack actually animates. They used
    // to hold the resting state, which is why this asserted the opposite.
    public void SetCeramic_LeavesEveryCrackRendererInactive()
    {
        _view.SetCeramic(_fourCrackDefinition, cracksRepaired: 0, colourVariant: 0);

        for (int i = 0; i < 12; i++)
        {
            Assert.IsFalse(
                _view.GetCrackRenderer(i).gameObject.activeSelf,
                $"crack {i} should be idle — the composite sprite draws the resting state");
        }
    }

    [Test]
    public void SetCeramic_RepairProgress_ChangesTheRenderedSprite()
    {
        _view.SetCeramic(_fourCrackDefinition, cracksRepaired: 0, colourVariant: 0);
        Sprite noneRepaired = CurrentSilhouette();

        _view.SetCeramic(_fourCrackDefinition, cracksRepaired: 2, colourVariant: 0);
        Sprite twoRepaired = CurrentSilhouette();

        Assert.AreNotSame(noneRepaired, twoRepaired, "repairing cracks must repaint the ceramic");
    }

    [Test]
    public void SetCeramic_DifferentColourVariants_ProduceDifferentRepairedColour()
    {
        _view.SetCeramic(_fourCrackDefinition, cracksRepaired: 1, colourVariant: 0);
        Sprite baseVariant = CurrentSilhouette();

        _view.SetCeramic(_fourCrackDefinition, cracksRepaired: 1, colourVariant: 2);
        Sprite secondVariant = CurrentSilhouette();

        // §3.4's tier 10+ colour variants have to reach the resting ceramic,
        // not just a crack mid-animation.
        Assert.AreNotSame(baseVariant, secondVariant, "a colour variant must repaint the gold");
        Assert.AreNotEqual(
            CeramicGold.ForVariant(0),
            CeramicGold.ForVariant(2),
            "variant 2 must differ in hue from the base gold");
    }

    [Test]
    public void AnimateCrackFill_ActivatesOnlyThatCracksRenderer()
    {
        _view.SetCeramic(_fourCrackDefinition, cracksRepaired: 0, colourVariant: 0);

        _view.AnimateCrackFill(1, null);

        Assert.IsTrue(_view.GetCrackRenderer(1).gameObject.activeSelf, "the filling crack draws with a line");
        Assert.IsFalse(_view.GetCrackRenderer(0).gameObject.activeSelf);
        Assert.IsFalse(_view.GetCrackRenderer(2).gameObject.activeSelf);
    }

    private Sprite CurrentSilhouette()
    {
        return _view.transform.Find("Silhouette").GetComponent<SpriteRenderer>().sprite;
    }

    [Test]
    public void SetCeramic_NullDefinition_DeactivatesAllCrackRenderers()
    {
        _view.SetCeramic(_fourCrackDefinition, cracksRepaired: 2, colourVariant: 0);

        _view.SetCeramic(null, cracksRepaired: 0, colourVariant: 0);

        for (int i = 0; i < 12; i++)
        {
            Assert.IsFalse(_view.GetCrackRenderer(i).gameObject.activeSelf);
        }
    }

    [Test]
    public void UpdateProgressBar_HalfRepaired_ScalesFillToHalf()
    {
        _view.UpdateProgressBar(cracksRepaired: 2, totalCracks: 4);

        Assert.AreEqual(0.5f, _view.ProgressBarFill.localScale.x, 0.0001f);
    }

    [Test]
    public void UpdateProgressBar_FillStaysPinnedToLeftEdgeAsItGrows()
    {
        _view.UpdateProgressBar(cracksRepaired: 0, totalCracks: 4);
        float leftEdgeAtZero = _view.ProgressBarFill.localPosition.x - (_view.ProgressBarFill.localScale.x * 0.5f);

        _view.UpdateProgressBar(cracksRepaired: 2, totalCracks: 4);
        float leftEdgeAtHalf = _view.ProgressBarFill.localPosition.x - (_view.ProgressBarFill.localScale.x * 0.5f);

        _view.UpdateProgressBar(cracksRepaired: 4, totalCracks: 4);
        float leftEdgeAtFull = _view.ProgressBarFill.localPosition.x - (_view.ProgressBarFill.localScale.x * 0.5f);

        Assert.AreEqual(leftEdgeAtZero, leftEdgeAtHalf, 0.0001f, "the fill's left edge must not move as it grows");
        Assert.AreEqual(leftEdgeAtZero, leftEdgeAtFull, 0.0001f, "the fill's left edge must not move as it grows");
    }

    [Test]
    public void UpdateProgressBar_ZeroTotalCracks_DoesNotThrowAndProducesEmptyBar()
    {
        Assert.DoesNotThrow(() => _view.UpdateProgressBar(0, 0));
        Assert.AreEqual(0f, _view.ProgressBarFill.localScale.x, 0.0001f);
    }
}
