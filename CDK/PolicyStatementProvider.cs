using Amazon.CDK.AWS.IAM;
using Olive;
using System.Linq;

namespace Olive.Aws.Cdk
{
    class PolicyStatementProvider
    {
        internal static PolicyStatement CreateAllowStatement(string[] resourceArns, params Action[] actions)
        {
            return CreateStatement(Effect.ALLOW, resourceArns, actions.Select(c => c.ToString()).ToArray());
        }

        internal static PolicyStatement CreateStatement(Effect effect, string[] resourceArns, params string[] actions)
        {
            var result = new PolicyStatement { Effect = effect };

            result.AddActions(actions);

            result.AddResources(resourceArns);

            return result;
        }
    }
}