using Amazon.CDK;
using Amazon.CDK.AWS.S3;

namespace Olive.Aws.Cdk
{
    class S3Factory
    {
        public static Bucket CreteMortalBucket(Constructs.Construct scope, string name)
        {
            return new Bucket(scope, name, new BucketProps
            {
                BucketName = name.ToAwsResourceName(),
                RemovalPolicy = RemovalPolicy.DESTROY
            });
        }
    }
}