# Gateway Documentation

## Overview

This directory contains comprehensive documentation for the Gateway project, a lightweight, high-performance Object Mapper for .NET that works with Couchbase using SQL++ (N1QL).

## Documentation Files

### [requirements.md](requirements.md)
Complete requirements specification for the Couchbase SimpleMapper library. This document defines all features, acceptance criteria, and technical specifications for the MVP (Minimum Viable Product).

**Contents:**
- Goals and non-goals
- Technical requirements
- Core features (Connection Management, Object Mapping, Query Execution, CRUD, Filters, Pagination, Performance, Error Handling)
- Detailed Gherkin scenarios for each requirement

### [ACCEPTANCE_TESTS_ROADMAP.md](ACCEPTANCE_TESTS_ROADMAP.md)
Strategic implementation roadmap for the 214 acceptance tests. This document provides a phased approach to implement all features and turn tests from Red (failing) to Green (passing).

**Contents:**
- Executive summary and current status
- Test coverage breakdown by category
- 4-phase implementation strategy (Foundation → Core → Advanced → Optimization)
- Detailed implementation order for each feature
- Progress tracking and milestones
- Implementation guidelines and best practices
- Success criteria and quality gates

## Quick Start

1. **Understand the Requirements**: Read `requirements.md` to understand what features need to be implemented
2. **Follow the Roadmap**: Use `ACCEPTANCE_TESTS_ROADMAP.md` to guide your implementation
3. **Run the Tests**: See `/tests/Gateway.AcceptanceTests/README.md` for test execution instructions
4. **Implement Features**: Follow the test-first approach outlined in the roadmap

## Additional Resources

- **Test Documentation**: `/tests/Gateway.AcceptanceTests/README.md`
- **Couchbase SDK**: [Official .NET SDK Documentation](https://docs.couchbase.com/dotnet-sdk/current/hello-world/start-using-sdk.html)
- **xUnit Framework**: [xUnit.net Documentation](https://xunit.net/)
- **FluentAssertions**: [FluentAssertions Documentation](https://fluentassertions.com/)

## Project Status

- ✅ Requirements defined (214 acceptance criteria)
- ✅ Acceptance tests written (214 tests)
- ✅ Implementation roadmap created
- 🔄 Implementation in progress (Phase 0)
- ⏳ Awaiting feature implementation