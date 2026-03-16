# Contributing

Thank you for your interest in contributing to this project! This document provides guidelines for contributing.

## Important Note

**This project accepts contributions selectively.** Before investing time in code changes, please:

1. **Open an issue first** to discuss your proposed changes
2. **Wait for approval** from maintainers before starting work
3. **Link your PR to the approved issue**

Unsolicited pull requests without prior discussion may be closed.

---

## Getting Started

### Prerequisites

Before you begin, ensure you have the following installed:

- **.NET SDK** (latest LTS version)
  - Download from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
- **Node.js and npm** (latest LTS version)
  - Download from [https://nodejs.org](https://nodejs.org)
- **Git** for version control

### Setting Up Your Development Environment

1. **Fork and clone the repository**

   ```bash
   git clone https://github.com/YOUR-USERNAME/REPO-NAME.git
   cd REPO-NAME
   ```

2. **Restore .NET dependencies**

   ```bash
   dotnet restore
   ```

3. **Install npm packages (if applicable)**

   ```bash
   npm install
   ```

4. **Build the project**

   ```bash
   dotnet build
   ```

5. **Run tests to verify setup**

   ```bash
   dotnet test
   ```

---

## Development Workflow

### 1. Create an Issue

Before making any changes:

- Search existing issues to avoid duplicates
- Open a new issue describing:
  - The problem you're solving or feature you're adding
  - Your proposed approach
  - Any breaking changes

### 2. Wait for Approval

- A maintainer will review and provide feedback
- Once approved, you can start working on the changes
- The issue will be assigned to you

### 3. Make Your Changes

1. **Create a feature branch**

   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Write clean, maintainable code**
   - Follow the code style guidelines (see below)
   - Add tests for new functionality
   - Update documentation as needed

3. **Commit your changes**

   ```bash
   git add .
   git commit -m "Brief description of changes"
   ```

### 4. Test Your Changes

Before submitting:

```bash
# Run all tests
dotnet test

# Build the project
dotnet build

# Run linters (if configured)
dotnet format --verify-no-changes
```

### 5. Submit a Pull Request

1. **Push your branch**

   ```bash
   git push origin feature/your-feature-name
   ```

2. **Open a pull request**
   - Use the PR template
   - Link to the approved issue
   - Provide clear description of changes
   - Include screenshots/examples if relevant

---

## Code Style Guidelines

### C# / .NET

- Follow [Microsoft's C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use **PascalCase** for public members
- Use **camelCase** for private fields (prefix with `_`)
- Prefer **explicit types** over `var` when type isn't obvious
- Write **XML documentation** for public APIs
- Use **async/await** for asynchronous operations
- Keep methods **small and focused** (single responsibility)

### TypeScript

- Follow **ESLint** rules (if configured)
- Use **camelCase** for variables and functions
- Use **PascalCase** for classes and interfaces
- Prefer **interfaces** over type aliases for object shapes
- Use **async/await** over promises when possible
- Enable **strict mode** in `tsconfig.json`
- Write **JSDoc comments** for exported functions

### General

- Keep lines under **120 characters** when possible
- Use **meaningful variable names**
- Write **self-documenting code** (comments explain "why", not "what")
- Remove **commented-out code** before committing
- No **console.log** or **Debug.WriteLine** in production code

---

## Testing

- Write **unit tests** for new functionality
- Maintain or improve **code coverage**
- Test **edge cases** and error conditions
- Use **descriptive test names** (e.g., `Should_ReturnNull_When_InputIsEmpty`)

---

## Documentation

Update documentation when you:

- Add new features
- Change existing behavior
- Modify public APIs
- Add configuration options

---

## Pull Request Guidelines

- **One feature per PR** - keep changes focused
- **Link to the approved issue**
- **Provide clear descriptions**
- **Include tests** for new functionality
- **Update documentation** as needed
- **Respond to feedback** promptly
- **Squash commits** if requested

---

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

---

## Questions?

If you have questions about contributing, please open an issue for discussion.

Thank you for contributing! 🎉
