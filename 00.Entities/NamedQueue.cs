using Amazon.CDK.AWS.SQS;
using Olive.Aws.Cdk.Stacks;
using System;
using System.Collections.Generic;

namespace Olive.Aws.Cdk
{
    public class NamedQueue : Queue
    {
        internal string Name { get; }
        public NamedQueue(ServiceStack scope, string id, IQueueProps props) : base(scope, id, props)
        {
            Name = props?.QueueName ?? throw new ArgumentNullException(nameof(props));
        }

        public string GetUrl() => $"https://sqs.{Stack.Region}.amazonaws.com/{Stack.Account}/{Name}";

        public void GrantSendMessages(ServiceStack stack) =>
        Grant(stack, $"write-for-{stack.Name.ToLower()}-{Name.ToLower()}", Action.Sqs.Write);
        public void GrantSendMessages(IEnumerable<ServiceStack> stacks) => stacks.Do(GrantSendMessages);

        internal void GrantConsumeMessages(ServiceStack stack) =>
            Grant(stack, $"write-for-{stack.Name.ToLower()}-{Name.ToLower()}", Action.Sqs.Read);

        internal void Grant(ServiceStack stack, string inlinePolicyName, Action[] actions)
        {
            var policy = PolicyFactory.Create(stack,
                                              inlinePolicyName,
                                              inlinePolicyName,
                                              PolicyStatementFactory.CreateAllow(actions, resourceArns: this.GetArn()));

            stack.RuntimeRole.AttachInlinePolicy(policy);
        }

        internal string GetArn() => this.GetArnFormatter().CreateArn("sqs", Name);
    }
}