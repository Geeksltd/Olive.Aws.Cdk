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
            public static Ec2 DescribeNetworkInterfaces { get; } = new() { Name = nameof(DescribeNetworkInterfaces) };
            public static Ec2 CreateNetworkInterface { get; } = new() { Name = nameof(CreateNetworkInterface) };
            public static Ec2 DeleteNetworkInterface { get; } = new() { Name = nameof(DeleteNetworkInterface) };
            public static Ec2 DescribeInstances { get; } = new() { Name = nameof(DescribeInstances) };
            public static Ec2 AttachNetworkInterface { get; } = new() { Name = nameof(AttachNetworkInterface) };
        }

        public class Kms : Action
        {
            internal static Kms All => new() { Name = "*" };
            public static Kms GenerateDataKey { get; } = new() { Name = nameof(GenerateDataKey) };
            public static Kms Decrypt { get; } = new() { Name = nameof(Decrypt) };
            public static Kms Encrypt { get; } = new() { Name = nameof(Encrypt) };
        }

        public class Ses : Action
        {
            internal static Ses All => new() { Name = "*" };
            public static Ses SendEmail { get; } = new() { Name = nameof(SendEmail) };
            public static Ses SendRawEmail { get; } = new() { Name = nameof(SendRawEmail) };
        }

        public class Sts : Action
        {
            public static Sts AssumeRole { get; } = new() { Name = nameof(AssumeRole) };
        }

        public class CloudWatchLogs : Action
        {
            public static CloudWatchLogs DescribeLogGroups { get; } = new() { Name = nameof(DescribeLogGroups) };
            public static CloudWatchLogs FilterLogEvents { get; } = new() { Name = nameof(FilterLogEvents) };
        }

        public class Lambda : Action
        {
            public static Lambda UpdateFunctionCode { get; } = new() { Name = nameof(UpdateFunctionCode) };
        }

        public class Ssm : Action
        {
            public static Ssm PutParameter { get; } = new() { Name = nameof(PutParameter) };
            public static Ssm GetParametersByPath { get; } = new() { Name = nameof(GetParametersByPath) };
        }

        public class ElasticFileSystem : Action
        {
            public static ElasticFileSystem ClientWrite { get; } = new() { Name = nameof(ClientWrite) };
            public static ElasticFileSystem ClientMount { get; } = new() { Name = nameof(ClientMount) };
            public static ElasticFileSystem DescribeMountTargets { get; } = new() { Name = nameof(DescribeMountTargets) };


        }

        public class S3 : Action
        {
            public static S3 Abort { get; } = new() { Name = nameof(Abort) };
            public static S3 DeleteObject { get; } = new() { Name = nameof(DeleteObject) };
            public static S3 GetBucket { get; } = new() { Name = nameof(GetBucket) };
            public static S3 GetObject { get; } = new() { Name = nameof(GetObject) };
            public static S3 ListBucket { get; } = new() { Name = nameof(ListBucket) };
            public static S3 PutObject { get; } = new() { Name = nameof(PutObject) };
            public static S3 WildcardObject { get; } = new() { Name = "*Object" };

            public static S3[] ReadWrite => new[]
            {
                Abort,
                DeleteObject,
                GetBucket,
                GetObject,
                ListBucket,
                PutObject
            };

            public override string ToString() => base.ToString() + "*";
        }

        public class Sqs : Action
        {
            public static Sqs SendMessage { get; } = new() { Name = nameof(SendMessage) };
            public static Sqs ReceiveMessage { get; } = new() { Name = nameof(ReceiveMessage) };
            public static Sqs PurgeQueue { get; } = new() { Name = nameof(PurgeQueue) };
            public static Sqs SendMessageBatch { get; } = new() { Name = nameof(SendMessageBatch) };
            public static Sqs DeleteMessageBatch { get; } = new() { Name = nameof(DeleteMessageBatch) };
            public static Sqs DeleteMessage { get; } = new() { Name = nameof(DeleteMessage) };
            public static Sqs GetQueueAttributes { get; } = new() { Name = nameof(GetQueueAttributes) };

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