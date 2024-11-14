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
using Refactored.Waffle.Scheduler.Jobs;

namespace Refactored.Waffle.Scheduler.Services;

public sealed class JobService(
    ISchedulerFactoryService factoryService, ILogger<JobService> logger) : IJobService
{
    private IScheduler? _scheduler;

    public async Task<bool> DoesJobExist(
        string jobKey, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{Name}] start", nameof(DoesJobExist));
        _scheduler ??= await factoryService.GetScheduler(cancellationToken);

        var existingJobKey = new JobKey(jobKey);
        var exist = await _scheduler.CheckExists(existingJobKey, cancellationToken);
        logger.LogDebug("[{Name}] finished, {IsExist}", nameof(DoesJobExist), exist);
        return exist;
    }

    public async Task ScheduleJob<TCommand>(
        string jobKey, string cronExpression, CancellationToken cancellationToken)
        where TCommand : new()
    {
        await ScheduleJobInternal<TCommand, string>(jobKey, cronExpression, CreateTrigger, cancellationToken);
    }

    public async Task ScheduleJob<TCommand>(
        string jobKey, int minutes, CancellationToken cancellationToken)
        where TCommand : new()
    {
        await ScheduleJobInternal<TCommand, int>(jobKey, minutes, CreateTrigger, cancellationToken);
    }
    public async Task RescheduleJob<TCommand>(
        string jobKey, string newCronExpression, CancellationToken cancellationToken)
        where TCommand : new()
    {
        await RescheduleJobInternal<TCommand, string>(jobKey, newCronExpression, CreateTrigger, cancellationToken);
    }

    public async Task RescheduleJob<TCommand>(
        string jobKey, int minutes, CancellationToken cancellationToken)
        where TCommand : new()
    {
        await RescheduleJobInternal<TCommand, int>(jobKey, minutes, CreateTrigger, cancellationToken);
    }

    public async Task UnscheduleJob(
        string jobKey, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{Name}] start", nameof(UnscheduleJob));

        _scheduler ??= await factoryService.GetScheduler(cancellationToken);

        var triggerKey = new TriggerKey(GetIdentity(jobKey));
        await _scheduler.UnscheduleJob(triggerKey, cancellationToken);

        logger.LogDebug("[{Name}] finished", nameof(UnscheduleJob));
    }

    private async Task RescheduleJobInternal<TCommand, TSchedulingValue>(
        string jobKey, TSchedulingValue schedulingValue,
        Func<string, TSchedulingValue, JobKey, ITrigger> triggerCreator,
        CancellationToken cancellationToken)
        where TCommand : new()
    {
        logger.LogDebug("[{Name}] start", nameof(RescheduleJob));
        _scheduler ??= await factoryService.GetScheduler(cancellationToken);

        await UnscheduleJob(jobKey, cancellationToken);
        await ScheduleJobInternal<TCommand, TSchedulingValue>(jobKey, schedulingValue, triggerCreator, cancellationToken);
    
        logger.LogDebug("[{Name}] finished", nameof(RescheduleJob));
    }

    private async Task ScheduleJobInternal<TCommand, TSchedulingValue>(
        string jobKey, TSchedulingValue schedulingValue,
        Func<string, TSchedulingValue, JobKey, ITrigger> triggerCreator, 
        CancellationToken cancellationToken)
        where TCommand : new()
    {
        logger.LogDebug("[{Name}] start", nameof(ScheduleJob));
        _scheduler ??= await factoryService.GetScheduler(cancellationToken);

        var key = new JobKey(jobKey);
        var jobDetail = JobBuilder
            .Create<SchedulingJob<TCommand>>()
            .WithIdentity(key)
            .Build();

        var trigger = triggerCreator(jobKey, schedulingValue, key);

        var time = await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
        logger.LogDebug("[{Name}] finished, {Time}", nameof(ScheduleJob), time);
    }
    
    private string GetIdentity(string jobKey)
    {
        return $"{jobKey}-Trigger";
    }

    private ITrigger CreateTrigger(string jobKey, int minutes, JobKey key)
    {
        return TriggerBuilder.Create()
            .WithIdentity(new TriggerKey(GetIdentity(jobKey)))
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(minutes).RepeatForever())
            .ForJob(key)
            .Build();
    }

    private ITrigger CreateTrigger(string jobKey, string expressionOrInterval, JobKey key)
    {
        return TriggerBuilder.Create()
            .WithIdentity(new TriggerKey(GetIdentity(jobKey)))
            .WithCronSchedule(expressionOrInterval, x => x.InTimeZone(TimeZoneInfo.Local))
            .ForJob(key)
            .Build();
    }
}