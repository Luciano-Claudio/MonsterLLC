using NUnit.Framework;
using System.Collections.Generic;

public class StairRoutingTests
{
    [Test]
    public void GetNextPosition_ReturnsTheOneAbove()
    {
        var positions = new List<int> { 0, 1, 2 };
        Assert.AreEqual(1, StairRouting.GetNextPosition(positions, 0));
        Assert.AreEqual(2, StairRouting.GetNextPosition(positions, 1));
    }

    [Test]
    public void GetNextPosition_AtTop_ReturnsNull()
    {
        var positions = new List<int> { 0, 1, 2 };
        Assert.IsNull(StairRouting.GetNextPosition(positions, 2));
    }

    [Test]
    public void GetPreviousPosition_AtGround_ReturnsNull()
    {
        var positions = new List<int> { 0, 1, 2 };
        Assert.IsNull(StairRouting.GetPreviousPosition(positions, 0));
    }

    [Test]
    public void GetPreviousPosition_ReturnsTheOneBelow()
    {
        var positions = new List<int> { 0, 1, 2 };
        Assert.AreEqual(0, StairRouting.GetPreviousPosition(positions, 1));
    }

    [Test]
    public void Routing_WorksEvenWhenPositionsAreReassigned()
    {
        // simula uma troca de Active Floor Position — o mesmo tipo de coisa
        // que o Remove Tower Layer vai fazer na Deadline 12.
        var positions = new List<int> { 0, 2, 1 }; // Floor 1 e Floor 2 trocaram de posição
        Assert.AreEqual(1, StairRouting.GetNextPosition(positions, 0));
        Assert.AreEqual(2, StairRouting.GetNextPosition(positions, 1));
    }
}
