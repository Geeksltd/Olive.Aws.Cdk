using Amazon.CDK.AWS.SSM;
using Constructs;

namespace Olive.Aws.Cdk
{
    public class NamedStringParameter : StringParameter
    {
        public string Name { get; }
        public NamedStringParameter(Construct scope, string id, IStringParameterProps props) : base(scope, id, props)
        {
            Name = props?.ParameterName;
        }

    }
}