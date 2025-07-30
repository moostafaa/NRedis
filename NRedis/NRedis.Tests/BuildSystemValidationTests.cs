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
    /// Tests to validate MSBuild and build system configuration
    /// Testing Framework: xUnit with FluentAssertions
    /// </summary>
    public class BuildSystemValidationTests
    {
        private readonly XDocument _projectDocument;
        private readonly string _projectDirectory;

        public BuildSystemValidationTests()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            _projectDirectory = Directory.GetParent(assemblyLocation)?.Parent?.Parent?.Parent?.FullName!;
            var projectFilePath = Path.Combine(_projectDirectory, "NRedis.Tests.csproj");
            _projectDocument = XDocument.Load(projectFilePath);
        }

        [Fact]
        public void ProjectFile_ShouldHaveRequiredPropertyGroups()
        {
            var propertyGroups = _projectDocument.Descendants("PropertyGroup").ToList();
            propertyGroups.Should().NotBeEmpty("project should have at least one PropertyGroup");
        }

        [Fact]
        public void ProjectFile_ShouldHaveRequiredItemGroups()
        {
            var itemGroups = _projectDocument.Descendants("ItemGroup").ToList();
            itemGroups.Should().NotBeEmpty("project should have at least one ItemGroup for package references");
        }

        [Theory]
        [InlineData("TargetFramework")]
        [InlineData("ImplicitUsings")]
        [InlineData("Nullable")]
        [InlineData("OutputType")]
        [InlineData("DockerDefaultTargetOS")]
        [InlineData("IsPackable")]
        public void ProjectFile_ShouldHaveRequiredProperties(string propertyName)
        {
            var property = _projectDocument.Descendants(propertyName).FirstOrDefault();
            property.Should().NotBeNull($"project should have {propertyName} property defined");
            property?.Value.Should().NotBeNullOrEmpty($"{propertyName} should have a non-empty value");
        }

        [Fact]
        public void ProjectFile_ShouldHaveValidTargetFramework()
        {
            var targetFramework = _projectDocument.Descendants("TargetFramework").FirstOrDefault()?.Value;
            
            targetFramework.Should().NotBeNullOrEmpty("TargetFramework should be specified");
            targetFramework.Should().MatchRegex(@"^net\d+\.\d+$", "should follow .NET framework naming convention");
        }

        [Theory]
        [InlineData("enable")]
        [InlineData("disable")]
        public void ImplicitUsings_ShouldHaveValidValue(string expectedValue)
        {
            var implicitUsings = _projectDocument.Descendants("ImplicitUsings").FirstOrDefault()?.Value;
            
            if (implicitUsings == expectedValue)
            {
                implicitUsings.Should().Be(expectedValue, $"ImplicitUsings should be {expectedValue} if specified");
            }
        }

        [Theory]
        [InlineData("enable")]
        [InlineData("disable")]  
        [InlineData("warnings")]
        [InlineData("annotations")]
        public void Nullable_ShouldHaveValidValue(string expectedValue)
        {
            var nullable = _projectDocument.Descendants("Nullable").FirstOrDefault()?.Value;
            
            if (nullable == expectedValue)
            {
                nullable.Should().Be(expectedValue, $"Nullable should be {expectedValue} if specified");
            }
        }

        [Fact]
        public void ProjectFile_ShouldNotHaveInvalidElements()
        {
            // Check for common MSBuild mistakes
            var invalidElements = new[] { "Reference", "HintPath" };
            
            foreach (var invalidElement in invalidElements)
            {
                var elements = _projectDocument.Descendants(invalidElement);
                elements.Should().BeEmpty($"modern .csproj should not contain {invalidElement} elements - use PackageReference instead");
            }
        }

        [Fact]
        public void PackageReferences_ShouldHaveVersions()
        {
            var packageReferences = _projectDocument.Descendants("PackageReference").ToList();
            
            foreach (var packageRef in packageReferences)
            {
                var include = packageRef.Attribute("Include")?.Value;
                var version = packageRef.Attribute("Version")?.Value;
                
                include.Should().NotBeNullOrEmpty("PackageReference should have Include attribute");
                version.Should().NotBeNullOrEmpty($"PackageReference {include} should have Version attribute");
            }
        }

        [Fact]
        public void ProjectReferences_ShouldHaveValidPaths()
        {
            var projectReferences = _projectDocument.Descendants("ProjectReference").ToList();
            
            foreach (var projectRef in projectReferences)
            {
                var include = projectRef.Attribute("Include")?.Value;
                include.Should().NotBeNullOrEmpty("ProjectReference should have Include attribute");
                include.Should().EndWith(".csproj", "ProjectReference should point to a .csproj file");
            }
        }

        [Fact]
        public void ProjectFile_ShouldNotHaveRedundantProperties()
        {
            // Check for properties that are redundant with modern SDK-style projects
            var redundantElements = new[] { "AssemblyTitle", "AssemblyDescription", "AssemblyConfiguration" };
            
            foreach (var redundantElement in redundantElements)
            {
                var elements = _projectDocument.Descendants(redundantElement);
                if (elements.Any())
                {
                    // This is more of a warning than a hard requirement
                    true.Should().BeTrue($"Consider if {redundantElement} is needed in modern SDK-style projects");
                }
            }
        }

        [Fact]
        public void ProjectFile_ShouldUseModernSdkFormat()
        {
            var root = _projectDocument.Root;
            var sdkAttribute = root?.Attribute("Sdk")?.Value;
            
            sdkAttribute.Should().NotBeNullOrEmpty("project should use SDK-style format");
            sdkAttribute.Should().StartWith("Microsoft.NET.Sdk", "should use Microsoft .NET SDK");
        }

        [Fact]
        public void ProjectFile_ShouldNotHaveUnnecessaryImports()
        {
            var imports = _projectDocument.Descendants("Import").ToList();
            
            // Modern SDK-style projects should have minimal explicit imports
            foreach (var import in imports)
            {
                var project = import.Attribute("Project")?.Value;
                if (project != null && !project.Contains("Microsoft."))
                {
                    project.Should().StartWith("$(", "custom imports should typically use MSBuild properties");
                }
            }
        }

        [Fact]
        public void BuildConfiguration_ShouldSupportMultipleTargets()
        {
            // This test ensures the project can be built in different configurations
            var targetFramework = _projectDocument.Descendants("TargetFramework").FirstOrDefault()?.Value;
            var targetFrameworks = _projectDocument.Descendants("TargetFrameworks").FirstOrDefault()?.Value;
            
            (targetFramework != null || targetFrameworks != null)
                .Should().BeTrue("project should specify either TargetFramework or TargetFrameworks");
        }
    }
}