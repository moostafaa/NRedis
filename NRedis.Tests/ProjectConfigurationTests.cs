using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Xunit;
using FluentAssertions;

namespace NRedis.Tests
{
    /// <summary>
    /// Tests to validate the project configuration and structure
    /// Testing Framework: xUnit with FluentAssertions for better readability
    /// </summary>
    public class ProjectConfigurationTests
    {
        private readonly string _projectFilePath;
        private readonly XDocument _projectDocument;

        public ProjectConfigurationTests()
        {
            // Get the path to the current test project file
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var projectDirectory = Directory.GetParent(assemblyLocation)?.Parent?.Parent?.Parent?.FullName;
            _projectFilePath = Path.Combine(projectDirectory!, "NRedis.Tests.csproj");
            _projectDocument = XDocument.Load(_projectFilePath);
        }

        [Fact]
        public void ProjectFile_ShouldExist()
        {
            File.Exists(_projectFilePath).Should().BeTrue("the project file should exist");
        }

        [Fact]
        public void ProjectFile_ShouldTargetCorrectFramework()
        {
            var targetFramework = _projectDocument
                .Descendants("TargetFramework")
                .FirstOrDefault()?.Value;

            targetFramework.Should().Be("net9.0", "project should target .NET 9.0");
        }

        [Fact]
        public void ProjectFile_ShouldHaveLibraryOutputType()
        {
            var outputType = _projectDocument
                .Descendants("OutputType")
                .FirstOrDefault()?.Value;

            outputType.Should().Be("Library", "test projects should output libraries, not executables");
        }

        [Fact]
        public void ProjectFile_ShouldHaveNullableEnabled()
        {
            var nullable = _projectDocument
                .Descendants("Nullable")
                .FirstOrDefault()?.Value;

            nullable.Should().Be("enable", "nullable reference types should be enabled");
        }

        [Fact]
        public void ProjectFile_ShouldHaveImplicitUsingsEnabled()
        {
            var implicitUsings = _projectDocument
                .Descendants("ImplicitUsings")
                .FirstOrDefault()?.Value;

            implicitUsings.Should().Be("enable", "implicit usings should be enabled for cleaner code");
        }

        [Fact]
        public void ProjectFile_ShouldNotBePackable()
        {
            var isPackable = _projectDocument
                .Descendants("IsPackable")
                .FirstOrDefault()?.Value;

            isPackable.Should().Be("false", "test projects should not be packable");
        }

        [Fact]
        public void ProjectFile_ShouldHaveDockerTargetOS()
        {
            var dockerTargetOS = _projectDocument
                .Descendants("DockerDefaultTargetOS")
                .FirstOrDefault()?.Value;

            dockerTargetOS.Should().Be("Linux", "Docker target OS should be Linux");
        }

        [Fact]
        public void ProjectFile_ShouldIncludeTestingFrameworkPackages()
        {
            var packageReferences = _projectDocument
                .Descendants("PackageReference")
                .Select(pr => pr.Attribute("Include")?.Value)
                .Where(include => include != null)
                .ToList();

            packageReferences.Should().Contain("Microsoft.NET.Test.Sdk", "should include Test SDK");
            packageReferences.Should().Contain("xunit", "should include xUnit framework");
            packageReferences.Should().Contain("xunit.runner.visualstudio", "should include xUnit Visual Studio runner");
        }

        [Fact]
        public void ProjectFile_ShouldIncludeMockingFramework()
        {
            var packageReferences = _projectDocument
                .Descendants("PackageReference")
                .Select(pr => pr.Attribute("Include")?.Value)
                .Where(include => include != null)
                .ToList();

            packageReferences.Should().Contain("Moq", "should include Moq for mocking dependencies");
        }

        [Fact]
        public void ProjectFile_ShouldIncludeFluentAssertions()
        {
            var packageReferences = _projectDocument
                .Descendants("PackageReference")
                .Select(pr => pr.Attribute("Include")?.Value)
                .Where(include => include != null)
                .ToList();

            packageReferences.Should().Contain("FluentAssertions", "should include FluentAssertions for better test readability");
        }

        [Fact]
        public void ProjectFile_ShouldIncludeLoggingAbstractions()
        {
            var packageReferences = _projectDocument
                .Descendants("PackageReference")
                .Select(pr => pr.Attribute("Include")?.Value)
                .Where(include => include != null)
                .ToList();

            packageReferences.Should().Contain("Microsoft.Extensions.Logging.Abstractions", 
                "should include logging abstractions for testing logging scenarios");
        }

        [Fact]
        public void ProjectFile_ShouldHaveProjectReferences()
        {
            var projectReferences = _projectDocument
                .Descendants("ProjectReference")
                .Select(pr => pr.Attribute("Include")?.Value)
                .Where(include => include != null)
                .ToList();

            projectReferences.Should().NotBeEmpty("should have at least one project reference to the main projects");
            projectReferences.Should().Contain(reference => 
                reference.Contains("NRedis.Core.csproj"), "should reference the NRedis.Core project");
            projectReferences.Should().Contain(reference => 
                reference.Contains("NRedis.Server.csproj"), "should reference the NRedis.Server project");
        }

        [Theory]
        [InlineData("Microsoft.NET.Test.Sdk")]
        [InlineData("xunit")]
        [InlineData("xunit.runner.visualstudio")]
        [InlineData("Moq")]
        [InlineData("FluentAssertions")]
        public void ProjectFile_PackageReference_ShouldHaveVersion(string packageName)
        {
            var packageReference = _projectDocument
                .Descendants("PackageReference")
                .FirstOrDefault(pr => pr.Attribute("Include")?.Value == packageName);

            packageReference.Should().NotBeNull($"package {packageName} should be referenced");
            
            var version = packageReference?.Attribute("Version")?.Value;
            version.Should().NotBeNullOrEmpty($"package {packageName} should have a version specified");
        }

        [Fact]
        public void ProjectFile_XUnitRunner_ShouldHaveIncludeAssetsConfiguration()
        {
            var xunitRunnerPackage = _projectDocument
                .Descendants("PackageReference")
                .FirstOrDefault(pr => pr.Attribute("Include")?.Value == "xunit.runner.visualstudio");

            xunitRunnerPackage.Should().NotBeNull("xunit.runner.visualstudio package should exist");

            var includeAssets = xunitRunnerPackage?
                .Descendants("IncludeAssets")
                .FirstOrDefault()?.Value;

            includeAssets.Should().NotBeNullOrEmpty("xunit.runner.visualstudio should have IncludeAssets configured");
            includeAssets.Should().Contain("runtime", "should include runtime assets");
            includeAssets.Should().Contain("build", "should include build assets");
        }

        [Fact]
        public void ProjectFile_ShouldBeValidXml()
        {
            // This test passes if the constructor doesn't throw, meaning XML is valid
            _projectDocument.Should().NotBeNull("project file should be valid XML");
            _projectDocument.Root.Should().NotBeNull("project file should have a root element");
            _projectDocument.Root?.Name.LocalName.Should().Be("Project", "root element should be Project");
        }

        [Fact]
        public void ProjectFile_ShouldUseMicrosoftNETSdk()
        {
            var sdkAttribute = _projectDocument.Root?.Attribute("Sdk")?.Value;
            sdkAttribute.Should().Be("Microsoft.NET.Sdk", "should use Microsoft.NET.Sdk");
        }

        [Fact]
        public void Assembly_ShouldHaveCorrectTargetFramework()
        {
            var targetFrameworkAttribute = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();

            targetFrameworkAttribute.Should().NotBeNull("assembly should have target framework attribute");
            targetFrameworkAttribute?.FrameworkName.Should().StartWith(".NETCoreApp,Version=v9.0", 
                "assembly should target .NET 9.0");
        }

        [Fact]
        public void TestAssembly_ShouldBeInCorrectNamespace()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            assemblyName.Should().Be("NRedis.Tests", "test assembly should have correct name");
        }

        [Fact]
        public void TestProject_ShouldHaveTestInName()
        {
            var projectName = Path.GetFileNameWithoutExtension(_projectFilePath);
            projectName.Should().EndWith("Tests", "test project should have 'Tests' suffix");
        }

        [Theory]
        [InlineData("17.8.0", "Microsoft.NET.Test.Sdk")]
        [InlineData("2.6.2", "xunit")]
        [InlineData("2.5.3", "xunit.runner.visualstudio")]
        [InlineData("8.0.0", "Microsoft.Extensions.Logging.Abstractions")]
        [InlineData("4.20.69", "Moq")]
        [InlineData("6.12.0", "FluentAssertions")]
        public void ProjectFile_ShouldHaveExpectedPackageVersions(string expectedVersion, string packageName)
        {
            var packageReference = _projectDocument
                .Descendants("PackageReference")
                .FirstOrDefault(pr => pr.Attribute("Include")?.Value == packageName);

            packageReference.Should().NotBeNull($"package {packageName} should be referenced");
            
            var actualVersion = packageReference?.Attribute("Version")?.Value;
            actualVersion.Should().Be(expectedVersion, $"package {packageName} should have expected version");
        }
    }
}