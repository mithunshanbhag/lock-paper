# COPILOT INSTRUCTIONS

This is my opinionated checklist for building indie-SaaS, micro-SaaS apps. It is not exhaustive, but it covers some important aspects of my app building process. This is a living document and will be updated / tweaked as required.

## PREFERRED TECH STACK

My preferred framework for building apps is .NET (currently .NET 10 is the latest version):

- Frontend: Blazor WebAssembly (with MudBlazor controls).
- CommandLine: .NET Console App (with Spectre.Console for rich CLI).
- Backend: Azure Function Apps.
- Database: Azure Cosmos DB (NOSQL API, formerly known as Core SQL API).

I prefer to host my apps and related infra on Azure using serverless/PaaS. This is to keep things relatively simple, low maintenance and low cost.

## GROUND RULES

| Key files & folders     | Purpose                                                           |
| ----------------------- | ----------------------------------------------------------------- |
| `/.github/LEARNINGS.md` | All learnings & notes will be documented here by the agent (you). |
| `/docs/specs`           | All product specifications will live under this folder.           |
| `/docs/specs/README.md` | The main documentation file for the specifications.               |
| `/docs/specs/ui.md`     | Detailed specifications about the UI.                             |
| `/docs/ui-mockups`      | All UI mockups will live under this folder.                       |
| `/README.md`            | The main documentation file for the project.                      |
| `/run-local.ps1`        | A convenience PowerShell script to run the app and tests locally. |
| `/src`                  | All source code will live under this folder.                      |
| `/tests`                | All unit and E2E tests will live under this folder.               |

## GENERAL WORKFLOW

1. See if there are any prior learnings documented in `/.github/LEARNINGS.md` that can be helpful for the current task. Create this file if it doesn't exist.

2. Start with the specifications in the `/docs/specs` folder. This will give you a clear understanding of the requirements and features of the app.
   - Start with `/docs/specs/README.md`.

3. If explicitly asked, only then create (or update) UI mockups in the `/docs/ui-mockups` folder based on the specifications.
   - Always use any existing UI mock ups in the `/docs/ui-mockups` folder as general reference. This will help you visualize the app and its user interface.

4. Implement the features in the `/src` folder based on the specifications and UI mockups.

5. Write tests in the `/tests` folder to verify that the features work as expected.

6. Set up the local development script (if not already set up).

7. Update the documentation in the `README.md` file as needed to reflect the current state of the project.

8. Document any new, relevant learnings and notes by updating the `/.github/LEARNINGS.md` file.

9. Along with your actual output, please also provide a brief summary of the MCP servers utilized, skills utilized, hooks invoked, LSPs invoked. Use the following format:

  ```
  MCP Servers Utilized
  - MCP Server 1
  - MCP Server 2
  Skills Utilized
  - Skill 1
  - Skill 2
  Hooks Invoked
  - Hook 1
  LSPs Invoked
  - LSP 1
  ```

## UI MOCKUP GUIDELINES

- Each UI mockup should be in its own subfolder under `/docs/ui-mockups` with a descriptive name. For example:

  ```text
  /docs/ui-mockups
    /UserRegistrationForm
    /DashboardOverview
    /ProductDetailsPage
  ```

- Please follow the guidelines in the `/.github/skills/ui-mockup/SKILL.md` file when authoring UI mockups.

## DEVELOPMENT GUIDELINES

- Each source project will be in its own subfolder under `/src` with a descriptive name. For example:

  ```text
  /src
    /MyApp.Api
    /MyApp.Application
    /MyApp.Core
    /MyApp.Domain
    /MyApp.Infrastructure
  ```

- Ensure that the code is clean, well-structured, and follows best practices for the programming language and framework being used.

- If you encounter any ambiguities or have questions about the specifications, please ask for clarification before proceeding with the implementation.

- Please follow the guidelines in the `/.github/skills/development/SKILL.md` file when implementing the code, creating features or fixing bugs.

- Do not declare success until you've actually verified that the changes work. Verification can be done by:
  - Running the application and testing the feature visually. OR
  - Writing and running existing automated tests to ensure the feature works as expected.
  - If there are no existing automated tests, you can consider writing new tests to verify the feature. These tests should be added to the appropriate test project under the `/tests` folder.

## TESTING GUIDELINES

- There will be two types of tests: Unit Tests and End-to-End Tests.

- Each test project (in the `/tests` folder) will mirror a source project (in the `/src` folder). See examples below.

  ```text
  /src
    /MyApp.Api
    /MyApp.Application
    /MyApp.Domain
    /MyApp.Infrastructure

  /tests
    /MyApp.E2ETests
    /MyApp.Application.UnitTests
    /MyApp.Domain.UnitTests
    /MyApp.Infrastructure.IntegrationTests
    /MyApp.Infrastructure.UnitTests
  ```

- Please follow the guidelines in the `/.github/skills/test-automation/SKILL.md` file when authoring automated tests.

- Always ensure that the tests are building, running, and passing before declaring success.

## LOCAL DEVELOPMENT SCRIPT GUIDELINES

- Please follow the guidelines in the `/.github/skills/local-dev-script/SKILL.md` file when setting up the local development script.

- Always ensure that the local development script is working as expected before declaring success.

## DOCUMENTATION GUIDELINES

- Please follow the guidelines in the `/.github/skills/documentation/SKILL.md` file when authoring documentation.

## MISCELLANEOUS

- Sometimes, we just want to capture feature requests, bug reports, future enhancements, or other brainstorming/ideation notes without having any detailed detailed specifications attached to it. For this purpose, we can use the `docs/specs/future.md` file as a catch-all for any relevant information that may be useful for the project. This file needs to be created if it doesn't already exist.