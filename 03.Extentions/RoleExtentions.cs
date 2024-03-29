﻿using Amazon.CDK.AWS.IAM;

namespace Olive.Aws.Cdk
{
    public static class RoleExtensions
    {
        public static void AddManagedPolicy(this Role @this, string policy)
        {
            @this.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName(policy));
        }
    }
}