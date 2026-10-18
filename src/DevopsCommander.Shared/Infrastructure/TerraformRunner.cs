using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevopsCommander.Shared.Interfaces;
using DevopsCommander.Shared.Models;

namespace DevopsCommander.Shared.Infrastructure
{
    /// <summary>
    /// Executes Terraform provisioning as an isolated child process,
    /// generating tfvars, running init and apply, and streaming stdout/stderr as logs.
    /// </summary>
    public class TerraformRunner : ITerraformRunner
    {
        private readonly string _templatesRoot;
        private readonly string _workspaceRoot;
        private readonly string _terraformExe;

        public TerraformRunner(string? templatesRoot = null, string? workspaceRoot = null, string? terraformExe = null)
        {
            _templatesRoot = templatesRoot ?? FindTemplatesRoot();
            _workspaceRoot = workspaceRoot ?? Path.Combine(Path.GetTempPath(), "DevopsCommander_Runs");
            _terraformExe = terraformExe ?? FindTerraformExecutable();
        }

        private static string FindTemplatesRoot()
        {
            // Search upwards from BaseDirectory and CurrentDirectory
            var searchOrigins = new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() };
            foreach (var origin in searchOrigins)
            {
                var current = origin;
                while (!string.IsNullOrEmpty(current))
                {
                    var candidate = Path.Combine(current, "templates", "terraform");
                    if (Directory.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }

                    var parent = Directory.GetParent(current);
                    if (parent == null) break;
                    current = parent.FullName;
                }
            }

            return Path.Combine(Directory.GetCurrentDirectory(), "templates", "terraform");
        }

        private static string FindTerraformExecutable()
        {
            var envPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in envPath.Split(Path.PathSeparator))
            {
                try
                {
                    var exe = Path.Combine(dir.Trim(), "terraform.exe");
                    if (File.Exists(exe)) return exe;

                    var bin = Path.Combine(dir.Trim(), "terraform");
                    if (File.Exists(bin)) return bin;
                }
                catch
                {
                    // Ignore invalid path segments
                }
            }

            return "terraform";
        }

        public async Task<Result<bool>> ExecuteAsync(
            DeploymentState state,
            Func<string, string, Task> onLogAsync,
            CancellationToken cancellationToken = default)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (onLogAsync == null) throw new ArgumentNullException(nameof(onLogAsync));

            var infraType = state.Request.InfrastructureType.ToLowerInvariant();
            if (infraType == "virtualmachine") infraType = "vm";

            var templateDir = Path.Combine(_templatesRoot, infraType);
            if (!Directory.Exists(templateDir))
            {
                var msg = $"Terraform template directory not found for infrastructure type '{infraType}' at '{templateDir}'.";
                await onLogAsync("Validation", msg);
                return Result<bool>.Failure(msg);
            }

            var runDir = Path.Combine(_workspaceRoot, state.Id.ToString("N"));
            Directory.CreateDirectory(runDir);

            try
            {
                await onLogAsync("Preparation", $"Setting up isolated workspace at '{runDir}'.");

                // 1. Copy template files (*.tf) to run directory
                foreach (var file in Directory.GetFiles(templateDir, "*.tf"))
                {
                    var destFile = Path.Combine(runDir, Path.GetFileName(file));
                    File.Copy(file, destFile, overwrite: true);
                }

                // 2. Generate terraform.tfvars.json
                var tfVars = new Dictionary<string, object>
                {
                    ["project_name"] = state.Request.ProjectName,
                    ["environment"] = state.Request.Environment.ToLowerInvariant(),
                    ["region"] = state.Request.Region
                };

                foreach (var (k, v) in state.Request.Parameters)
                {
                    tfVars[k] = v;
                }

                var tfVarsJson = JsonSerializer.Serialize(tfVars, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(runDir, "terraform.tfvars.json"), tfVarsJson, cancellationToken);
                await onLogAsync("Preparation", "Generated terraform.tfvars.json with deployment parameters.");

                // 3. Run 'terraform init -no-color'
                await onLogAsync("TerraformInit", "Executing 'terraform init'...");
                var initResult = await RunProcessAsync(_terraformExe, "init -no-color", runDir, "TerraformInit", onLogAsync, cancellationToken);
                if (initResult.ExitCode != 0)
                {
                    var err = $"Terraform init failed with exit code {initResult.ExitCode}. Details: {initResult.StdErr}";
                    await onLogAsync("TerraformInit", err);
                    return Result<bool>.Failure(err);
                }

                // 4. Run 'terraform apply -auto-approve -no-color'
                await onLogAsync("TerraformApply", "Executing 'terraform apply -auto-approve'...");
                var applyResult = await RunProcessAsync(_terraformExe, "apply -auto-approve -no-color", runDir, "TerraformApply", onLogAsync, cancellationToken);
                if (applyResult.ExitCode != 0)
                {
                    var err = $"Terraform apply failed with exit code {applyResult.ExitCode}. Details: {applyResult.StdErr}";
                    await onLogAsync("TerraformApply", err);
                    return Result<bool>.Failure(err);
                }

                await onLogAsync("TerraformApply", "Infrastructure provisioning completed successfully.");
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                var err = $"Terraform execution error: {ex.Message}";
                await onLogAsync("ExecutionError", err);
                return Result<bool>.Failure(err);
            }
            finally
            {
                // Optional cleanup of workspace directory if desired
            }
        }

        private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(
            string executable,
            string arguments,
            string workingDirectory,
            string phase,
            Func<string, string, Task> onLogAsync,
            CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };

            var stdOutLines = new List<string>();
            var stdErrLines = new List<string>();

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    lock (stdOutLines) stdOutLines.Add(e.Data);
                    _ = onLogAsync(phase, e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    lock (stdErrLines) stdErrLines.Add(e.Data);
                    _ = onLogAsync(phase, e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            return (process.ExitCode, string.Join(Environment.NewLine, stdOutLines), string.Join(Environment.NewLine, stdErrLines));
        }
    }
}
