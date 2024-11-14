// Copyright (C) 2024 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of OiltrackGateway applcation
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
// TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS
// BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using Microsoft.Extensions.Logging;
using Quartz;

namespace Refactored.Waffle.Scheduler.Services;

internal sealed class SchedulerFactoryService(
    ISchedulerFactory factory, ILogger<SchedulerFactoryService> logger) : ISchedulerFactoryService
{
    private IScheduler? _scheduler;
    private bool _isInitialized;

    private bool _initInProgress;
    private static readonly object InitLock = new();

    public async Task<IScheduler> GetScheduler(CancellationToken cancellationToken)
    {
        if (!_isInitialized)
        {
            await WaitInitialize(cancellationToken);
        }
        return _scheduler!;
    }

    private async Task WaitInitialize(CancellationToken cancellationToken)
    {
        var tryInit = 5;
        const int delayTime = 500;
        while (tryInit > 0)
        {
            if (!_isInitialized)
            {
                await Initialize(cancellationToken);
                if (!_isInitialized)
                {
                    await Task.Delay(delayTime, cancellationToken);
                    if (!_isInitialized)
                    {
                        tryInit--;
                        continue;
                    }
                }
            }

            break;
        }
    }

    private async Task Initialize(CancellationToken cancellationToken)
    {
        lock (InitLock)
        {
            if (_initInProgress)
            {
                return;
            }
            _initInProgress = true;
        }

        if (_isInitialized)
        {
            logger.LogDebug("[{Name}]: Scheduler is already initializing", nameof(SchedulerFactoryService));
            return;
        }

        try
        {
            logger.LogDebug("[{Name}]: Initialize started", nameof(SchedulerFactoryService));
            _scheduler ??= await factory.GetScheduler(cancellationToken);
            await _scheduler.Start(cancellationToken);

            logger.LogDebug("[{Name}]: Initialize finished", nameof(SchedulerFactoryService));
            _isInitialized = true;
            _initInProgress = false;
        }
        catch (Exception e)
        {
            logger.LogError(e, "[{Name}]: Initialize error", nameof(SchedulerFactoryService));
            _initInProgress = false;
            throw;
        }
    }
}