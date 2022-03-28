using Amazon.CDK;
using Olive;

namespace Olive.Aws.Cdk
{
    public class ArnFormatter
    {
        readonly Stack Stack;
        public ArnFormatter(Stack stack) => Stack = stack;

        internal string CreateArn(string service, string resourceName, bool includeRegion = true, bool includeAccountId = true) =>
            $"arn:aws:{service}:{Stack.Region.OnlyWhen(includeRegion)}:{Stack.Account.OnlyWhen(includeAccountId)}:{resourceName}";

        internal string AllSQSOf(string name) => CreateArn("sqs", name, true, true);
    }
}