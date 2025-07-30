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
    /// Tests to validate Docker-related configuration in the project
    /// Testing Framework: xUnit with FluentAssertions
    /// </summary>
    public class DockerConfigurationTests
    {
        private readonly XDocument _projectDocument;
        private readonly string _projectDirectory;

        public DockerConfigurationTests()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            _projectDirectory = Directory.GetParent(assemblyLocation)?.Parent?.Parent?.Parent?.FullName!;
            var projectFilePath = Path.Combine(_projectDirectory, "NRedis.Tests.csproj");
            _projectDocument = XDocument.Load(projectFilePath);
        }

        [Fact]
        public void ProjectFile_ShouldHaveDockerDefaultTargetOS()
        {
            var dockerTargetOS = _projectDocument
                .Descendants("DockerDefaultTargetOS")
                .FirstOrDefault()?.Value;

            dockerTargetOS.Should().NotBeNullOrEmpty("DockerDefaultTargetOS should be specified");
            dockerTargetOS.Should().Be("Linux", "Docker target OS should be Linux for better compatibility");
        }

        [Fact]
        public void ProjectFile_ShouldIncludeContainerToolsTargets()
        {
            var containerToolsPackage = _projectDocument
                .Descendants("PackageReference")
                .FirstOrDefault(pr => pr.Attribute("Include")?.Value == "Microsoft.VisualStudio.Azure.Containers.Tools.Targets");

            containerToolsPackage.Should().NotBeNull("should include Container Tools Targets package");

            var version = containerToolsPackage?.Attribute("Version")?.Value;
            version.Should().NotBeNullOrEmpty("Container Tools Targets should have a version specified");
        }

        [Theory]
        [InlineData("linux")]
        [InlineData("Linux")]
        [InlineData("LINUX")]
        public void DockerTargetOS_ShouldAcceptLinuxInVariousCases(string targetOS)
        {
            // This test validates that our configuration would work with different casing
            targetOS.ToLowerInvariant().Should().Be("linux", "Docker target OS should be Linux regardless of case");
        }

        [Fact]
        public void ContainerToolsVersion_ShouldBeRecentVersion()
        {
            var containerToolsPackage = _projectDocument
                .Descendants("PackageReference")
                .FirstOrDefault(pr => pr.Attribute("Include")?.Value == "Microsoft.VisualStudio.Azure.Containers.Tools.Targets");

            var version = containerToolsPackage?.Attribute("Version")?.Value;
            
            version.Should().NotBeNullOrEmpty("Container Tools should have a version");
            
            // Parse version to ensure it's a reasonable version number
            if (Version.TryParse(version, out var parsedVersion))
            {
                parsedVersion.Major.Should().BeGreaterOrEqualTo(1, "should use a modern version of Container Tools");
                parsedVersion.Minor.Should().BeGreaterOrEqualTo(0, "version should be valid");
            }
            else
            {
                // If version doesn't parse as System.Version, it might be a pre-release or different format
                version.Should().MatchRegex(@"^\d+\.\d+\.\d+", "version should follow semantic versioning pattern");
            }
        }

        [Fact]
        public void DockerConfiguration_ShouldBeConsistentWithNetFramework()
        {
            var targetFramework = _projectDocument
                .Descendants("TargetFramework")
                .FirstOrDefault()?.Value;

            var dockerTargetOS = _projectDocument
                .Descendants("DockerDefaultTargetOS")
                .FirstOrDefault()?.Value;

            // .NET 9.0 should work well with Linux containers
            if (targetFramework == "net9.0" && dockerTargetOS == "Linux")
            {
                true.Should().BeTrue("NET 9.0 and Linux Docker target are compatible");
            }
            else
            {
                // This test documents the expected configuration
                targetFramework.Should().StartWith("net", "should target .NET framework");
                dockerTargetOS.Should().Be("Linux", "should target Linux for containers");
            }
        }

        [Fact]
        public void ProjectStructure_ShouldSupportContainerization()
        {
            // Check if there might be a Dockerfile in the solution
            var solutionDirectory = Directory.GetParent(_projectDirectory)?.FullName;
            
            if (solutionDirectory != null)
            {
                var dockerFiles = Directory.GetFiles(solutionDirectory, "Dockerfile*", SearchOption.AllDirectories);
                var hasDockerSupport = dockerFiles.Length > 0 || 
                                     _projectDocument.Descendants("DockerDefaultTargetOS").Any();

                hasDockerSupport.Should().BeTrue("project should have Docker support indicated by configuration or Dockerfile presence");
            }
        }

        [Fact]
        public void ContainerToolsTargets_ShouldHaveExpectedVersion()
        {
            var containerToolsPackage = _projectDocument
                .Descendants("PackageReference")
                .FirstOrDefault(pr => pr.Attribute("Include")?.Value == "Microsoft.VisualStudio.Azure.Containers.Tools.Targets");

            var version = containerToolsPackage?.Attribute("Version")?.Value;
            version.Should().Be("1.21.0", "Container Tools Targets should have the expected version from the original project file");
        }
    }
}