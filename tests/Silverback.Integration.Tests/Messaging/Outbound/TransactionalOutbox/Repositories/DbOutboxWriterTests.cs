﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Database;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Tests.Integration.TestTypes.Database;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.TransactionalOutbox.Repositories
{
    public sealed class DbOutboxWriterTests : IDisposable
    {
        private static readonly IOutboundEnvelope SampleOutboundEnvelope = new OutboundEnvelope(
            new TestEventOne { Content = "Test" },
            new[] { new MessageHeader("one", "1"), new MessageHeader("two", "2") },
            TestProducerEndpoint.GetDefault());

        private readonly SqliteConnection _connection;

        private readonly IServiceScope _scope;

        private readonly TestDbContext _dbContext;

        private readonly DbOutboxWriter _queueWriter;

        public DbOutboxWriterTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddDbContext<TestDbContext>(
                    options => options
                        .UseSqlite(_connection))
                .AddSilverback()
                .UseDbContext<TestDbContext>();

            var serviceProvider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true
                });

            _scope = serviceProvider.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<TestDbContext>();
            _dbContext.Database.EnsureCreated();

            _queueWriter =
                new DbOutboxWriter(_scope.ServiceProvider.GetRequiredService<IDbContext>());
        }

        [Fact]
        public async Task WriteAsync_SomeMessages_TableStillEmpty()
        {
            await _queueWriter.WriteAsync(SampleOutboundEnvelope);
            await _queueWriter.WriteAsync(SampleOutboundEnvelope);
            await _queueWriter.WriteAsync(SampleOutboundEnvelope);

            _dbContext.Outbox.Should().HaveCount(0);
        }

        [Fact]
        public async Task WriteAsyncAndSaveChanges_SomeMessages_MessagesAddedToQueue()
        {
            await _queueWriter.WriteAsync(SampleOutboundEnvelope);
            await _queueWriter.WriteAsync(SampleOutboundEnvelope);
            await _queueWriter.WriteAsync(SampleOutboundEnvelope);
            await _queueWriter.CommitAsync();
            await _dbContext.SaveChangesAsync();

            _dbContext.Outbox.Should().HaveCount(3);
        }

        [Fact]
        public async Task WriteAsyncAndRollbackAsync_SomeMessages_TableStillEmpty()
        {
            await _queueWriter.WriteAsync(SampleOutboundEnvelope);
            await _queueWriter.WriteAsync(SampleOutboundEnvelope);
            await _queueWriter.WriteAsync(SampleOutboundEnvelope);
            await _queueWriter.RollbackAsync();

            _dbContext.Outbox.Should().HaveCount(0);
        }

        [Fact]
        public async Task WriteAsyncCommitAndSaveChanges_Message_MessageCorrectlyAddedToQueue()
        {
            await _queueWriter.WriteAsync(SampleOutboundEnvelope);
            await _queueWriter.WriteAsync(SampleOutboundEnvelope);
            await _queueWriter.WriteAsync(SampleOutboundEnvelope);
            await _queueWriter.CommitAsync();
            await _dbContext.SaveChangesAsync();

            var outboundMessage = _dbContext.Outbox.First();
            outboundMessage.EndpointName.Should().Be("test");
            outboundMessage.MessageType.Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
            outboundMessage.Content.Should().NotBeNullOrEmpty();
            outboundMessage.SerializedHeaders.Should().NotBeNullOrEmpty();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            _connection.Dispose();
            _scope.Dispose();
        }
    }
}