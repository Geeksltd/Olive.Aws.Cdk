using Amazon.CDK.AWS.S3;

namespace Olive.Aws.Cdk
{
    public class NamedBucket : Bucket
    {
        internal string Name { get; }
        public NamedBucket(Constructs.Construct scope, string id, IBucketProps? props = null)
            : base(scope, id, props)
        {
            Name = props?.BucketName;
        }

        internal string GetArn() => this.GetArnFormatter().CreateArn("s3", Name, false, false);

    }
}