using System.Text;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Hlibz.MinimalApi.Endpoints.Generators;

/// <summary>
/// Reads a type's XML <c>&lt;summary&gt;</c> from its source as CommonMark text, for the OpenAPI
/// operation description. It reads the comment's text rather than the compiler's documentation,
/// so it works whether or not the project generates a documentation file.
/// </summary>
internal static class SummaryReader
{
    /// <summary>
    /// Reads the summary of the first declaration of <paramref name="type"/> that has one.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    /// <returns>
    /// The summary, or <see langword="null"/> when there is none, it is empty, or the comment
    /// isn't well-formed XML.
    /// </returns>
    public static string? Read(INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        foreach (SyntaxReference reference in type.DeclaringSyntaxReferences)
        {
            string? summary = Read(reference.GetSyntax(cancellationToken));
            if (summary is not null)
            {
                return summary;
            }
        }

        return null;
    }

    private static string? Read(SyntaxNode declaration)
    {
        string comment = CommentText(declaration);
        if (comment.Length == 0)
        {
            return null;
        }

        XElement document;
        try
        {
            document = XElement.Parse($"<doc>{comment}</doc>", LoadOptions.None);
        }
        catch (XmlException)
        {
            // The compiler warns about it (CS1570) when it reads documentation.
            return null;
        }

        if (document.Element("summary") is not { } summary)
        {
            return null;
        }

        List<string> paragraphs = [];
        StringBuilder paragraph = new();
        Render(summary.Nodes(), paragraph, paragraphs);
        Flush(paragraph, paragraphs);
        return paragraphs.Count == 0 ? null : string.Join("\n\n", paragraphs);
    }

    /// <summary>
    /// The XML of the documentation comment in front of the declaration, without the comment
    /// markers. With documentation off the compiler parses <c>///</c> as plain comments, so
    /// those count too.
    /// </summary>
    private static string CommentText(SyntaxNode declaration)
    {
        StringBuilder xml = new();
        foreach (SyntaxTrivia trivia in declaration.GetLeadingTrivia())
        {
            string text = trivia.ToFullString();
            switch (trivia.Kind())
            {
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.SingleLineCommentTrivia when IsDocumentationLine(text):
                    AppendSingleLine(xml, text);
                    break;
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia
                    when text.StartsWith("/**", StringComparison.Ordinal)
                         && !text.StartsWith("/**/", StringComparison.Ordinal):
                    AppendMultiLine(xml, text);
                    break;
                default:
                    // Like the compiler, every documentation comment in front of the
                    // declaration counts, whatever else is between them.
                    break;
            }
        }

        return xml.ToString();
    }

    private static bool IsDocumentationLine(string text) =>
        text.StartsWith("///", StringComparison.Ordinal)
        && !text.StartsWith("////", StringComparison.Ordinal);

    private static void AppendSingleLine(StringBuilder xml, string text)
    {
        foreach (string line in Lines(text))
        {
            string trimmed = line.TrimStart();
            xml.Append(trimmed.StartsWith("///", StringComparison.Ordinal)
                    ? WithoutOneSpace(trimmed.Substring(3))
                    : trimmed)
                .Append('\n');
        }
    }

    private static void AppendMultiLine(StringBuilder xml, string text)
    {
        string body = text.Substring(3);
        if (body.EndsWith("*/", StringComparison.Ordinal))
        {
            body = body.Substring(0, body.Length - 2);
        }

        foreach (string line in Lines(body))
        {
            string trimmed = line.TrimStart();
            xml.Append(trimmed.StartsWith("*", StringComparison.Ordinal)
                    ? WithoutOneSpace(trimmed.Substring(1))
                    : trimmed)
                .Append('\n');
        }
    }

    /// <summary>
    /// The line without the one space that follows the comment marker, so a
    /// <c>&lt;code&gt;</c> block keeps its own indentation.
    /// </summary>
    private static string WithoutOneSpace(string line) =>
        line.StartsWith(" ", StringComparison.Ordinal) ? line.Substring(1) : line;

    private static string[] Lines(string text) =>
        text.Replace("\r\n", "\n").Split('\n');

    private static void Render(
        IEnumerable<XNode> nodes,
        StringBuilder paragraph,
        List<string> paragraphs)
    {
        foreach (XNode node in nodes)
        {
            switch (node)
            {
                case XText text:
                    paragraph.Append(text.Value);
                    break;
                case XElement element:
                    Render(element, paragraph, paragraphs);
                    break;
                default:
                    break;
            }
        }
    }

    private static void Render(
        XElement element,
        StringBuilder paragraph,
        List<string> paragraphs)
    {
        switch (element.Name.LocalName)
        {
            case "para":
                Flush(paragraph, paragraphs);
                Render(element.Nodes(), paragraph, paragraphs);
                Flush(paragraph, paragraphs);
                break;
            case "code":
                Flush(paragraph, paragraphs);
                paragraphs.Add($"```\n{element.Value.Trim('\n', '\r')}\n```");
                break;
            case "c":
                AppendCode(paragraph, element.Value);
                break;
            case "paramref":
            case "typeparamref":
                AppendCode(paragraph, (string?)element.Attribute("name") ?? string.Empty);
                break;
            case "see":
            case "seealso":
                RenderReference(element, paragraph, paragraphs);
                break;
            case "item":
                Flush(paragraph, paragraphs);
                paragraph.Append("- ");
                Render(element.Nodes(), paragraph, paragraphs);
                Flush(paragraph, paragraphs);
                break;
            default:
                Render(element.Nodes(), paragraph, paragraphs);
                break;
        }
    }

    private static void RenderReference(
        XElement element,
        StringBuilder paragraph,
        List<string> paragraphs)
    {
        string content = Collapse(element.Value);
        if ((string?)element.Attribute("href") is { } href)
        {
            paragraph.Append(content.Length == 0 ? href : $"[{content}]({href})");
        }
        else if (content.Length > 0)
        {
            Render(element.Nodes(), paragraph, paragraphs);
        }
        else if ((string?)element.Attribute("langword") is { } keyword)
        {
            AppendCode(paragraph, keyword);
        }
        else if ((string?)element.Attribute("cref") is { } reference)
        {
            AppendCode(paragraph, ShortName(reference));
        }
    }

    private static void AppendCode(StringBuilder paragraph, string code)
    {
        string text = Collapse(code);
        if (text.Length > 0)
        {
            paragraph.Append('`').Append(text).Append('`');
        }
    }

    /// <summary>
    /// The name a reader knows a <c>cref</c> by: <c>T:App.Orders.OrderDto</c> and
    /// <c>OrderDto</c> are both <c>OrderDto</c>, <c>List{T}</c> is <c>List&lt;T&gt;</c>.
    /// </summary>
    private static string ShortName(string reference)
    {
        string name = reference.Length > 2 && reference[1] == ':'
            ? reference.Substring(2)
            : reference;
        int parameters = name.IndexOf('(');
        if (parameters >= 0)
        {
            name = name.Substring(0, parameters);
        }

        int depth = 0;
        int start = 0;
        for (int index = 0; index < name.Length; index++)
        {
            switch (name[index])
            {
                case '{':
                case '<':
                    depth++;
                    break;
                case '}':
                case '>':
                    depth--;
                    break;
                case '.' when depth == 0:
                    start = index + 1;
                    break;
                default:
                    break;
            }
        }

        StringBuilder shortName = new(name.Length - start);
        bool inArity = false;
        foreach (char character in name.Substring(start))
        {
            // Skips a documentation id's generic arity: List`1.
            inArity = character == '`' || (inArity && char.IsDigit(character));
            if (!inArity)
            {
                shortName.Append(character switch
                {
                    '{' => '<',
                    '}' => '>',
                    _ => character,
                });
            }
        }

        return shortName.ToString();
    }

    private static void Flush(StringBuilder paragraph, List<string> paragraphs)
    {
        string text = Collapse(paragraph.ToString());
        paragraph.Clear();
        if (text.Length > 0 && text != "-")
        {
            paragraphs.Add(text);
        }
    }

    /// <summary>
    /// Joins every run of whitespace, line breaks included, into one space.
    /// </summary>
    private static string Collapse(string text)
    {
        StringBuilder collapsed = new(text.Length);
        foreach (char character in text)
        {
            if (!char.IsWhiteSpace(character))
            {
                collapsed.Append(character);
            }
            else if (collapsed.Length > 0 && collapsed[collapsed.Length - 1] != ' ')
            {
                collapsed.Append(' ');
            }
        }

        return collapsed.ToString().TrimEnd();
    }
}
