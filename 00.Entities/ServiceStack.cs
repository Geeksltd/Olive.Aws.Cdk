using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using System.Linq;
using System.Collections.Generic;
using apigatewayV2 = Amazon.CDK.AWS.APIGatewayv2;
using events = Amazon.CDK.AWS.Events;
using eventTargets = Amazon.CDK.AWS.Events.Targets;
using route53 = Amazon.CDK.AWS.Route53;
using ssm = Amazon.CDK.AWS.SSM;
using Newtonsoft.Json;
using efs = Amazon.CDK.AWS.EFS;
using System.IO;
using System;
using Amazon.CDK.AWS.APIGatewayv2.Integrations;
using System.ComponentModel;

namespace Olive.Aws.Cdk.Stacks
{
    public abstract class ServiceStack<TSuite> : ServiceStack where TSuite : Suite
    {
        protected TSuite App => (TSuite)base.App;

        protected ServiceStack(TSuite scope, string name, ServiceStackProps props) : base(scope, name, props)
        {
        }
    }

    public abstract class ServiceStack : Stack
    {
        protected readonly ArnFormatter ArnFormatter;

        public string Subdomain { get; private set; }

        public string Name { get; private set; }

        internal NamedBucket ApplicationBucket { get; private set; }

        internal NamedBucket TempApplicationBucket { get; private set; }

        protected efs.FileSystem FileSystem { get; private set; }

        public Role RuntimeRole { get; private set; }

        public NamedFunction ApplicationFunction { get; private set; }

        protected NamedStringParameter ApplicationSecrets { get; private set; }

        NamedStringParameter ConfigurationsParameterStore { get; set; }

        protected readonly Suite App;

        protected efs.AccessPoint FileSystemAccessPoint { get; private set; }

        Amazon.CDK.AWS.EC2.Connections_ Connections;

        protected events.Rule BackgroundTaskRule { get; private set; }

        internal string ToFullStackResourceName(string suffix = null) => (App.Name + "-" + Name + suffix).ToAwsResourceName();

        protected ServiceStack()
        {
        }

        protected ServiceStack(Suite scope, string name, ServiceStackProps props = null) : base(scope, name, props)
        {
            App = scope;
            Name = name;
            ArnFormatter = new ArnFormatter(this);
            Subdomain = props?.Subdomain;

            CreateRuntimeRole();

            GrantMasterKeyPermissions();

            GrantStoringAntiForgeryKeyInSystemsManagerParameters();

            if (HasFileSystem())
                WithFileSystem();

            if (HasApplicationBucket())
                WithApplicationBucket();

            if (HasApplicationSecrets())
                WithApplicationSecrets();

            if (StoreConfigurationsInSystemsParameters())
                App.OnPrepared += () => WithConfigurationsParameterStore();

            WithFunction(Name + "ApplicationFunction", ToFullStackResourceName(), props?.AssetDirectory);

            if (HasBackgroundTasks())
                WithCloudWatchBackgroundTaskTrigger();

            if (HasApiGateway())
                WithApiGateway();


        }

        protected virtual bool HasFileSystem() => false;

        private void WithFileSystem()
        {

            FileSystem = new efs.FileSystem(this, Name + "FileSystem", new efs.FileSystemProps
            {
                Vpc = App.Vpc,
                LifecyclePolicy = efs.LifecyclePolicy.AFTER_90_DAYS,
                PerformanceMode = efs.PerformanceMode.MAX_IO,
                VpcSubnets = App.LambdaSubnets.ToSubnetSelection(),
                SecurityGroup = App.LambdaSecurityGroup,
                Encrypted = true
            });

            FileSystemAccessPoint =
                FileSystem.AddAccessPoint(Name + "AccessPoint", new efs.AccessPointOptions
                {

                    Path = GetFileSystemAccessPointPath().ToLower(),
                    // as /export/lambda does not exist in a new efs filesystem, the efs will create the directory with the following createAcl
                    CreateAcl = new efs.Acl
                    {
                        OwnerUid = "1001",
                        OwnerGid = "1001",
                        Permissions = "750"
                    },
                    //// enforce the POSIX identity so lambda function will access with this identity
                    PosixUser = new efs.PosixUser
                    {
                        Uid = "1001",
                        Gid = "1001"
                    }
                });

            FileSystem.Node.Children.Do(el =>
            {
                if (el is efs.CfnMountTarget mountTarget)
                    FileSystemAccessPoint.Node.AddDependency(mountTarget);
            });


            FileSystem.GrantWrite(RuntimeRole);
        }
        protected virtual string GetFileSystemAccessPointPath() => "/mnt/" + Name;

        protected virtual bool HasApiGateway() => true;

        protected IEnumerable<ServiceStack> GetSiblingServiceStacks() => App.ServiceStacks.Except(this);

        void WithCloudWatchBackgroundTaskTrigger()
        {
            BackgroundTaskRule = new events.Rule(this, Name + "BackgroundTaskRule", new events.RuleProps
            {
                RuleName = Name + "BackgroundTaskRule",
                Schedule = events.Schedule.Rate(Duration.Minutes(1)),
                Targets = new[]
                {
                    new eventTargets.LambdaFunction(ApplicationFunction, new eventTargets.LambdaFunctionProps
                    {
                         Event = events.RuleTargetInput.FromObject(CreateTriggerEventObject())
                    })
                }
            });

            // AddApplicationConfiguration("Automated.Tasks:Enabled", "true");
        }

        static object CreateTriggerEventObject() =>
            Newtonsoft.Json.JsonConvert.DeserializeObject("{ \"RequestContext\": { \"Http\": { \"Path\": \"/olive-trigger-tasks\" } } }");

        void GrantStoringAntiForgeryKeyInSystemsManagerParameters()
        {
            RuntimeRole.AttachInlinePolicy(
                PolicyFactory.Create(
                                      this,
                                      "AntiForgeryKeyStorage",
                                      "AntiForgeryKeyStorage",
                                        PolicyStatementFactory.CreateAllow(
                                            new[]
                                            {
                                                Action.Ssm.PutParameter,
                                                Action.Ssm.PutParameter
                                            },
                                            resourceArns: "*")));
        }

        void GrantMasterKeyPermissions()
        {
            RuntimeRole.AttachInlinePolicy(
                PolicyFactory.Create(
                    this,
                    "KMS",
                    "KMSEncryptDecrypt",
                            PolicyStatementFactory.CreateAllow(
                                GetMasterKeyPermittedActions().ToArray(),
                                resourceArns: App.ApplicationMasterKey.GetArn())));
        }

        private IEnumerable<Action> GetMasterKeyPermittedActions()
        {
            if (AuthenticateUsers())
                yield return Action.Kms.GenerateDataKey;

            yield return Action.Kms.Decrypt;
        }

        protected virtual bool AuthenticateUsers() => false;

        void WithApplicationSecrets()
        {
            ApplicationSecrets = new NamedStringParameter(this, Name + "ApplicationSecrets", new ssm.StringParameterProps
            {
                ParameterName = ToFullStackResourceName(),
                Type = ssm.ParameterType.STRING,
                StringValue = "{}" //<-- DO NOT CHANGE THIS LINE

            });

            ApplicationSecrets.GrantRead(RuntimeRole);

            RuntimeRole.AddToPolicy(PolicyStatementFactory
                .CreateAllow(Action.Ssm.GetParametersByPath, resourceArns: ApplicationSecrets.ParameterArn + "/"));
        }

        Dictionary<string, object> ConfigurationsParameterStoreValues = new Dictionary<string, object>();
        void WithConfigurationsParameterStore()
        {
            if (ConfigurationsParameterStoreValues.None())
                return;

            ConfigurationsParameterStore = new NamedStringParameter(this, Name + "Configurations", new ssm.StringParameterProps
            {
                ParameterName = ToFullStackResourceName("-Configurations"),
                Type = ssm.ParameterType.STRING,
                Tier = ssm.ParameterTier.ADVANCED, // Standard has character limit
                StringValue = JsonConvert.SerializeObject(ConfigurationsParameterStoreValues, Formatting.Indented)
            });

            ConfigurationsParameterStore.GrantRead(RuntimeRole);

            RuntimeRole.AddToPolicy(PolicyStatementFactory
                .CreateAllow(Action.Ssm.GetParametersByPath, resourceArns: ConfigurationsParameterStore.ParameterArn + "/"));

            ApplicationFunction.AddEnvironment("CONFIGURATIONS_PARAMETER_STORE", ConfigurationsParameterStore.Name);
        }

        void CreateRuntimeRole()
        {
            RuntimeRole = new Role(this, RuntimeRoleName(), new RoleProps
            {
                RoleName = RuntimeRoleName(),
                AssumedBy = ServicePrincipals.Lambda
            });

            RuntimeRole.AddManagedPolicy(AwsManagedPolicies.AWSLambdaBasicExecutionRole);

            var pushPolicy = PolicyFactory.Create(this, "SQS_PUSH", "SQS_Push_Wildcard",
                 PolicyStatementFactory.CreateAllow(Action.Sqs.Write, resourceArns: ArnFormatter.AllSQSOf(QueueNameFormatting.FifoWildCard(this))));

            RuntimeRole.AttachInlinePolicy(pushPolicy);

            var networkInterfacePermission = PolicyFactory.Create(this, Name + "Ec2NetworkInterface", "Ec2NetworkInterface",
                PolicyStatementFactory.CreateAllow(new[] {
                    Action.Ec2.CreateNetworkInterface,
                    Action.Ec2.DeleteNetworkInterface,
                    Action.Ec2.DescribeNetworkInterfaces,
                    Action.Ec2.DescribeInstances,
                    Action.Ec2.AttachNetworkInterface,
                }, resourceArns: "*"));

            RuntimeRole.AttachInlinePolicy(networkInterfacePermission);

            // TODO: To be removed after full migration
            RuntimeRole.AttachInlinePolicy(PolicyFactory.Create(RuntimeRole, "SQS_Permissions", "SQS_Permissions",
                PolicyStatementFactory.CreateAllow(Action.Sqs.ReadWrite, resourceArns:
                new[]
                {
                    $"arn:aws:sqs:eu-west-1:669486994886:{Name}Service-*",
                    $"arn:aws:sqs:eu-west-1:669486994886:{Name}-*"
                })));

            // TODO: To be removed after full migration
            RuntimeRole.AttachInlinePolicy(PolicyFactory.Create(RuntimeRole, "EmailCommand_Permissions", "EmailCommand_Permissions",
                PolicyStatementFactory.CreateAllow(Action.Sqs.ReadWrite, resourceArns:
                    $"arn:aws:sqs:eu-west-1:669486994886:EmailService-SendEmailCommand"
                )));

            // TODO : To be removed after full migration
            RuntimeRole.AttachInlinePolicy(PolicyFactory.Create(RuntimeRole, "TasksService-CreateTaskCommand", "TasksService-CreateTaskCommand",
              PolicyStatementFactory.CreateAllow(Action.Sqs.ReadWrite, resourceArns:
                  $"arn:aws:sqs:eu-west-1:669486994886:TasksService-CreateTaskCommand"
              )));
        }

        protected virtual string ApplicationBucketName() => ToFullStackResourceName();

        protected virtual bool HasApplicationBucket() => true;

        protected virtual bool HasApplicationSecrets() => true;
        protected virtual bool StoreConfigurationsInSystemsParameters() => false;

        protected virtual bool HasBackgroundTasks() => true;

        protected virtual string RuntimeRoleName() => ToFullStackResourceName();

        protected ServiceStack WithApplicationBucket()
        {
            ApplicationBucket = CreateBucket(ToFullStackResourceName(), Name + "ApplicationBucket");

            TempApplicationBucket = new NamedBucket(this, Name + "TempApplicationBucket", new BucketProps
            {
                BucketName = ToFullStackResourceName().WithPrefix("temp"),
                RemovalPolicy = RemovalPolicy.DESTROY,
                BlockPublicAccess = new BlockPublicAccess(new BlockPublicAccessOptions
                {
                    BlockPublicPolicy = false,
                }),
                LifecycleRules = new[]
                {
                    new LifecycleRule
                    {
                        Enabled = true,
                        Expiration = Duration.Days(1)
                    }
                },
                ObjectOwnership =  ObjectOwnership.BUCKET_OWNER_ENFORCED,
                Cors = new[]
                {
                    new CorsRule
                    {
                       AllowedOrigins = new []{"https://*."+App.Domain,},
                       AllowedHeaders =  new [] { "*" },
                       AllowedMethods = new [] {HttpMethods.POST, HttpMethods.GET  }
                    }
                }
            });

            TempApplicationBucket.GrantReadWrite(RuntimeRole);

            return this;
        }

        protected NamedBucket CreateBucket(string bucketName, string bucketId, RemovalPolicy removalPolicy = RemovalPolicy.RETAIN)
        {
            var result = new NamedBucket(this, bucketId, new BucketProps
            {
                BucketName = bucketName,
                RemovalPolicy = removalPolicy,
                Versioned = true,
                ObjectOwnership = ObjectOwnership.BUCKET_OWNER_ENFORCED
            });

            result.GrantReadWrite(RuntimeRole);

            return result;
        }

        protected NamedBucket CreateBucketForSES(string bucketName, string configKey)
        {
            var sesBucket = CreateBucket(bucketName, bucketName + "_SES_BUCKET", Amazon.CDK.RemovalPolicy.DESTROY);
            sesBucket.AddToResourcePolicy(PolicyStatementFactory.CreateAllow(Action.S3.PutObject, ServicePrincipals.Ses, resourceArns: new[] { sesBucket.BucketArn + "/*" }));

            AddApplicationConfiguration(configKey, bucketName);
            return sesBucket;
        }

        protected virtual LambdaMemorySize LambdaMemorySize() => Cdk.LambdaMemorySize.Small;

        protected virtual int LambdaTimeoutSeconds() => 60;

        protected virtual string CustomRuntime() => "";

        protected virtual string ApplicationFunctionHandler() => "website::Website.ApiGatewayLambdaHandler::FunctionHandlerAsync";

        protected ServiceStack WithFunction(string id, string functionName, string assetDirectory)
        {
            var assetPath = (assetDirectory ?? "/tmp/") + "BaseFunction.zip";

            try
            {
                assetPath.AsFile().DeleteIfExists();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var assembly = assemblies.Single(a => a.FullName?.Contains("Olive.Aws.Cdk") ?? false);
                var baseFunction = assembly.ReadEmbeddedResource("Olive.Aws.Cdk.BaseFunction.zip");
                using var stream = new FileStream(assetPath, FileMode.CreateNew);
                stream.Write(baseFunction, 0, baseFunction.Length);
                stream.Close();
            }
            catch (Exception e)
            {
                Log.For<ServiceStack>().Error(e);
                throw;
            }

            ApplicationFunction = new NamedFunction(this, id,
                new FunctionProps
                {
                    FunctionName = functionName,
                    Code = Code.FromAsset(assetPath),
                    Runtime = RuntimeFactory.Create(CustomRuntime()),
                    Handler = ApplicationFunctionHandler(),
                    Timeout = LambdaTimeoutSeconds().Seconds(),
                    MemorySize = (int)LambdaMemorySize(),
                    Role = RuntimeRole,
                    SecurityGroups = new[] { App.LambdaSecurityGroup },
                    LogRetention = Amazon.CDK.AWS.Logs.RetentionDays.ONE_DAY,
                    Vpc = App.Vpc,
                    VpcSubnets = App.LambdaSubnets.ToSubnetSelection(),
                    Filesystem = GetLambdaFunctionFileSystem()
                });

            ApplicationFunction.AddEnvironment("TZ", App.Timezone);

            ApplicationFunction.AddEnvironment("ASPNETCORE_ENVIRONMENT", App.AppEnvironmentName);

            ApplicationFunction.AddEnvironment("AWS_KMS_MASTERKEY_ARN", App.ApplicationMasterKey.KeyArn);

            AddApplicationConfiguration("Authentication:Cookie:Domain", App.Domain);

            AddApplicationConfiguration("Logging:EventBus:Source", Name);

            AddApplicationConfiguration("Aws:Region", Region);

            if (ApplicationBucket != null)
                AddApplicationConfiguration("Blob:S3:Bucket", ApplicationBucket.BucketName);

            if (HasFileSystem())
                FileSystem.Connections.AllowDefaultPortFrom(ApplicationFunction, description: "Connection from " + Name);

            if (TempApplicationBucket != null)
                AddApplicationConfiguration("Blob:S3:TempBucket", TempApplicationBucket.BucketName);

            if (ApplicationSecrets != null)
            {
                AddApplicationConfiguration("Aws:Secrets:Id", ApplicationSecrets.Name,true);
                AddApplicationConfiguration("Aws:Secrets:Region", Region);
            }

            return this;
        }

        Amazon.CDK.AWS.Lambda.FileSystem GetLambdaFunctionFileSystem()
        {
            if (!HasFileSystem())
                return null;

            return new Amazon.CDK.AWS.Lambda.FileSystem(new Amazon.CDK.AWS.Lambda.FileSystemConfig
            {
                Arn = FileSystemAccessPoint.AccessPointArn,
                LocalMountPath = GetFileSystemAccessPointPath().ToLower(),
                Dependency = new[] { FileSystemAccessPoint }
            });
        }

        internal void ClaimReadWrite(NamedBucket bucket)
        {
            var bucketArn = bucket.GetArn();

            RuntimeRole.AddToPolicy(PolicyStatementProvider.CreateAllowStatement(new[]
            {
                bucketArn,
                bucketArn + "/*"
            }, Action.S3.ReadWrite));
        }

        void WithApiGateway()
        {
            var api = new apigatewayV2.HttpApi(this, ToFullStackResourceName() + "HttpApi",
                new apigatewayV2.HttpApiProps
                {
                    ApiName = ToFullStackResourceName()
                });

            var apiIntegration = new HttpLambdaIntegration(ToFullStackResourceName() + "HttpApiIntegration", ApplicationFunction, new HttpLambdaIntegrationProps());

            apiIntegration.Bind(new apigatewayV2.HttpRouteIntegrationBindOptions
            {
                Route = new apigatewayV2.HttpRoute(
                    this,
                    Name + "HttpApiRoute",
                    new apigatewayV2.HttpRouteProps
                    {
                        RouteKey = apigatewayV2.HttpRouteKey.DEFAULT,
                        HttpApi = api,
                        Integration = apiIntegration
                    }),
                Scope = this
            });

            var domainName = new apigatewayV2.DomainName(this, Name + "ApiDomain", new apigatewayV2.DomainNameProps
            {
                DomainName = GetDomainName().ToLower(),
                Certificate = App.Certificate
            });

            new apigatewayV2.ApiMapping(this, Name + "ApiMapping", new apigatewayV2.ApiMappingProps
            {
                Api = api,
                DomainName = domainName
            });

            new route53.ARecord(this, Name + "RecordSet", new route53.ARecordProps
            {
                RecordName = GetDomainName().ToLower(),
                Ttl = Duration.Minutes(5),
                Target = new route53.RecordTarget(aliasTarget: new route53.Targets.ApiGatewayv2DomainProperties(domainName.RegionalDomainName, domainName.RegionalHostedZoneId)),
                Zone = App.HostedZone
            });
        }

        public string GetDomainName() => Subdomain.Or(Name) + "." + App.Domain;

        public CommandQueue CreateCommandQueue(string name, bool isFifo = true, double? visibilityTimeout = null)
        {
            var result = SqsFactory.CreateCommand(this, name, isFifo: isFifo, visibilityTimeout ?? ApplicationFunction.Timeout);
            ApplicationFunction.HandleEvent(result, EnableSqsEventSources, SqsEventSourcesBatchSize);
            result.ConfigureConsumeMessages(this);
            result.GrantConsumeMessages(this);

            return result;
        }

        public NamedQueue SubscribeTo(ServiceStack publisher)
        {
            var result = SqsFactory.CreateFIFO(this, QueueNameFormatting.FifoPull(publisher, this), visibility: ApplicationFunction.Timeout);
            ApplicationFunction.HandleEvent(result, EnableSqsEventSources, SqsEventSourcesBatchSize);

            result.GrantConsumeMessages(this);
            var configKey = ConfigurationNameFormatting.GetQueueConfigKey(publisher, this);
            var queueUrl = result.GetUrl();
            AddApplicationConfiguration(configKey, queueUrl);
            publisher.AddApplicationConfiguration(configKey, queueUrl);

            return result;
        }

        protected virtual bool EnableSqsEventSources => false;
        protected virtual int SqsEventSourcesBatchSize => 1;

        public void AddApplicationConfiguration(string key, string value, bool forceEnvironment=false)
        {
            if (StoreConfigurationsInSystemsParameters()&&!forceEnvironment)
                GetConfigurationContainer(key).Add(key.Split(":").Last(), value);
            else
                ApplicationFunction.AddApplicationConfig(key, value);
        }

        Dictionary<string, object> GetConfigurationContainer(string key)
        {
            var result = ConfigurationsParameterStoreValues;
            var levels = key.Split(":").ToArray();
            levels.Do((level, index) =>
            {
                var hasMoreDepth = index < levels.Count() - 1;
                if (!result.ContainsKey(level))
                {
                    if (hasMoreDepth)
                    {
                        var container = new Dictionary<string, object>();
                        result.Add(level, container);
                        result = container;
                    }
                }
                else
                {
                    var resultDictionary = result[level] as Dictionary<string, object>;
                    if (hasMoreDepth && (resultDictionary == null))
                    {
                        throw new System.Exception(result[level] + " is not a container and cannot accept deeper level of configurations");
                    }
                    result = resultDictionary;
                }
            });


            return result ?? throw new System.Exception("Could not find the contianer for " + key);
        }

        public class ServiceStackProps : StackProps
        {
            public string Subdomain { get; set; }
            public string AssetDirectory { get; set; }
        }
    }
}