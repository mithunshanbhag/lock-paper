---
name: local-dev-script
description: Guidelines and best practices for local development script.
---

- A `run-local.ps1` PowerShell script should be created in the root folder of this workspace. This script will be used to run the application and tests locally.

- The script will take in a parameter `target` which can have the following values:
  - `app`: This will run the application locally.
  - `tests`: This will run all tests locally.
  - `unit-tests`: This will run only unit tests locally.
  - `e2e-tests`: This will run only E2E tests locally.

- The `run-local.ps1` script should be idempotent, meaning it can be run multiple times without causing issues or requiring manual cleanup.
