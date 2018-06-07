namespace Sitecore.Support.Shell.Controls.RichTextEditor.Pipelines.SaveRichTextContent
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text.RegularExpressions;
  using Sitecore.Configuration;
  using Sitecore.Resources.Media;
  using Sitecore.Web;
  using Sitecore.Shell.Controls.RichTextEditor.Pipelines.SaveRichTextContent;

  /// <summary>
  /// Protect external link a tag from security vulnerability
  /// </summary>
  public class ProtectExternalLink
  {
    private const string HrefRegex = "href[ ]*=[ ]*[\"\']([\\w:\\/\\.\\~\\-\\?\\&\\=\\;]+)[\"]";
    private const string ATagRegex = "(i?)<a([^>]+)>(.+?)</a>";
    private const string ProtectionTag = "rel=\"noopener noreferrer\"";

    /// <summary>
    /// Process
    /// </summary>
    /// <param name="args"></param>
    public void Process([NotNull] SaveRichTextContentArgs args)
    {
      if (string.IsNullOrEmpty(args.Content))
      {
        return;
      }

      if (!Settings.ProtectExternalLinksWithBlankTarget)
      {
        return;
      }

      var matches = this.GetATagMatchCollection(args).Cast<Match>().Distinct();

      foreach (Match match in matches)
      {
        if (!match.Success)
        {
          continue;
        }

        var aNode = match.Value;

        if (string.IsNullOrEmpty(aNode) || !aNode.Contains("_blank"))
        {
          continue;
        }

        if (!this.IsInternalLink(aNode))
        {
          int relStartPosition = aNode.IndexOf(" rel=");

          if (relStartPosition < 0)
          {
            string protectedLink = aNode.Insert(2, " rel=\"noopener noreferrer\"");
            args.Content = args.Content.Replace(aNode, protectedLink);
          }

          else
          {
            relStartPosition += 5;
            int relEndPosition = aNode.IndexOf("\"", relStartPosition + 1) + 1;
            int noopenerPosition = GetValuePosition(aNode, "noopener", relStartPosition, relEndPosition - relStartPosition);
            string protectedLink = aNode;

            if (noopenerPosition < 0)
            {
              protectedLink = protectedLink.Insert(relStartPosition + 1, "noopener ");
              relEndPosition += 9;
              noopenerPosition = relStartPosition + 1;
            }

            int noreferrerPosition = GetValuePosition(aNode, "noreferrer", relStartPosition, relEndPosition - relStartPosition);

            if (noreferrerPosition < 0)
            {
              protectedLink = protectedLink.Insert(noopenerPosition + 8, " noreferrer");
            }

            args.Content = args.Content.Replace(aNode, protectedLink);
          }
        }
      }

    }

    private int GetValuePosition(string input, string substring, int start, int length)
    {
      var regex = new Regex("[\" ]" + substring + "[\" ]", RegexOptions.ECMAScript);

      var match = regex.Match(input, start, length);

      if (!match.Success)
        return -1;

      return match.Index + 1;
    }

    /// <summary>
    /// Is Internal Link
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    protected virtual bool IsInternalLink([NotNull]string html)
    {
      var hrefValue = this.GetHref(html);

      var isInternalLink = this.GetMediaPrefixes().Any(m => hrefValue.StartsWith(m)) ||
        hrefValue.StartsWith(Constants.LinkPrefix) ||
        hrefValue.StartsWith("/") ||
        hrefValue.StartsWith("~") ||
        hrefValue.StartsWith("-");

      if (isInternalLink)
      {
        return true;
      }

      var serverUrl = WebUtil.GetServerUrl(false);

      return !string.IsNullOrEmpty(serverUrl) && html.Contains(serverUrl);
    }

    /// <summary>
    /// Get the matches of Html A tag
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    protected virtual MatchCollection GetATagMatchCollection([NotNull]SaveRichTextContentArgs args)
    {
      var regex = new Regex(ATagRegex, RegexOptions.ECMAScript);

      return regex.Matches(args.Content, 0);
    }

    /// <summary>
    /// Get Href from a tag / node
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    protected virtual string GetHref([NotNull] string html)
    {
      var hrefRegex = new Regex(HrefRegex, RegexOptions.ECMAScript);

      var match = hrefRegex.Match(html);

      if (match.Success && match.Groups.Count > 0)
      {
        return match.Groups[1] != null ? match.Groups[1].Value : match.Groups[0].Value;
      }

      return string.Empty;
    }

    /// <summary>
    /// Get Protected Html
    /// </summary>
    /// <param name="args"></param>
    /// <param name="startWriteIndex"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    [NotNull]
    [Obsolete("The method is not in use any longer.The logic is now is in the Process method")]
    protected virtual string GetProtectedHtml([NotNull]SaveRichTextContentArgs args, int startWriteIndex, int length)
    {
      var tagExtract = args.Content.Substring(startWriteIndex, length);

      return tagExtract.Contains(ProtectionTag) ? args.Content : string.Concat(args.Content.Substring(0, startWriteIndex + 1),
        ProtectionTag,
        args.Content.Substring(startWriteIndex));
    }

    /// <summary>
    /// Get Media Prefixes
    /// </summary>
    /// <returns></returns>
    [NotNull]
    protected virtual List<string> GetMediaPrefixes()
    {
      return MediaManager.Config.MediaPrefixes;
    }
  }
}