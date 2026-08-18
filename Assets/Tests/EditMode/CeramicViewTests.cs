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
                controlPoints = new[]
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
    public void SetCeramic_ActivatesExactlyTotalCracksRenderers()
    {
        _view.SetCeramic(_fourCrackDefinition, cracksRepaired: 0, colourVariant: 0);

        for (int i = 0; i < 4; i++)
        {
            Assert.IsTrue(_view.GetCrackRenderer(i).gameObject.activeSelf, $"crack {i} should be active");
        }

        for (int i = 4; i < 12; i++)
        {
            Assert.IsFalse(_view.GetCrackRenderer(i).gameObject.activeSelf, $"crack {i} should be inactive (only 4 cracks exist)");
        }
    }

    [Test]
    public void SetCeramic_RepairedCracks_GetGoldColourUnrepairedGetGrey()
    {
        _view.SetCeramic(_fourCrackDefinition, cracksRepaired: 2, colourVariant: 0);

        Assert.AreEqual(UiPalette.GoldFill, _view.GetCrackRenderer(0).startColor);
        Assert.AreEqual(UiPalette.GoldFill, _view.GetCrackRenderer(1).startColor);
        Assert.AreNotEqual(UiPalette.GoldFill, _view.GetCrackRenderer(2).startColor);
        Assert.AreNotEqual(UiPalette.GoldFill, _view.GetCrackRenderer(3).startColor);
    }

    [Test]
    public void SetCeramic_DifferentColourVariants_ProduceDifferentRepairedColour()
    {
        _view.SetCeramic(_fourCrackDefinition, cracksRepaired: 1, colourVariant: 0);
        Color baseVariantColour = _view.GetCrackRenderer(0).startColor;

        _view.SetCeramic(_fourCrackDefinition, cracksRepaired: 1, colourVariant: 2);
        Color secondVariantColour = _view.GetCrackRenderer(0).startColor;

        Assert.AreNotEqual(baseVariantColour, secondVariantColour);
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
