namespace Tests;

[TestClass]
public class MainTests
{
    [TestMethod]
    public void TestMethod1()
    {
        var sample = new HierarchyString(
            new[]
            {
            "foo",
            new HierarchyString(new [] { "i1", "i2" }, "{", "}", ","),
            "bar"
            },
            "[",
            "]",
            ";",
            separatorIsTerminator: true
        );

        Assert.AreEqual("", sample.RenderToString());
    }
}
