using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;


public static class GroupedStringExtensions
{
    public static String RenderToString(this GroupedString source, String indentation = ".")
    {
        var writer = new StringWriter();
        GroupedStringRenderer.Render(source, writer, indentation);
        return writer.ToString();
    }
}

public struct GroupedString
{
    internal Object impl;

    public GroupedString(String s)
    {
        impl = s;
    }

    public GroupedString(StringGroup g)
    {
        impl = g;
    }

    public GroupedString(IEnumerable<String> nested, String head = null, String tail = null, String separator = null, Boolean separatorIsTerminator = false)
    {
        impl = new StringGroup(nested.Select(s => new GroupedString(s)).ToArray(), head, tail, separator, separatorIsTerminator);
    }

    public GroupedString(IEnumerable<GroupedString> nested, String head = null, String tail = null, String separator = null, Boolean separatorIsTerminator = false)
    {
        impl = new StringGroup(nested.ToArray(), head, tail, separator, separatorIsTerminator);
    }

    public override string ToString() => this.RenderToString();

    public static implicit operator GroupedString(String s)
        => new GroupedString(s);

    public static GroupedString operator +(GroupedString lhs, GroupedString rhs)
        => new GroupedString(new StringGroup(lhs, rhs));
}

public class GroupedStringVisitor
{
    public void Visit(GroupedString text)
    {
        if (text.impl is String s)
        {
            Visit(s);
        }
        else if (text.impl is StringGroup g)
        {
            Visit(g);
        }
        else if (text.impl != null)
        {
            throw new Exception($"Unknown impl type {text.impl.GetType().Name}");
        }
    }

    protected virtual void Visit(String s) { }
    protected virtual void Visit(StringGroup g) { }
}

public class GroupedStringRenderer : GroupedStringVisitor
{
    Int32 depth;
    TextWriter writer;
    String identation = "  ";

    Boolean isSpacePending = false;
    Boolean isLineTouched = true;
    Boolean areWritingInline = false;

    public static void Render(GroupedString gs, TextWriter writer, String identation)
    {
        new GroupedStringRenderer { writer = writer, identation = identation }.Visit(gs);
    }

    protected override void Visit(string s)
    {
        WriteToken(s);
    }

    protected override void Visit(StringGroup g)
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


public class StringGroup
{
    internal String head, separator, tail;
    internal Boolean separatorIsTerminator;
    internal GroupedString[] children;

    public StringGroup(params GroupedString[] nested)
    {
        this.children = nested;
    }

    public StringGroup(GroupedString[] nested, String head = null, String tail = null, String separator = null, Boolean separatorIsTerminator = false)
    {
        this.children = nested;
        this.head = head;
        this.tail = tail;
        this.separator = separator;
        this.separatorIsTerminator = separatorIsTerminator;
    }

    static StringGroup Create(params GroupedString[] nested)
        => new StringGroup(nested);

    public override string ToString()
        => String.Join("", children.Select(g => g.ToString()));
}
