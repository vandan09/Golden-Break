using NUnit.Framework;
using UnityEngine;

public class BezierUtilityTests
{
    private static readonly Vector2[] StraightLine =
    {
        new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(2f, 0f), new Vector2(3f, 0f)
    };

    [Test]
    public void Evaluate_AtTZero_ReturnsFirstControlPoint()
    {
        Vector2 result = BezierUtility.Evaluate(StraightLine, 0f);

        Assert.AreEqual(StraightLine[0], result);
    }

    [Test]
    public void Evaluate_AtTOne_ReturnsLastControlPoint()
    {
        Vector2 result = BezierUtility.Evaluate(StraightLine, 1f);

        Assert.AreEqual(StraightLine[3], result);
    }

    [Test]
    public void Evaluate_StraightLine_AtHalfway_IsAtMidpoint()
    {
        Vector2 result = BezierUtility.Evaluate(StraightLine, 0.5f);

        Assert.AreEqual(1.5f, result.x, 0.0001f);
        Assert.AreEqual(0f, result.y, 0.0001f);
    }

    [Test]
    public void Evaluate_CurvedPath_DeviatesFromStraightLineAtMidpoint()
    {
        var curved = new[]
        {
            new Vector2(0f, 0f), new Vector2(0f, 10f), new Vector2(10f, 10f), new Vector2(10f, 0f)
        };

        Vector2 result = BezierUtility.Evaluate(curved, 0.5f);

        Assert.AreEqual(5f, result.x, 0.0001f);
        Assert.Greater(result.y, 0f, "a curved path should bow away from the straight line between its endpoints");
    }

    [Test]
    public void Evaluate_NullControlPoints_ThrowsArgumentException()
    {
        Assert.Throws<System.ArgumentException>(() => BezierUtility.Evaluate(null, 0.5f));
    }

    [Test]
    public void Evaluate_WrongNumberOfControlPoints_ThrowsArgumentException()
    {
        Assert.Throws<System.ArgumentException>(() => BezierUtility.Evaluate(new[] { Vector2.zero, Vector2.one }, 0.5f));
    }
}
