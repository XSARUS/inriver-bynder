using Bynder.Models;
using Bynder.Sdk.Model;
using Bynder.Sdk.Query.Asset;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bynder.Utils.Helpers
{
    using SdkIBynderClient = Sdk.Service.IBynderClient;

    public static class MediaHelper
    {
        // Cache compiled regex instances (thread-safe)
        private static readonly ConcurrentDictionary<string, Regex> _regexCache =
            new ConcurrentDictionary<string, Regex>(StringComparer.Ordinal);

        public static async Task<(string Url, string Filename)> GetDownloadUrlAndFilename(
            inRiverContext context,
            SdkIBynderClient bynderClient,
            Media asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            string originalExtension = GetOriginalExtension(asset);

            var mappings = SettingHelper
                .GetFilenameExtensionMediaTypeMapping(context.Settings, context.Logger);

            context.Log(LogLevel.Verbose,
                $"Got {mappings.Count} filename-ext mappings!");

            if (!TryGetMappedThumbnail(
                context,
                asset,
                originalExtension,
                mappings,
                out var mappedResult))
            {
                mappedResult = await GetFallbackDownloadAsync(context, bynderClient, asset);
            }

            // should we use ID as prefix for the filename?
            if (SettingHelper.ShouldAddAssetIdPrefixToFilename(context.Settings, context.Logger))
            {
                mappedResult.Filename = $"{asset.Id}_{mappedResult.Filename}";
            }

            return mappedResult;
        }

        internal static string GetOriginalExtension(Media asset)
        {
            return Path.GetExtension(asset.GetOriginalFileName())
                .Replace(".", string.Empty)
                .ToLowerInvariant();
        }

        internal static bool TryGetMappedThumbnail(
            inRiverContext context,
            Media asset,
            string originalExtension,
            IReadOnlyDictionary<string, List<MediaTypeTransformConfig>> mappings,
            out (string Url, string Filename) result)
        {
            result = default;

            if (!mappings.TryGetValue(originalExtension, out var extensionMappings))
            {
                context.Log(LogLevel.Verbose,
                    $"No mappings found for filename-ext '{originalExtension}'!");
                return false;
            }

            foreach (var mapping in extensionMappings)
            {
                if (!asset.Thumbnails.All.TryGetValue(mapping.MediaType, out JToken token))
                {
                    context.Log(LogLevel.Verbose,
                        $"Asset thumbnails do not contain key '{mapping.MediaType}' for filename-ext '{originalExtension}'!");
                    continue;
                }

                string downloadUrl = token.Value<string>();
                string filename = FormatFilename(asset, downloadUrl, mapping.FilenameRegex);

                context.Log(LogLevel.Verbose,
                    $"Mapping hit for mediatype '{mapping.MediaType}', formatted filename '{filename}'");

                result = (downloadUrl, filename);
                return true;
            }

            return false;
        }

        internal static string FormatFilename(
            Media asset,
            string downloadUrl,
            string regexPattern)
        {
            var uri = new Uri(downloadUrl);
            string filename = Path.GetFileName(uri.LocalPath);

            if (string.IsNullOrWhiteSpace(filename))
                filename = asset.GetOriginalFileName();

            if (!string.IsNullOrWhiteSpace(regexPattern))
            {
                var regex = _regexCache.GetOrAdd(
                    regexPattern.Trim(),
                    pattern => new Regex(
                        pattern,
                        RegexOptions.IgnoreCase | RegexOptions.Compiled));

                filename = regex.Replace(filename, string.Empty);
            }

            return filename;
        }

        internal static async Task<(string Url, string Filename)> GetFallbackDownloadAsync(
            inRiverContext context,
            SdkIBynderClient bynderClient,
            Media asset)
        {
            string downloadMediaType =
                SettingHelper.GetDownloadMediaType(context.Settings, context.Logger);

            context.Log(LogLevel.Verbose,
                $"Fall back to configured download-mediatype '{downloadMediaType}'!");

            if (downloadMediaType.Equals("original", StringComparison.OrdinalIgnoreCase))
            {
                Uri downloadLocation =
                    await bynderClient.GetAssetService()
                        .GetDownloadFileUrlAsync(
                            new DownloadMediaQuery { MediaId = asset.Id });

                return (downloadLocation.ToString(), asset.GetOriginalFileName());
            }

            if (asset.Thumbnails.All.TryGetValue(downloadMediaType, out JToken token))
            {
                string url = token.Value<string>();

                string filename =
                    asset.MediaItems
                        .FirstOrDefault(mi =>
                            mi.Type.Equals(downloadMediaType,
                                StringComparison.OrdinalIgnoreCase))
                        ?.Name
                    ?? asset.GetOriginalFileName();

                context.Log(LogLevel.Verbose,
                    $"Using thumbnail for configured download-mediatype '{downloadMediaType}'");

                return (url, filename);
            }

            context.Log(LogLevel.Error,
                $"Unable to resolve download media type '{downloadMediaType}' for asset '{asset.Id}'.");

            throw new InvalidOperationException(
                $"Download media type '{downloadMediaType}' could not be resolved for asset '{asset.Id}'.");
        }
    }
}