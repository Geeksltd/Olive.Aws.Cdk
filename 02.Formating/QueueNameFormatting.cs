using Infrastructure.Stacks;
using Olive;

namespace Olive.Aws.Cdk
{
    public class QueueNameFormatting
    {
        public static string FifoCommand(ServiceStack handler) => handler.Name + "Command.fifo";

        public static string FifoCommand(ServiceStack owner, string commandName) => ToOwnerSpecificName(owner, commandName.EnsureEndsWith("Command").EnsureEndsWith(".fifo"));
        public static string Command(ServiceStack owner, string commandName) => ToOwnerSpecificName(owner, commandName.EnsureEndsWith("Command").TrimEnd(".fifo"));

        public static string FifoPull(ServiceStack publisher, ServiceStack consumer) => ToOwnerSpecificName(publisher, consumer.Name + ".fifo");

        public static string FifoWildCard(ServiceStack publisher) => publisher.Name + "_*";

        static string ToOwnerSpecificName(ServiceStack owner, string name) => owner.Name + "_" + name;
    }
}