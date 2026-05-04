---
name: diagnostics
description: Guidelines and best practices for performing diagnostics.
---

## Diagnostics Guidelines

- The application's logs are injected into Azure Application Insights. You can use the AZ CLI to query traces, exceptions, requests, metrics and other telemetry data.
- If you are unable to diagnose an issue due to lack of information in the logs, you can consider adding additional logging/telemetry to the application. Please follow the guidelines in the `/.github/skills/development/SKILL.md` file for this purpose.
- Miscellaneous: While using the AZ CLI to query Application Insights, you should use the `--offset` flag to specify the time range for your query.