---
uid: kafka-partitioning
---

# Kafka Partitioning and Key

While using a single poll loop, Silverback processes the messages consumed from each Kafka partition independently and concurrently.

By default up to 10 messages/partitions are processed concurrently (per topic). This value can be tweaked in the endpoint configuration or disabled completely.

# [Fluent](#tab/kafka-consumer-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("order-events")
                    .LimitParallelism(2)
                    .Configure(config =>
                        {
                            config.GroupId = "my-consumer";
                        })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("inventory-events")
                    .ProcessAllPartitionsTogether()
                    .Configure(config =>
                        {
                            config.GroupId = "my-consumer";
                        })));
}
```
# [Legacy](#tab/kafka-consumer-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
                new KafkaConsumerEndpoint("order-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer",
                    },
                    MaxDegreeOfParallelism = 2 
                })
            .AddInbound(
                new KafkaConsumerEndpoint("inventory-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer",
                    },
                    ProcessPartitionsIndependently = false 
                });
}
```
***

# Key

Apache Kafka require a message key for different purposes, such as:
* **Partitioning**: Kafka can guarantee ordering only inside the same partition and it is therefore important to be able to route correlated messages into the same partition. To do so you need to specify a key for each message and Kafka will put all messages with the same key in the same partition.
* **Compacting topics**: A topic can be configured with `cleanup.policy=compact` to instruct Kafka to keep only the latest message related to a certain object, identified by the message key. In other words Kafka will retain only 1 message per each key value.

<figure>
	<a href="~/images/diagrams/kafka-key.png"><img src="~/images/diagrams/kafka-key.png"></a>
    <figcaption>The messages with the same key are guaranteed to be written to the same partition.</figcaption>
</figure>

Silverback will always generate a message key (same value as the `x-message-id` [header](xref:headers)) but it also offers a convenient way to specify a custom key. It is enough to decorate the properties that must be part of the key with <xref:Silverback.Messaging.Messages.KafkaKeyMemberAttribute>.

```csharp
public class MultipleKeyMembersMessage : IIntegrationMessage
{
    public Guid Id { get; set; }

    [KafkaKeyMember]
    public string One { get; set; }
    
    [KafkaKeyMember]
    public string Two { get; set; }

    public string Three { get; set; }
}
```

> [!Note]
> The message key will also be received as header (see <xref:headers> for details).