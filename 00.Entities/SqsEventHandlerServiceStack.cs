using System;
using System.Collections.Generic;
using System.Text;

namespace Olive.Aws.Cdk.Stacks
{
    public class SqsEventHandlerServiceStack : ServiceStack
    {
        public SqsEventHandlerServiceStack(Suite scope, string name) : base(scope, name)
        {
        }

        protected override bool HasBackgroundTasks() => false;
        protected override bool HasApiGateway() => false;
        protected override string ApplicationFunctionHandler() => "website::Website.SQSTriggerHandler::FunctionHandler";
    }
}
