using NUnit.Framework;
using UnityEngine;

public class FloorActivationCheckTests
{
    [Test]
    public void IsActive_SameFloor_ReturnsTrue()
    {
        var floorObj = new GameObject();
        var floor = floorObj.AddComponent<FloorDefinition>();

        Assert.IsTrue(FloorActivationCheck.IsActive(floor, floor));

        Object.DestroyImmediate(floorObj);
    }

    [Test]
    public void IsActive_DifferentFloor_ReturnsFalse()
    {
        var floorAObj = new GameObject();
        var floorA = floorAObj.AddComponent<FloorDefinition>();
        var floorBObj = new GameObject();
        var floorB = floorBObj.AddComponent<FloorDefinition>();

        Assert.IsFalse(FloorActivationCheck.IsActive(floorA, floorB));

        Object.DestroyImmediate(floorAObj);
        Object.DestroyImmediate(floorBObj);
    }

    [Test]
    public void IsActive_NoOwnerFloor_ReturnsTrue()
    {
        var floorObj = new GameObject();
        var floor = floorObj.AddComponent<FloorDefinition>();

        Assert.IsTrue(FloorActivationCheck.IsActive(null, floor));

        Object.DestroyImmediate(floorObj);
    }
}
