using Amazon.CDK.AWS.SQS;
using Olive.Aws.Cdk.Stacks;

namespace Olive.Aws.Cdk
{
    class SqsFactory
    {
        internal static CommandQueue CreateCommand(ServiceStack scope, string name, bool isFifo = true, double? visibilityTimeout = 30)
        {
            var props = isFifo ? new QueueProps { QueueName = name, Fifo = true } : new QueueProps { QueueName = name };
            if (visibilityTimeout.HasValue)
                props.VisibilityTimeout = Amazon.CDK.Duration.Seconds(visibilityTimeout.Value);

            return new CommandQueue(scope, name, props);
        }

        internal static NamedQueue CreateFIFO(ServiceStack scope, string name, double? visibility = 60) =>
            CreateQueue(scope, name, true, visibility);

        static NamedQueue CreateQueue(ServiceStack scope, string name, bool fifo, double? visibility = 60) =>
            new(scope, name, new QueueProps { Fifo = fifo, QueueName = name, VisibilityTimeout = visibility?.Seconds() });
    }
}