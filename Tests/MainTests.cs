namespace Tests;

[TestClass]
public class MainTests
{
    RenderOptions inline = new RenderOptions { Mode = RenderMode.Inline };

    RenderOptions expanded = new RenderOptions { Mode = RenderMode.Expanded };

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
    public void TestExpand()
    {
        var shortLine = new RenderOptions { MaxLineLength = 20 };

        HierarchyString Make(Int32 stringLength)
        {
            return Create(tuples, "a", new String('x', stringLength), "b");
        }

        var inlineMinLength = @"( a, x, b )".Length - 1;

        TestRender(@"( a, x, b )", Make(1), shortLine);

        for (var i = 4; i < 25; ++i)
        {
            // i is the line length we're aiming for, but it can get longer even if expanded

            var symbolLength = i - shortLine.Indentation.Length - tuples.separator.Length;

            var source = Make(symbolLength);

            var actual = source.RenderToString(shortLine);

            var lines = actual.Split(new [] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            var longestLine = lines.OrderByDescending(s => s.Length).FirstOrDefault();

            var maxLength = longestLine!.Length;

            // if inline, the max line length should be inlineMinLength + symbolLength
            // if expanded, the max line length should be i and only then it may be greater than shortLine.MaxLineLength

            Assert.IsTrue(maxLength == i || maxLength == inlineMinLength + symbolLength, $"Longest line is {maxLength} (not {i} or {inlineMinLength + i}) characters long:\n\n<{longestLine}>");
        }
    }

    void TestRender(String expected, HierarchyString source, RenderOptions options = null)
    {
        var actual = source.RenderToString(options);

        Assert.AreEqual(expected, actual);
    }
}
