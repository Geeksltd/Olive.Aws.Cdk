using Amazon.CDK.AWS.EC2;

namespace Olive.Aws.Cdk
{
    class SecurityGroupFactory
    {
        public static SecurityGroup Create(Constructs.Construct scope, string name, IVpc vpc)
        {
            return new SecurityGroup(
                            scope,
                            name,
                            new SecurityGroupProps
                            {
                                SecurityGroupName = name,
                                Vpc = vpc
                            });
        }
    }
}