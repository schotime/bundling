﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE-THIRD-PARTY in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Karambolo.AspNetCore.Bundling.Tools.Infrastructure
{
    public class ProcessRunner
    {
        private readonly IReporter _reporter;

        public ProcessRunner(IReporter reporter)
        {
            if (reporter == null)
                throw new ArgumentNullException(nameof(reporter));

            _reporter = reporter;
        }

        // May not be necessary in the future. See https://github.com/dotnet/corefx/issues/12039
        public async Task<int> RunAsync(ProcessSpec processSpec, CancellationToken cancellationToken)
        {
            if (processSpec == null)
                throw new ArgumentNullException(nameof(processSpec));

            int exitCode;

            var stopwatch = new Stopwatch();

            using (Process process = CreateProcess(processSpec))
            using (var processState = new ProcessState(process))
            {
                cancellationToken.Register(() => processState.TryKill());

                stopwatch.Start();
                process.Start();
                _reporter.Verbose($"Started '{processSpec.Executable}' with process id {process.Id}");

                if (processSpec.IsOutputCaptured)
                {
                    await Task.WhenAll(
                        processState.Task,
                        ConsumeStreamAsync(process.StandardOutput, processSpec.OutputCapture.AddLine),
                        ConsumeStreamAsync(process.StandardError, processSpec.OutputCapture.AddLine)
                    );
                }
                else
                {
                    await processState.Task;
                }

                exitCode = process.ExitCode;
                stopwatch.Stop();
                _reporter.Verbose($"Process id {process.Id} ran for {stopwatch.ElapsedMilliseconds}ms");
            }

            return exitCode;
        }

        private Process CreateProcess(ProcessSpec processSpec)
        {
            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo =
                {
                    FileName = processSpec.Executable,
                    Arguments = ArgumentEscaper.EscapeAndConcatenate(processSpec.Arguments),
                    UseShellExecute = false,
                    WorkingDirectory = processSpec.WorkingDirectory,
                    RedirectStandardOutput = processSpec.IsOutputCaptured,
                    RedirectStandardError = processSpec.IsOutputCaptured,
                }
            };

            foreach (System.Collections.Generic.KeyValuePair<string, string> env in processSpec.EnvironmentVariables)
            {
                process.StartInfo.Environment.Add(env.Key, env.Value);
            }

            return process;
        }

        private static async Task ConsumeStreamAsync(StreamReader reader, Action<string> consume)
        {
            string line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                consume?.Invoke(line);
            }
        }

        private class ProcessState : IDisposable
        {
            private readonly Process _process;
            private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
            private volatile bool _disposed;

            public ProcessState(Process process)
            {
                _process = process;
                _process.Exited += OnExited;
            }

            public Task Task => _tcs.Task;

            public void TryKill()
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.KillTree();
                    }
                }
                catch
                { }
            }

            private void OnExited(object sender, EventArgs args)
                => _tcs.TrySetResult(null);

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    TryKill();
                    _process.Exited -= OnExited;
                    _process.Dispose();
                }
            }
        }
    }
}
