using System;

namespace Sanity
{
    public static class LinkOpener
    {
        public static bool Open(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            var config = AppConfig.Load();
            var finalUrl = url.Trim();
            var cleaned = false;

            if (config.IsLinkProxyActive)
            {
                string cleanedUrl;
                if (UrlCleaner.TryClean(finalUrl, config.Rules, out cleanedUrl))
                {
                    finalUrl = cleanedUrl;
                    cleaned = true;
                    UsageMetrics.RecordClean(finalUrl);
                }
            }

            var targetBrowser = ResolveTargetBrowser(config);
            BrowserHelper.TryLaunchBrowser(targetBrowser, finalUrl);
            return cleaned;
        }

        private static string ResolveTargetBrowser(AppConfig config)
        {
            if (!string.IsNullOrWhiteSpace(config.TargetBrowser))
                return config.TargetBrowser;

            return ProtocolRegistration.GetBackedUpBrowser() ?? string.Empty;
        }
    }
}
