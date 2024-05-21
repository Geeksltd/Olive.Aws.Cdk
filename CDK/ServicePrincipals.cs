using Amazon.CDK.AWS.IAM;

namespace Olive.Aws.Cdk
{
    class ServicePrincipals
    {
        static ServicePrincipal CreatePrincipal(string serviceName) => new(serviceName + ".amazonaws.com");

        internal static ServicePrincipal Lambda => CreatePrincipal("lambda");
        internal static ServicePrincipal CodePipeline => CreatePrincipal("codepipeline");
        internal static ServicePrincipal CodeBuild => CreatePrincipal("codebuild");
        internal static ServicePrincipal Ses => CreatePrincipal("ses");
    }
}