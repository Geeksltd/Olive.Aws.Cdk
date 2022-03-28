using Infrastructure.Stacks;
using Olive;
using System;

namespace Olive.Aws.Cdk
{
    class ConfigurationNameFormatting
    {
        internal static string GetQueueConfigKey(ServiceStack publisher, ServiceStack subscriber) =>
            $"DataReplication:{publisher.Name.WithSuffix("Service") + "_" + subscriber.Name.WithSuffix("Endpoint")}:Url";

        internal static string GetCommandQueueConfigKey(ServiceStack commandProvider, string commandName) =>
            $"EventBus:Queues:{commandName.TrimStart(commandProvider.Name + "_").WithPrefix(commandProvider.Name + "Service_").TrimEnd(".fifo").EnsureEndsWith("Command")}:Url";
    }
}