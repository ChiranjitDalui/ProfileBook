
# `README_TESTING.md` 

# Testing — ProfileBook (Backend)



---

## Table of contents

* Prerequisites
* Project structure (important testing folders)
* Run unit tests
* Run integration tests
* Run all tests (solution)
* Collect code coverage and produce HTML report
* Common errors & fixes (you hit many of these)
* Tips for VSCode + Git / submission checklist
* Notes on improving coverage

---

## Prerequisites

* .NET SDK matching the project (you used .NET 9.0): ensure `dotnet --version` shows a 9.x SDK.
* PowerShell / Command Prompt access.
* (Optional) `reportgenerator` tool for HTML coverage: `dotnet tool install -g dotnet-reportgenerator-globaltool`.

---

## Project structure (relevant)

```
D:\ProfileBook
 ├─ ProfileBook.sln
 ├─ ProfileBookAPI/                     <-- backend API project
 ├─ ProfileBookAPI.Tests/               <-- integration tests (WebApplicationFactory)
 ├─ ProfileBookAPI.UnitTests/           <-- unit tests (xUnit + Moq)
 └─ profilebook-frontend/               <-- frontend (not needed for backend testing)
```

---

## Where to create tests / files

* Unit tests: `D:\ProfileBook\ProfileBookAPI.UnitTests\` (each test file is a `.cs` file within).
* Integration tests: `D:\ProfileBook\ProfileBookAPI.Tests\` (already present).
* Add README: `D:\ProfileBook\README_TESTING.md` (this file).
* Solution file (root): `D:\ProfileBook\ProfileBook.sln` — make sure your projects are added into it.

---

## Run unit tests (only)

Open PowerShell at repo root or navigate to the unit tests folder:

```powershell
cd D:\ProfileBook\ProfileBookAPI.UnitTests
dotnet test
```

You should see the unit test project build and run (count of tests, pass/fail).

---

## Run integration tests (only)

Integration tests use `WebApplicationFactory<Program>` (launch a test host):

```powershell
cd D:\ProfileBook\ProfileBookAPI.Tests
dotnet test
```

Watch console for logs from the application — important messages are shown there.

---

## Run all tests (solution)

From repo root run everything (unit + integration):

```powershell
cd D:\ProfileBook
dotnet test .\ProfileBook.sln
```

This runs all projects referenced in the solution.

---

## Collect code coverage (XPlat) and generate HTML

1. Add the coverlet collector to test projects if not already present:

```powershell
cd D:\ProfileBook
dotnet add ./ProfileBookAPI.UnitTests/ package coverlet.collector
dotnet add ./ProfileBookAPI.Tests/ package coverlet.collector
```

2. Run tests with the coverage collector:

```powershell
dotnet test .\ProfileBook.sln /p:CollectCoverage=true --collect:"XPlat Code Coverage"
```

3. Find the produced coverage file(s) under `TestResults\<run-guid>\coverage.cobertura.xml`.

4. (Optional) Convert to HTML:

```powershell
dotnet tool install -g dotnet-reportgenerator-globaltool    # only once
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html
```

Open `D:\ProfileBook\coverage-report\index.htm` in a browser to view the HTML report.

---

## Commit and CI tips (what to push)

* Commit the `ProfileBook.sln` with all projects added.
* Commit all test files in `ProfileBookAPI.UnitTests` and `ProfileBookAPI.Tests`.
* Commit `README_TESTING.md`.
* Optionally add `.github/workflows/ci.yml` to run tests and coverage on push (I can generate this if you want).

---

## Troubleshooting — common errors you hit (and fixes)

### 1) `The type or namespace name 'FluentAssertions' / 'Moq' could not be found`

**Cause:** NuGet packages not installed in the test project.
**Fix:**

```powershell
cd D:\ProfileBook\ProfileBookAPI.UnitTests
dotnet add package FluentAssertions
dotnet add package Moq
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Microsoft.AspNetCore.Mvc.Core
dotnet restore
```

Also ensure the unit-test project has a project reference to the API project:

```powershell
dotnet add reference ..\ProfileBookAPI\ProfileBookAPI.csproj
```

### 2) `Missing FileProvider` or `StaticFileMiddleware` errors when running integration tests

**Cause:** The test server's `IWebHostEnvironment` did not expose a file provider or webroot (this happens with WebApplicationFactory/Custom env).
**Fix:** In your test factory override, register a test `IWebHostEnvironment` with `ContentRootFileProvider` and `WebRootFileProvider` (PhysicalFileProvider for a temp folder). Example (inside the factory):

```csharp
Directory.CreateDirectory(TestWebRoot);
services.AddSingleton<IWebHostEnvironment>(sp => new TestWebHostEnvironment {
    EnvironmentName = Environments.Development,
    ContentRootPath = contentRoot,
    ContentRootFileProvider = new PhysicalFileProvider(contentRoot),
    WebRootPath = TestWebRoot,
    WebRootFileProvider = new PhysicalFileProvider(TestWebRoot)
});
```

(This is the pattern already used in the `IntegrationTestFactory` we adjusted earlier.)

### 3) `Only a single database provider can be registered`

**Cause:** Both `UseSqlServer` (default app) and `UseInMemoryDatabase` (test setup) were registered in DI at once. This causes EF Core to complain.
**Fix:** In the test factory remove the descriptors for `AppDbContext` / `DbContextOptions<T>` from `services` then add the `InMemory` registration. Example:

```csharp
var descriptorsToRemove = services.Where(d =>
    d.ServiceType == typeof(AppDbContext) ||
    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
    (d.ImplementationType != null && d.ImplementationType == typeof(AppDbContext))
).ToList();

foreach (var d in descriptorsToRemove) services.Remove(d);

services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(dbName));
```

### 4) `Program is inaccessible due to its protection level`

**Cause:** Using `WebApplicationFactory<Program>` requires `Program` class to be `public` or have the `public partial class Program{}` entry point pattern (the template `Program` is internal sometimes).
**Fix:** Ensure your `Program` class is accessible to tests — simplest is to use the top-level program pattern with `public partial class Program { }` in Program.cs, or add a `public partial class Program {}` file that declares it `public` (so `WebApplicationFactory<Program>` can use it).

### 5) `ISystemClock is obsolete` warnings

**Cause:** Auth test handler uses `ISystemClock` constructor overload that is marked obsolete. This is a warning and not fatal. You can keep using it in tests, or update to `TimeProvider` if required.

### 6) `async void` xUnit warnings (xUnit1048)

**Cause:** Tests declared `async void`.
**Fix:** Make test methods `async Task` and `await` asynchronous operations. Example:

```csharp
[Fact]
public async Task MyTestAsync() { await ...; }
```

---

## How to write more tests (quick patterns)

* Unit tests: instantiate controller with required dependencies (mock config, mock env) and call methods directly. Set `ControllerContext.HttpContext.User` to a `ClaimsPrincipal` if the controller reads claims.
* Integration tests: use `WebApplicationFactory<Program>` and `CreateClient()` to call real HTTP endpoints.
* For `IFormFile` fake uploads, use a `MemoryStream` and a `Mock<IFormFile>` with `OpenReadStream()` / `CopyTo(...)` set.

---

## Example test-running commands you will use daily

```powershell
# unit only
dotnet test .\ProfileBookAPI.UnitTests

# integration only
dotnet test .\ProfileBookAPI.Tests

# all tests
dotnet test .\ProfileBook.sln

# coverage for solution
dotnet test .\ProfileBook.sln /p:CollectCoverage=true --collect:"XPlat Code Coverage"
```

---


