﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Database;
using Silverback.Database.Model;
using Silverback.Messaging.Inbound.ExactlyOnce.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes.Database;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Inbound.ExactlyOnce.Repositories;

public sealed class DbInboundLogTests : IDisposable
{
    private readonly SqliteConnection _connection;

    private readonly IServiceScope _scope;

    private readonly TestDbContext _dbContext;

    private readonly DbInboundLog _inboundLog;

    public DbInboundLogTests()
    {
        _connection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _connection.Open();

        ServiceCollection services = new();

        services
            .AddLoggerSubstitute()
            .AddDbContext<TestDbContext>(
                options => options
                    .UseSqlite(_connection.ConnectionString))
            .AddSilverback()
            .UseDbContext<TestDbContext>();

        ServiceProvider? serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateScopes = true
            });

        _scope = serviceProvider.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<TestDbContext>();
        _dbContext.Database.EnsureCreated();

        _inboundLog = new DbInboundLog(_scope.ServiceProvider.GetRequiredService<IDbContext>());
    }

    [Fact]
    public async Task AddAsync_SomeEnvelopes_TableStillEmpty()
    {
        await _inboundLog.AddAsync(
            new InboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { "x-message-id", "123" }
                },
                new TestOffset("topic1", "1"),
                new TestConsumerConfiguration("topic1").GetDefaultEndpoint()));
        await _inboundLog.AddAsync(
            new InboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { "x-message-id", "456" }
                },
                new TestOffset("topic1", "2"),
                new TestConsumerConfiguration("topic1").GetDefaultEndpoint()));
        await _inboundLog.AddAsync(
            new InboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { "x-message-id", "789" }
                },
                new TestOffset("topic2", "1"),
                new TestConsumerConfiguration("topic2").GetDefaultEndpoint()));

        _dbContext.InboundMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsyncAndCommit_SomeEnvelopes_EnvelopesStored()
    {
        await _inboundLog.AddAsync(
            new InboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { "x-message-id", "123" }
                },
                new TestOffset("topic1", "1"),
                new TestConsumerConfiguration("topic1").GetDefaultEndpoint()));
        await _inboundLog.AddAsync(
            new InboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { "x-message-id", "456" }
                },
                new TestOffset("topic1", "2"),
                new TestConsumerConfiguration("topic1").GetDefaultEndpoint()));
        await _inboundLog.AddAsync(
            new InboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { "x-message-id", "789" }
                },
                new TestOffset("topic2", "1"),
                new TestConsumerConfiguration("topic2").GetDefaultEndpoint()));

        await _inboundLog.CommitAsync();

        _dbContext.InboundMessages.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddAsyncAndRollback_SomeEnvelopes_TableStillEmpty()
    {
        await _inboundLog.AddAsync(
            new InboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { "x-message-id", "123" }
                },
                new TestOffset("topic1", "1"),
                new TestConsumerConfiguration("topic1").GetDefaultEndpoint()));
        await _inboundLog.AddAsync(
            new InboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { "x-message-id", "456" }
                },
                new TestOffset("topic1", "2"),
                new TestConsumerConfiguration("topic1").GetDefaultEndpoint()));
        await _inboundLog.AddAsync(
            new InboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { "x-message-id", "789" }
                },
                new TestOffset("topic2", "1"),
                new TestConsumerConfiguration("topic2").GetDefaultEndpoint()));

        await _inboundLog.RollbackAsync();

        _dbContext.InboundMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsyncAndCommit_Envelope_EnvelopeCorrectlyStored()
    {
        await _inboundLog.AddAsync(
            new InboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { "x-message-id", "123" }
                },
                new TestOffset("topic1", "1"),
                new TestConsumerConfiguration("topic1").GetDefaultEndpoint()));
        await _inboundLog.CommitAsync();

        InboundLogEntry? logEntry = _dbContext.InboundMessages.First();

        logEntry.MessageId.Should().Be("123");
        logEntry.EndpointName.Should().Be("topic1");
    }

    [Fact]
    public async Task ExistsAsync_ExistingEnvelope_TrueReturned()
    {
        InboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { "x-message-id", "123" }
            },
            new TestOffset("topic1", "1"),
            new TestConsumerConfiguration("topic1").GetDefaultEndpoint());

        await _inboundLog.AddAsync(envelope);
        await _inboundLog.CommitAsync();

        bool result = await _inboundLog.ExistsAsync(envelope);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NotExistingMessageId_FalseReturned()
    {
        InboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { "x-message-id", "123" }
            },
            new TestOffset("topic1", "1"),
            new TestConsumerConfiguration("topic1").GetDefaultEndpoint());

        await _inboundLog.AddAsync(envelope);
        await _inboundLog.CommitAsync();

        envelope.Headers.AddOrReplace("x-message-id", "456");

        bool result = await _inboundLog.ExistsAsync(envelope);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ExistingMessageIdWithDifferentTopicName_FalseReturned()
    {
        await _inboundLog.AddAsync(
            new InboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { "x-message-id", "123" }
                },
                new TestOffset("topic1", "1"),
                new TestConsumerConfiguration("topic1").GetDefaultEndpoint()));
        await _inboundLog.CommitAsync();

        InboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { "x-message-id", "123" }
            },
            new TestOffset("topic2", "1"),
            new TestConsumerConfiguration("topic2").GetDefaultEndpoint());

        bool result = await _inboundLog.ExistsAsync(envelope);

        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
        _scope.Dispose();
    }
}
