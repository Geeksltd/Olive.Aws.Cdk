using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using System.Collections.Generic;
using System.Linq;
using kms = Amazon.CDK.AWS.KMS;

namespace Olive.Aws.Cdk
{
    public static class Extensions
    {
        public static ArnFormatter GetArnFormatter(this IResource resource) => new(resource.Stack);

        public static string GetArn(this kms.IKey key) => key.GetArnFormatter().CreateArn("kms", "key/" + key.KeyId);

        public static string GetArn(this Amazon.CDK.AWS.EFS.FileSystem @this) =>
            @this.GetArnFormatter().CreateArn("elasticfilesystem", "filesystem/" + @this.FileSystemId);

        internal static string ToAwsResourceName(this string name) => name.ToLower();

        internal static Duration Seconds(this int @this) => Duration.Seconds(@this);
        internal static Duration Seconds(this double @this) => Duration.Seconds(@this);

        public static Function AddApplicationConfig(this Function @this, string key, string value) =>
            @this.AddEnvironment("CONFIG__" + key.Replace(":", "__"), value);

        internal static SubnetSelection ToSubnetSelection(this IEnumerable<ISubnet> @this) => new() { Subnets = @this.ToArray() };

        internal static ISubnet[] GetLambdaSubnets(this IVpc @this) => @this.GetSubnets("Lambda");

        internal static ISubnet[] GetDatabaseSubnets(this IVpc @this) => @this.GetSubnets("Database");

        internal static ISubnet[] GetSubnets(this IVpc @this, string groupName)
            => @this.SelectSubnets(new SubnetSelection
            {
                SubnetGroupName = "Database"
            }).Subnets;

        internal static void GrantWrite(this Amazon.CDK.AWS.EFS.FileSystem @this, Amazon.CDK.AWS.IAM.Role role)
        {
            role.AddToPolicy(PolicyStatementFactory.CreateAllow(actions: new[]
            {
                Action.ElasticFileSystem.ClientWrite,
                Action.ElasticFileSystem.ClientMount,
                Action.ElasticFileSystem.DescribeMountTargets
            }, resourceArns: @this.GetArn()));
        }
    }
}
