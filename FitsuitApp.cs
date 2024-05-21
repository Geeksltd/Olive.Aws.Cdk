using Amazon.CDK;
using Olive.Aws.Cdk.Stacks;
using Olive;
using System;
using System.Collections.Generic;
using System.Linq;
using cdk = Amazon.CDK;
using cfrManager = Amazon.CDK.AWS.CertificateManager;
using ec2 = Amazon.CDK.AWS.EC2;
using kms = Amazon.CDK.AWS.KMS;
using route53 = Amazon.CDK.AWS.Route53;
using s3 = Amazon.CDK.AWS.S3;
using efs = Amazon.CDK.AWS.EFS;
using Olive.Aws.Cdk;

namespace Olive.Aws.Cdk
{
    public abstract class Suite : App
    {
        internal route53.IHostedZone HostedZone { get; }
        public string Domain { get; }
        internal string AppEnvironmentName { get; }
        internal cdk.Stack ApplicationResourceStack { get; }
        internal cfrManager.ICertificate Certificate { get; }
        public kms.IKey ApplicationMasterKey { get; private set; }
        public ec2.IVpc Vpc { get; }
        public ec2.ISubnet[] LambdaSubnets { get; }
        public string Name { get; }
        public s3.Bucket ApplicationSourcecodeBucket { get; }
        public s3.Bucket UserRoleBucket { get; }
        public ec2.SecurityGroup LambdaSecurityGroup { get; }

        public string Timezone;
        protected Suite(Props props) : base(props)
        {
            Name = props.Name;
            Domain = props.Domain;
            AppEnvironmentName = props.AppEnvironmentName;
            ApplicationResourceStack = new cdk.Stack(this, "ApplicationResourceStack");
            Vpc = ec2.Vpc.FromVpcAttributes(ApplicationResourceStack, Name + "Vpc", new ec2.VpcAttributes { VpcId = props.VpcId, AvailabilityZones = ApplicationResourceStack.AvailabilityZones });
            LambdaSubnets = props.LambdaSubnetIds.Select(s => ec2.Subnet.FromSubnetId(ApplicationResourceStack, s, s)).ToArray();
            Certificate = CreateCertificate(props);
            HostedZone = CreateHostedZone(props);
            LambdaSecurityGroup = SecurityGroupFactory.Create(ApplicationResourceStack, Name + "LambdaSecurityGroup", Vpc);
            ApplicationSourcecodeBucket = S3Factory.CreteMortalBucket(ApplicationResourceStack, Name + "Sourcecode");
            UserRoleBucket = S3Factory.CreteMortalBucket(ApplicationResourceStack, Name + "UserRoles");
            Timezone = props.Timezone;

            WithKMSMasterKey();

            AddStacks();

            ServiceStacks.Do(s =>
            {
                UserRoleBucket.GrantRead(s.RuntimeRole);
                s.ApplicationFunction.AddApplicationConfig("Authentication:UserRolesBucket", UserRoleBucket.BucketName);
            });

            OnPrepare();
            OnPrepared();
        }


        internal event System.Action OnPrepareEvent;
        internal event System.Action OnPreparedEvent;

        protected virtual void OnPrepare()
        {
            OnPrepareEvent?.Invoke();
        }

        protected virtual void OnPrepared()
        {
            OnPreparedEvent?.Invoke();
        }

        protected abstract void AddStacks();

        cfrManager.ICertificate CreateCertificate(Props props)
        {
            return cfrManager.Certificate.FromCertificateArn(ApplicationResourceStack, "ApplicationCertificate", props.CertificateArn);
        }

        route53.IHostedZone CreateHostedZone(Props props)
        {
            return route53.HostedZone.FromHostedZoneAttributes(ApplicationResourceStack, props.HostedZoneId,
                            new route53.HostedZoneAttributes
                            {
                                HostedZoneId = props.HostedZoneId,
                                ZoneName = props.Domain
                            });
        }

        void WithKMSMasterKey()
        {
            ApplicationMasterKey =
                new kms.Key(ApplicationResourceStack, Name + "ApplicationMasterKey", new kms.KeyProps
                {
                    RemovalPolicy = RemovalPolicy.DESTROY,
                    Enabled = true,
                    Alias = Name + "ApplicationMasterKey"
                });

            ApplicationMasterKey.AddToResourcePolicy(PolicyStatementFactory.CreateAllow(Action.Kms.All, new cdk.AWS.IAM.AccountRootPrincipal(), "*"));
        }

        public IEnumerable<ServiceStack> ServiceStacks => Node.Children.OfType<ServiceStack>();

        public TStack Service<TStack>() where TStack : ServiceStack
            => ServiceStacks.OfType<TStack>().FirstOrDefault()
            ?? throw new Exception($"{typeof(TStack).Name} stack has not been added to the app.");

        public class Props : AppProps
        {
            public string Name;
            public string Domain;
            public string HostedZoneId;
            public string CertificateArn;
            public string AppEnvironmentName;
            public string VpcId;
            public string[] LambdaSubnetIds;
            public string Timezone;
        }
    }
}