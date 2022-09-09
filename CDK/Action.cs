using System.Linq;

namespace Olive.Aws.Cdk
{
    public class Action
    {
        public string Name { get; private set; }

        protected virtual string Prefix => GetType().Name.ToLower() + ":";

        public override string ToString() => Prefix + Name;

        internal class Ec2 : Action
        {
            public static Ec2 DescribeNetworkInterfaces { get; } = new Ec2 { Name = nameof(DescribeNetworkInterfaces) };
            public static Ec2 CreateNetworkInterface { get; } = new Ec2 { Name = nameof(CreateNetworkInterface) };
            public static Ec2 DeleteNetworkInterface { get; } = new Ec2 { Name = nameof(DeleteNetworkInterface) };
            public static Ec2 DescribeInstances { get; } = new Ec2 { Name = nameof(DescribeInstances) };
            public static Ec2 AttachNetworkInterface { get; } = new Ec2 { Name = nameof(AttachNetworkInterface) };
        }

        public class Kms : Action
        {
            internal static Kms All => new Kms { Name = "*" };
            public static Kms GenerateDataKey { get; } = new Kms { Name = nameof(GenerateDataKey) };
            public static Kms Decrypt { get; } = new Kms { Name = nameof(Decrypt) };
            public static Kms Encrypt { get; } = new Kms { Name = nameof(Encrypt) };
        }

        public class Sts : Action
        {
            public static Sts AssumeRole { get; } = new Sts { Name = nameof(AssumeRole) };
        }

        public class CloudWatchLogs : Action
        {
            public static CloudWatchLogs DescribeLogGroups { get; } = new CloudWatchLogs { Name = nameof(DescribeLogGroups) };
            public static CloudWatchLogs FilterLogEvents { get; } = new CloudWatchLogs { Name = nameof(FilterLogEvents) };
        }

        public class Lambda : Action
        {
            public static Lambda UpdateFunctionCode { get; } = new Lambda { Name = nameof(UpdateFunctionCode) };
        }

        public class Ssm : Action
        {
            public static Ssm PutParameter { get; } = new Ssm { Name = nameof(PutParameter) };
            public static Ssm GetParametersByPath { get; } = new Ssm { Name = nameof(GetParametersByPath) };
        }

        public class ElasticFileSystem : Action
        {
            public static ElasticFileSystem ClientWrite { get; } = new ElasticFileSystem { Name = nameof(ClientWrite) };
            public static ElasticFileSystem ClientMount { get; } = new ElasticFileSystem { Name = nameof(ClientMount) };
            public static ElasticFileSystem DescribeMountTargets { get; } = new ElasticFileSystem { Name = nameof(DescribeMountTargets) };


        }

        public class S3 : Action
        {
            public static S3 Abort { get; } = new S3 { Name = nameof(Abort) };
            public static S3 DeleteObject { get; } = new S3 { Name = nameof(DeleteObject) };
            public static S3 GetBucket { get; } = new S3 { Name = nameof(GetBucket) };
            public static S3 GetObject { get; } = new S3 { Name = nameof(GetObject) };
            public static S3 List { get; } = new S3 { Name = nameof(List) };
            public static S3 PutObject { get; } = new S3 { Name = nameof(PutObject) };

            public static S3[] ReadWrite => new[]
            {
                Abort,
                DeleteObject,
                GetBucket,
                GetObject,
                List,
                PutObject
            };

            public override string ToString() => base.ToString() + "*";
        }

        public class Sqs : Action
        {
            public static Sqs SendMessage { get; } = new Sqs { Name = nameof(SendMessage) };
            public static Sqs ReceiveMessage { get; } = new Sqs { Name = nameof(ReceiveMessage) };
            public static Sqs PurgeQueue { get; } = new Sqs { Name = nameof(PurgeQueue) };
            public static Sqs SendMessageBatch { get; } = new Sqs { Name = nameof(SendMessageBatch) };
            public static Sqs DeleteMessageBatch { get; } = new Sqs { Name = nameof(DeleteMessageBatch) };
            public static Sqs DeleteMessage { get; } = new Sqs { Name = nameof(DeleteMessage) };
            public static Sqs GetQueueAttributes { get; } = new Sqs { Name = nameof(GetQueueAttributes) };

            public static Sqs[] Write => new[]
            {
                SendMessage,
                SendMessageBatch
            };

            public static Sqs[] Read => new[]
           {
                ReceiveMessage,
                PurgeQueue,
                DeleteMessageBatch,
                DeleteMessage
            };

            public static Sqs[] ReadWrite => Write.Union(Read).ToArray();
        }

        public static implicit operator string(Action action) => action.ToString();
    }
}