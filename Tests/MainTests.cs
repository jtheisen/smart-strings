namespace Tests;

[TestClass]
public class MainTests
{
    RenderOptions inline = new RenderOptions { Mode = RenderMode.InlineOnly };

    RenderOptions expanded = new RenderOptions { Mode = RenderMode.ExpandedOnly };

    HierarchyStringBranchInfo tuples = new HierarchyStringBranchInfo("(", ")", ",");

    [TestMethod]
    public void TestBasics()
    {
        var source = Create(tuples, "one", "two", "three");

        TestRender(@"( one, two, three )", source, inline);
        TestRender(@"(
  one,
  two,
  three
)", source, expanded);
    }

    [TestMethod]
    public void TestNested()
    {
        var source = Create(tuples, "one", "two", Create(tuples, "three", "pio"));

        TestRender(@"( one, two, ( three, pio ) )", source, inline);
        TestRender(@"(
  one,
  two,
  (
    three,
    pio
  )
)", source, expanded);
    }

    [TestMethod]
    public void TestCollapse()
    {
        var shortLine = new RenderOptions { MaxLineLength = 16 };

        var source = Create(tuples, Create(tuples, "one", "two"), Create(tuples, "three", "pio"), "done");

        TestRender(@"(
  ( one, two ),
  (
    three,
    pio
  ),
  done
)", source, shortLine);
    }

    void TestRender(String expected, HierarchyString source, RenderOptions options = null)
    {
        var actual = source.RenderToString(options);

        Assert.AreEqual(expected, actual);
    }
}
