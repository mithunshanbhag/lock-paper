---
name: test-automation
description: Guidelines and best practices for authoring automated tests (including unit tests and end-to-end tests).
---

## Unit Tests

- Unit tests should be categorized as positive, negative, boundary/edge cases. Other categories can also be added if needed. Each category should be in its own \#region, \#endregion block.
- For .NET source projects, you should ideally author unit tests using latest versions of XUnit, XUnit assertions, Moq and Bogus.
  - Some references for writing good unit tests in .NET:
    - [Unit testing best practices for .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

## End-to-End Tests

- Please author all end-to-end tests using Playwright (with .NET SDK) and XUnit (using the `Microsoft.Playwright.XUnit` nuget package).
- For details on getting started with Playwright using XUnit: [Playwright .NET SDK](https://playwright.dev/dotnet/docs/intro).
- Playwright best practices are [documented here](https://playwright.dev/docs/best-practices).
- For consistency, please use the same testing libraries and frameworks as mentioned in the Unit Tests section.
  - But, if possible, use Playwright's own assertion library for end-to-end tests over XUnit assertions.
- It is preferable to run Playwright in Headless mode, especially since these tests will be running in CI/CD pipelines too.
