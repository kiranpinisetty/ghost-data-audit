#!/usr/bin/env python3
"""
Ghost Data Still Exists - Entry Point Runner
This script provides the standard python3 entry point that invokes
the GhostDataAudit.Api .NET solution to audit deletion requests
and generate output.csv at the repository root.
"""

import os
import sys
import subprocess
from pathlib import Path

def main():
    repo_root = Path(__file__).resolve().parent.parent
    project_path = repo_root / "code" / "GhostDataAudit.Api"

    print("[GhostDataAudit] Launching ASP.NET Core Deletion Audit Engine from Python runner...")
    env = os.environ.copy()
    if "DOTNET_CLI_HOME" not in env:
        env["DOTNET_CLI_HOME"] = str(Path.home())
    if "HOME" not in env:
        env["HOME"] = str(Path.home())

    cmd = [
        "dotnet",
        "run",
        "--project",
        str(project_path),
        "--",
        "--generate-output"
    ]
    try:
        result = subprocess.run(cmd, cwd=repo_root, env=env, check=True)
        sys.exit(result.returncode)
    except subprocess.CalledProcessError as e:
        print(f"[GhostDataAudit] Error executing .NET engine: {e}")
        sys.exit(e.returncode)
    except FileNotFoundError:
        print("[GhostDataAudit] dotnet CLI not found in PATH. Please install .NET 10.0 SDK.")
        sys.exit(1)

if __name__ == "__main__":
    main()

