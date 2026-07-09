using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sanity
{
    public static class UrlCleaner
    {
        public static bool TryClean(string text, IList<UrlRule> rules, out string cleaned)
        {
            cleaned = text;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var trimmed = text.Trim();
            if (!LooksLikeUrl(trimmed))
                return false;

            Uri uri;
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            var result = trimmed;
            foreach (var rule in rules)
            {
                if (!DomainMatches(uri.Host, rule.Domain))
                    continue;

                try
                {
                    result = Regex.Replace(result, rule.Regex, string.Empty, RegexOptions.IgnoreCase);
                }
                catch (ArgumentException)
                {
                    // Skip invalid regex patterns in user config.
                }
            }

            result = TidyUrl(result);
            if (result == trimmed)
                return false;

            cleaned = result;
            return true;
        }

        private static bool LooksLikeUrl(string text)
        {
            return text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        private static bool DomainMatches(string host, string domain)
        {
            if (string.IsNullOrWhiteSpace(domain) || domain == "*")
                return true;

            return host.Equals(domain, StringComparison.OrdinalIgnoreCase)
                || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
        }

        private static string TidyUrl(string url)
        {
            url = Regex.Replace(url, @"[?&]+$", string.Empty);
            url = Regex.Replace(url, @"\?&", "?");
            url = Regex.Replace(url, @"&&+", "&");
            return url;
        }
    }
}
