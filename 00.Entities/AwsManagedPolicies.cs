using Amazon.CDK.AWS.IAM;

namespace Olive.Aws.Cdk
{
    class AwsManagedPolicies
    {
        internal static IManagedPolicy AWSLambdaBasicExecutionRole => ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole");
    }
}