# .NET Development Standards

## Instructions

- All C# code must adhere to the .NET coding conventions and style guidelines.
- Use `async` and `await` for asynchronous operations, avoiding `Task.Wait()` or `Task.Result` when possible.
- All public methods should include XML documentation comments describing their purpose, parameters, and return values.
- Dependency injection should be used for managing service dependencies, favoring constructor injection.
- Unit tests are required for all new features and bug fixes, with a minimum of 80% code coverage.
- Logging should utilize a structured logging framework (e.g., Serilog, NLog) and include relevant context.
- When interacting with AWS services, use the official AWS SDK for .NET and follow the principle of least privilege for IAM roles.
- Avoid hardcoding configuration values; instead, use configuration providers (e.g., `appsettings.json`, environment variables).

## Security Guidelines

- All sensitive data (e.g., connection strings, API keys) must be stored securely and not committed to source control.
- Input validation is required for all user-provided data to prevent common vulnerabilities like SQL injection and cross-site scripting (XSS).
- Use HTTPS for all external communication and ensure proper certificate validation.
- Implement appropriate error handling and logging for security-sensitive operations.