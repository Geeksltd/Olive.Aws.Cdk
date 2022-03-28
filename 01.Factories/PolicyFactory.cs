using Amazon.CDK.AWS.IAM;

namespace Olive.Aws.Cdk
{
    public class PolicyFactory
    {
        public static Policy Create(Constructs.Construct scope, string id, string policyName, params PolicyStatement[] statements)
        {
            return new Policy(scope, id, new PolicyProps
            {
                PolicyName = policyName,
                Document = new PolicyDocument(new PolicyDocumentProps
                {
                    Statements = statements
                })
            });
        }
    }
}