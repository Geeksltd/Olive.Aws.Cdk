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
                RemovalPolicy = RemovalPolicy.DESTROY,

                // TODO : Check this : https://stackoverflow.com/questions/60199739/find-or-create-s3-bucket-in-cdk
                AutoDeleteObjects = true


            });
        }
    }
}