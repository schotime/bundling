﻿using System;
using System.Threading;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Karambolo.AspNetCore.Bundling.Internal.Caching
{
    public class FileSystemBundleCacheTest : BundleCacheTest
    {
        private static int s_counter;
        private string _basePath;
        private CancellationTokenSource _cts;
        private FileSystemBundleCache _cache;
        protected override IBundleCache Cache => _cache;

        protected override bool ProvidesPhysicalFiles => true;

        private void Cleanup(string basePath)
        {
            _cache.Reset();
        }

        protected override void Setup(TimeSpan? expirationScanFrequency)
        {
            var loggerProvider = new ConsoleLoggerProvider((s, l) => l >= LogLevel.Warning, true);
            var loggerFactory = new LoggerFactory(new[] { loggerProvider });

            _cts = new CancellationTokenSource();

            var cache = new FileSystemBundleCache(_cts.Token, null, loggerFactory, Clock,
                Options.Create(new FileSystemBundleCacheOptions
                {
                    FileProvider = new PhysicalFileProvider(Environment.CurrentDirectory),
                    EnableExpirationScanning = expirationScanFrequency.HasValue,
                    ExpirationScanFrequency = expirationScanFrequency ?? default(TimeSpan),
                    AutoResetOnCreate = true,
                    BasePath = "Cache" + Interlocked.Increment(ref s_counter).ToString()
                }),
                Options.Create(new BundleGlobalOptions
                {
                    EnableChangeDetection = true
                }));

            _cache = cache;
            _basePath = cache.PhysicalBasePath;
        }

        protected override void Teardown()
        {
            _cts.Cancel();
            _cts.Dispose();

            _cache.Reset();
        }
    }
}

