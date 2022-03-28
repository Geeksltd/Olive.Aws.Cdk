using Amazon.CDK.AWS.SecretsManager;

namespace Olive.Aws.Cdk
{
    class NamedSecret : Secret
    {
        internal string Name { get; private set; }
        public NamedSecret(Constructs.Construct scope, string id, ISecretProps? props = null) : base(scope, id, props)
        {
            Name = props?.SecretName;
        }
    }
}