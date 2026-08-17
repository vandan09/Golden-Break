using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ObjectPoolTests
{
    private sealed class Dummy
    {
        public bool WasReset;
    }

    [Test]
    public void Get_OnEmptyPool_CallsFactoryAndTracksAsActive()
    {
        int factoryCalls = 0;
        var pool = new ObjectPool<Dummy>(() => { factoryCalls++; return new Dummy(); });

        Dummy item = pool.Get();

        Assert.IsNotNull(item);
        Assert.AreEqual(1, factoryCalls);
        Assert.AreEqual(1, pool.CountActive);
        Assert.AreEqual(0, pool.CountInactive);
    }

    [Test]
    public void Get_WithInitialCapacity_DoesNotCallFactoryAgain()
    {
        int factoryCalls = 0;
        var pool = new ObjectPool<Dummy>(() => { factoryCalls++; return new Dummy(); }, initialCapacity: 3);

        Assert.AreEqual(3, factoryCalls);
        Assert.AreEqual(3, pool.CountInactive);

        pool.Get();

        Assert.AreEqual(3, factoryCalls);
        Assert.AreEqual(1, pool.CountActive);
        Assert.AreEqual(2, pool.CountInactive);
    }

    [Test]
    public void Return_PreviouslyGottenItem_MovesFromActiveToInactiveAndInvokesCallback()
    {
        var pool = new ObjectPool<Dummy>(() => new Dummy(), onReturn: d => d.WasReset = true);
        Dummy item = pool.Get();

        pool.Return(item);

        Assert.AreEqual(0, pool.CountActive);
        Assert.AreEqual(1, pool.CountInactive);
        Assert.IsTrue(item.WasReset);
    }

    [Test]
    public void Return_ItemNotCurrentlyActive_LogsErrorAndDoesNotThrow()
    {
        var pool = new ObjectPool<Dummy>(() => new Dummy());
        var strayItem = new Dummy();

        LogAssert.Expect(LogType.Error, "ObjectPool: attempted to return an item that is not currently active in this pool.");
        Assert.DoesNotThrow(() => pool.Return(strayItem));
        Assert.AreEqual(0, pool.CountInactive);
    }

    [Test]
    public void Get_AfterReturn_ReusesTheSameInstanceInsteadOfAllocatingNew()
    {
        int factoryCalls = 0;
        var pool = new ObjectPool<Dummy>(() => { factoryCalls++; return new Dummy(); });
        Dummy first = pool.Get();
        pool.Return(first);

        Dummy second = pool.Get();

        Assert.AreSame(first, second);
        Assert.AreEqual(1, factoryCalls);
    }

    [Test]
    public void Get_WhenInactiveStackIsExhausted_ExpandsByCallingFactoryAgain()
    {
        int factoryCalls = 0;
        var pool = new ObjectPool<Dummy>(() => { factoryCalls++; return new Dummy(); }, initialCapacity: 1);

        pool.Get();
        pool.Get();

        Assert.AreEqual(2, factoryCalls);
        Assert.AreEqual(2, pool.CountActive);
    }

    [Test]
    public void Constructor_NullFactory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ObjectPool<Dummy>(null));
    }

    [Test]
    public void Clear_WithActiveAndInactiveItems_ResetsAllCountsToZero()
    {
        var pool = new ObjectPool<Dummy>(() => new Dummy(), initialCapacity: 2);
        pool.Get();

        pool.Clear();

        Assert.AreEqual(0, pool.CountActive);
        Assert.AreEqual(0, pool.CountInactive);
    }
}
