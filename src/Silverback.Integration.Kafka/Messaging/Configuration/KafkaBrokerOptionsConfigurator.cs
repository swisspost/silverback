// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Behaviors;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Configuration
{
    public class KafkaBrokerOptionsConfigurator : IBrokerOptionsConfigurator<KafkaBroker>
    {
        public void Configure(IBrokerOptionsBuilder options) =>
            options.SilverbackBuilder.AddSingletonBehavior<KafkaMessageKeyBehavior>();
    }
}