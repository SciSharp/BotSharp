using HtmlAgilityPack;

namespace BotSharp.Plugin.WebDriver;

public static class WebPageHelper
{
    public static HtmlNode GetElement(string html, string selector)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        return htmlDoc.DocumentNode.SelectSingleNode(selector);
    }

    public static HtmlNodeCollection GetElements(string html, string selector)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        return htmlDoc.DocumentNode.SelectNodes(selector);
    }

    public static string RemoveElements(string html, string selector)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var nodesToRemove = htmlDoc.DocumentNode.SelectNodes(selector);

        if (nodesToRemove != null)
        {
            foreach (var node in nodesToRemove)
            {
                node.Remove();
            }
        }

        return htmlDoc.DocumentNode.OuterHtml;
    }

    public static string RemoveAttribute(string html, string attrName)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        // Select all elements with a style attribute
        var nodesWithStyle = htmlDoc.DocumentNode.SelectNodes($"//*[@{attrName}]");

        if (nodesWithStyle != null)
        {
            foreach (var node in nodesWithStyle)
            {
                node.Attributes.Remove(attrName);
            }
        }

        return htmlDoc.DocumentNode.OuterHtml;
    }
}
