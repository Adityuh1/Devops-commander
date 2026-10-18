using System;
using System.Threading;
using System.Threading.Tasks;
using DevopsCommander.Shared.Models;

namespace DevopsCommander.Shared.Interfaces
{
    /// <summary>
    /// Abstraction for executing infrastructure provisioning with Terraform.
    /// Runs 'terraform init' and 'terraform apply', streaming logs back into the deployment state.
    /// </summary>
    public interface ITerraformRunner
    {
        Task<Result<bool>> ExecuteAsync(
            DeploymentState state,
            Func<string, string, Task> onLogAsync,
            CancellationToken cancellationToken = default);
    }
}
