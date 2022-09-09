using Amazon.CDK.AWS.Lambda;

namespace Olive.Aws.Cdk
{
    public class RuntimeFactory
    {
        public static Runtime Create(string customRuntime)
        {
            switch (customRuntime)
            {
                case "DOTNET_CORE_NODE_JS": return CreateDotNetCoreNodeJsRuntime();
            }

            return Runtime.DOTNET_CORE_3_1;
        }

        private static Runtime CreateDotNetCoreNodeJsRuntime()
        {
            return new Runtime("DOTNET_CORE_NODE_JS", RuntimeFamily.DOTNET_CORE,
                new LambdaRuntimeProps
                {
                    BundlingDockerImage = ""
                });
        }
    }
}