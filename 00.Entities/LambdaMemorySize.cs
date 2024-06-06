namespace Olive.Aws.Cdk
{
    public enum LambdaMemorySize
    {
        Tiny = 1024 / 2,
        Small = 1024,
        Medium = 1024 * 2,
        Large = 1024 * 3
    }
}