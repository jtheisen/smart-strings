using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

public enum RenderMode
{
    Inline,
    Expanded
}

public class RenderOptions
{
    internal static readonly RenderOptions Default = new RenderOptions();

    public RenderMode? Mode { get; set; }

    public Int32 MaxLineLength { get; set; } = 74;

    public String Indentation { get; set; } = "  ";
}

public static class HierarchyStringExtensions
{
    public static String RenderToString(this HierarchyString source, RenderOptions options = null)
    {
        var writer = new StringWriter();
        HierarchyStringRenderer.Render(source, writer, options);
        return writer.ToString();
    }
}

public struct HierarchyString
{
    internal Object impl;

    public override string ToString() => this.RenderToString();

    public static implicit operator HierarchyString(String s) => new HierarchyString { impl = s };

    public static implicit operator HierarchyString(HierarchyStringBranch b) => new HierarchyString { impl = b };
}

public static class HierarchyStringCreation
{
    public static HierarchyString Create(HierarchyStringBranchInfo branchInfo, params HierarchyString[] children)
    {
        return new HierarchyStringBranch(children, branchInfo);
    }

    public static HierarchyString Create(IEnumerable<String> nested, String head = null, String tail = null, String separator = null, Boolean separatorIsTerminator = false)
    {
        return new HierarchyStringBranch(nested.Select(s => (HierarchyString)s).ToArray(), new HierarchyStringBranchInfo(head, tail, separator, separatorIsTerminator));
    }

    public static HierarchyString Create(IEnumerable<HierarchyString> nested, String head = null, String tail = null, String separator = null, Boolean separatorIsTerminator = false)
    {
        var childArray = nested as HierarchyString[] ?? nested.ToArray();

        return new HierarchyStringBranch(childArray, new HierarchyStringBranchInfo(head, tail, separator, separatorIsTerminator));
    }
}

public class HierarchyStringVisitor
{
    public virtual void Visit(HierarchyString text)
    {
        if (text.impl is String s)
        {
            Visit(s);
        }
        else if (text.impl is HierarchyStringBranch b)
        {
            Visit(ref b.info, b.children);
        }
        else if (text.impl != null)
        {
            throw new Exception($"Unknown impl type {text.impl.GetType().Name}");
        }
    }

    protected virtual void Visit(String s) { }
    protected virtual void Visit(ref HierarchyStringBranchInfo branchInfo, HierarchyString[] children) { }
}

public class HierarchyStringLengthMeasurerer : HierarchyStringVisitor
{
    Int32 max;
    Int32 length;

    public Boolean IsTooLong(HierarchyString source, Int32 max)
    {
        Reset(max);
        Visit(source);
        return length >= max;
    }

    public void Reset(Int32 max)
    {
        this.max = max;
        this.length = 0;
    }

    protected override void Visit(string s) => Record(s);

    protected override void Visit(ref HierarchyStringBranchInfo branchInfo, HierarchyString[] children)
    {
        Record(branchInfo.head);

        Record(branchInfo.separator, children.Length - (branchInfo.separatorIsTerminator ? 0 : 1));

        foreach (var child in children)
        {
            if (length >= max) break;

            Visit(child);
        }
    }

    void Record(String s, Int32 times = 1)
    {
        if (s != null)
        {
            length += times * (s.Length + 1);
        }
    }
}

public class HierarchyStringRenderer : HierarchyStringVisitor
{
    Int32 depth;
    TextWriter writer;
    RenderOptions options;

    Boolean isSpacePending = false;
    Boolean isLineTouched = true;
    RenderMode? setMode;
    RenderMode haveMode = RenderMode.Inline;

    HierarchyStringLengthMeasurerer measurer;

    public static void Render(HierarchyString gs, TextWriter writer, RenderOptions options)
    {
        new HierarchyStringRenderer
        {
            writer = writer,
            options = options ?? RenderOptions.Default,
            measurer = new HierarchyStringLengthMeasurerer(),
            setMode = options.Mode
        }
        .Visit(gs);
    }

    public override void Visit(HierarchyString text)
    {
        var isTooLong = measurer.IsTooLong(text, options.MaxLineLength - options.Indentation.Length * depth);

        var hadMode = haveMode;

        if (isTooLong)
        {
            haveMode = setMode ?? RenderMode.Expanded;
        }

        try
        {
            base.Visit(text);
        }
        finally
        {
            haveMode = hadMode;
        }
    }

    protected override void Visit(string s)
    {
        WriteToken(s);
    }

    protected override void Visit(ref HierarchyStringBranchInfo branch, HierarchyString[] children)
    {
        WriteToken(branch.head, true);

        ++depth;

        try
        {
            var hadFirst = false;
            foreach (var i in children)
            {
                if (hadFirst)
                {
                    WriteToken(branch.separator, true, isSeparator: true);
                }

                Visit(i);

                hadFirst = true;
            }

            if (branch.separatorIsTerminator)
            {
                WriteToken(branch.separator, isSeparator: true);
            }

            if (haveMode == RenderMode.Expanded)
            {
                WriteLine();
            }
        }
        finally
        {
            --depth;
        }

        WriteToken(branch.tail);
    }

    void WriteToken(String token, Boolean isSeparation = false, Boolean isSeparator = false)
    {
        EnsureIndentation();

        if (!isSeparator && isSpacePending)
        {
            writer.Write(' ');
        }

        if (isSeparation && haveMode == RenderMode.Expanded)
        {
            writer.WriteLine(token);

            isLineTouched = false;
            isSpacePending = false;
        }
        else
        {
            writer.Write(token);

            isLineTouched = true;
            isSpacePending = true;
        }
    }

    void WriteLine()
    {
        writer.WriteLine();

        isLineTouched = false;
        isSpacePending = false;
    }

    void EnsureIndentation()
    {
        if (isLineTouched) return;

        for (var i = 0; i < depth; ++i)
        {
            writer.Write(options.Indentation);
        }

        isLineTouched = true;
    }
}

public struct HierarchyStringBranchInfo
{
    public String head, tail, separator;
    public Boolean separatorIsTerminator;

    public HierarchyStringBranchInfo(String head = null, String tail = null, String separator = null, Boolean separatorIsTerminator = false)
    {
        this.head = head;
        this.tail = tail;
        this.separator = separator;
        this.separatorIsTerminator = separatorIsTerminator;
    }
}

public class HierarchyStringBranch
{
    internal HierarchyStringBranchInfo info;
    internal HierarchyString[] children;

    public HierarchyStringBranch(HierarchyString[] nested, HierarchyStringBranchInfo info)
    {
        this.children = nested;
        this.info = info;
    }
}
