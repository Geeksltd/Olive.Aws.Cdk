using System.Collections.Generic;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SQS;
using Olive.Aws.Cdk.Stacks;

namespace Olive.Aws.Cdk
{
    public class CommandQueue : NamedQueue
    {
        string ConfigKey;

        public CommandQueue(ServiceStack scope, string id, IQueueProps props = null) : base(scope, id, props)
        {
            ConfigKey = ConfigurationNameFormatting.GetCommandQueueConfigKey(scope, Name);
        }

        public void ConfigureConsumeMessages(ServiceStack stack)
        {
            GrantConsumeMessages(stack.RuntimeRole);
            AddToConfiguration(stack);
        }

        public void ConfigureSendMessages(ServiceStack stack)
        {
            this.GrantSendMessages(stack);
            AddToConfiguration(stack);
        }
        public void ConfigureSendMessages(IEnumerable<ServiceStack> stacks) => stacks.Do(ConfigureSendMessages);

        void AddToConfiguration(ServiceStack stack) => stack.ApplicationFunction?.AddApplicationConfig(ConfigKey, GetUrl());

        //public void Trigger(NamedFunction lambdaHandler) =>
        //    lambdaHandler.AddEventSource(new SqsEventSource(this));

    }
}