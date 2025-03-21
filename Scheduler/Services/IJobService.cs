﻿// Copyright (C) 2024 Maxim [maxirmx] Samsonov (www.sw.consulting)
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

namespace Refactored.Waffle.Scheduler.Services;

public interface IJobService
{
    Task<bool> DoesJobExist(
        string jobKey, CancellationToken cancellationToken);

    Task ScheduleJob<TCommand>(
        string jobKey, string cronExpression, CancellationToken cancellationToken)
        where TCommand : new();

    Task ScheduleJob<TCommand>(
        string jobKey, int minutes, CancellationToken cancellationToken)
        where TCommand : new();

    Task RescheduleJob<TCommand>(
        string jobKey, string newCronExpression, CancellationToken cancellationToken)
        where TCommand : new();

    Task RescheduleJob<TCommand>(
        string jobKey, int minutes, CancellationToken cancellationToken)
        where TCommand : new();

    Task UnscheduleJob(
        string jobKey, CancellationToken cancellationToken);
}