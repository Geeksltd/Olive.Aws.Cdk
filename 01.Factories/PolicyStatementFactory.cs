using Amazon.CDK.AWS.IAM;
using System.Linq;

namespace Olive.Aws.Cdk
{
    public class PolicyStatementFactory
    {
        public static PolicyStatement CreateAllow(Action action, IPrincipal principal = null, params string[] resourceArns) =>
            CreateAllow(new[] { action }, principal, resourceArns);

        internal static PolicyStatement CreateTrustRelationshipsAllow(Action action, string principalArn)
        {
            var result = new PolicyStatement { Effect = Effect.ALLOW };

            result.AddActions(action);

            result.AddArnPrincipal(principalArn);

            return result;
        }

        internal static PolicyStatement CreateAllow(Action[] actions, IPrincipal principal = null, params string[] resourceArns) =>
            Create(Effect.ALLOW, actions, principal, resourceArns);

        static PolicyStatement Create(Effect effect, Action[] actions, IPrincipal principal = null, params string[] resourceArns)
        {
            var result = new PolicyStatement { Effect = effect };

            result.AddActions(actions.Select(a => a.ToString()).ToArray());

            result.AddResources(resourceArns);

            if (principal != null)
                result.AddPrincipals(principal);

            return result;
        }
    }
}