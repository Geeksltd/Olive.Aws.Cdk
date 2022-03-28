using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;

namespace Olive.Aws.Cdk
{
    public class NamedFunction : Function
    {
        internal string Name { get; private set; }
        public double? Timeout { get; }
        public NamedFunction(Constructs.Construct scope, string id, IFunctionProps props) : base(scope, id, props)
        {
            Timeout = props.Timeout?.ToSeconds();
            Name = props?.FunctionName;
        }

        internal string GetArn() => this.GetArnFormatter().CreateArn("lambda", "function:" + Name);

        internal void HandleEvent(NamedQueue queue, bool enaled, double? batchSize) =>
            AddEventSource(new SqsEventSource(queue, new SqsEventSourceProps
            {
                Enabled = enaled,
                BatchSize = batchSize
            }));
    }
}