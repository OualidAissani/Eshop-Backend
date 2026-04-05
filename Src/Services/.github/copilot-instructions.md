# Copilot Instructions

## Project Guidelines
- User wants test guidance to stay strictly with Imposter syntax and not switch to NSubstitute-style verification APIs.
- User requires test fixes to stay strictly on Imposter APIs and avoid switching to non-Imposter mocking style.
- When reviewing Imposter-based tests, treat open generic imposter generation (e.g., GenerateImposter(typeof(ILogger<>))) as valid unless project-specific evidence shows otherwise.
