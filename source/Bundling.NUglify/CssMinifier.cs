﻿using System;
using Karambolo.AspNetCore.Bundling.Css;
using Microsoft.Extensions.Logging;
using NUglify;
using NUglify.Css;

namespace Karambolo.AspNetCore.Bundling.NUglify
{
    public class CssMinifier : ICssMinifier
    {
        private readonly CssSettings _settings;
        private readonly ILogger _logger;

        public CssMinifier(CssSettings settings, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _settings = settings;
            _logger = loggerFactory.CreateLogger<CssMinifier>();
        }

        public string Process(string content, string filePath)
        {
            UglifyResult result = Uglify.Css(content, _settings);

            if (result.Errors.Count > 0)
            {
                var message =
                    result.HasErrors ?
                    $"Css minification of '{{FILEPATH}}' failed:{Environment.NewLine}{{REASON}}" :
                    $"Css minification of '{{FILEPATH}}' completed with warnings:{Environment.NewLine}{{REASON}}";

                _logger.LogWarning(message,
                    (filePath ?? "(content)"),
                    string.Join(Environment.NewLine, result.Errors));

                if (result.HasErrors)
                    return content;
            }

            return result.Code;
        }
    }
}
