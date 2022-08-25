using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;


public static class HierarchyStringExtensions
{
    public static String RenderToString(this HierarchyString source, String indentation = ".")
    {
        var writer = new StringWriter();
        HierarchyStringRenderer.Render(source, writer, indentation);
        return writer.ToString();
    }
}

public struct HierarchyString
{
    internal Object impl;

    public HierarchyString(String s)
    {
        impl = s;
    }

    public HierarchyString(HierarchyStringImplementation g)
    {
        impl = g;
    }

    public HierarchyString(IEnumerable<String> nested, String head = null, String tail = null, String separator = null, Boolean separatorIsTerminator = false)
    {
        impl = new HierarchyStringImplementation(nested.Select(s => new HierarchyString(s)).ToArray(), head, tail, separator, separatorIsTerminator);
    }

    public HierarchyString(IEnumerable<HierarchyString> nested, String head = null, String tail = null, String separator = null, Boolean separatorIsTerminator = false)
    {
        impl = new HierarchyStringImplementation(nested.ToArray(), head, tail, separator, separatorIsTerminator);
    }

    public override string ToString() => this.RenderToString();

    public static implicit operator HierarchyString(String s)
        => new HierarchyString(s);

    public static HierarchyString operator +(HierarchyString lhs, HierarchyString rhs)
        => new HierarchyString(new HierarchyStringImplementation(lhs, rhs));
}

public class HierarchyStringVisitor
{
    public void Visit(HierarchyString text)
    {
        if (text.impl is String s)
        {
            Visit(s);
        }
        else if (text.impl is HierarchyStringImplementation g)
        {
            Visit(g);
        }
        else if (text.impl != null)
        {
            throw new Exception($"Unknown impl type {text.impl.GetType().Name}");
        }
    }

    protected virtual void Visit(String s) { }
    protected virtual void Visit(HierarchyStringImplementation g) { }
}

public class HierarchyStringRenderer : HierarchyStringVisitor
{
    Int32 depth;
    TextWriter writer;
    String identation = "  ";

    Boolean isSpacePending = false;
    Boolean isLineTouched = true;
    Boolean areWritingInline = false;

    public static void Render(HierarchyString gs, TextWriter writer, String identation)
    {
        new HierarchyStringRenderer { writer = writer, identation = identation }.Visit(gs);
    }

    protected override void Visit(string s)
    {
        WriteToken(s);
    }

    protected override void Visit(HierarchyStringImplementation g)
    {
        WriteToken(g.head, true);

        ++depth;

        try
        {
            var hadFirst = false;
            foreach (var i in g.children)
            {
                if (hadFirst)
                {
                    WriteToken(g.separator, true, isSeparator: true);
                }

                Visit(i);

                hadFirst = true;
            }

            if (g.separatorIsTerminator)
            {
                WriteToken(g.separator, isSeparator: true);
            }

            if (!areWritingInline)
            {
                WriteLine();
            }
        }
        finally
        {
            --depth;
        }

        WriteToken(g.tail);
    }

    void WriteSeparator(String separator)
    {
        writer.Write(separator);
    }

    void WriteToken(String token, Boolean isSeparation = false, Boolean isSeparator = false)
    {
        EnsureIndentation();

        if (!isSeparator && isSpacePending)
        {
            writer.Write(' ');
        }

        if (isSeparation && !areWritingInline)
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
            writer.Write(identation);
        }

        isLineTouched = true;
    }
}


public class HierarchyStringImplementation
{
    internal String head, separator, tail;
    internal Boolean separatorIsTerminator;
    internal HierarchyString[] children;

    public HierarchyStringImplementation(params HierarchyString[] nested)
    {
        this.children = nested;
    }

    public HierarchyStringImplementation(HierarchyString[] nested, String head = null, String tail = null, String separator = null, Boolean separatorIsTerminator = false)
    {
        this.children = nested;
        this.head = head;
        this.tail = tail;
        this.separator = separator;
        this.separatorIsTerminator = separatorIsTerminator;
    }

    static HierarchyStringImplementation Create(params HierarchyString[] nested)
        => new HierarchyStringImplementation(nested);

    public override string ToString()
        => String.Join("", children.Select(g => g.ToString()));
}
