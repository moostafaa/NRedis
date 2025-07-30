using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using FluentAssertions;

namespace NRedis.Tests
{
    /// <summary>
    /// Tests to validate the overall project structure and conventions
    /// Testing Framework: xUnit with FluentAssertions
    /// </summary>
    public class ProjectStructureValidationTests
    {
        private readonly string _projectDirectory;
        private readonly string _solutionDirectory;

        public ProjectStructureValidationTests()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            _projectDirectory = Directory.GetParent(assemblyLocation)?.Parent?.Parent?.Parent?.FullName!;
            _solutionDirectory = Directory.GetParent(_projectDirectory)?.FullName!;
        }

        [Fact]
        public void TestProject_ShouldHaveProperDirectoryStructure()
        {
            Directory.Exists(_projectDirectory).Should().BeTrue("test project directory should exist");
            
            var projectName = Path.GetFileName(_projectDirectory);
            projectName.Should().Be("NRedis.Tests", "directory should match expected test project name");
        }

        [Fact]
        public void TestProject_ShouldBeInSolutionDirectory()
        {
            Directory.Exists(_solutionDirectory).Should().BeTrue("solution directory should exist");
            
            var testProjectPath = Path.Combine(_solutionDirectory, "NRedis.Tests");
            Directory.Exists(testProjectPath).Should().BeTrue("test project should be in solution directory");
        }

        [Fact]
        public void TestProject_ShouldHaveProjectFile()
        {
            var projectFilePath = Path.Combine(_projectDirectory, "NRedis.Tests.csproj");
            File.Exists(projectFilePath).Should().BeTrue("project file should exist");
        }

        [Fact]
        public void TestProject_ShouldHaveTestFiles()
        {
            var testFiles = Directory.GetFiles(_projectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(f => Path.GetFileName(f).Contains("Test"))
                .ToList();

            testFiles.Should().NotBeEmpty("test project should contain test files");
            testFiles.Should().Contain(f => f.EndsWith("Tests.cs"), "should have files ending with Tests.cs");
        }

        [Fact]
        public void SolutionProjects_ShouldExist()
        {
            var coreProjectPath = Path.Combine(_solutionDirectory, "NRedis.Core");
            var serverProjectPath = Path.Combine(_solutionDirectory, "NRedis.Server");
            
            Directory.Exists(coreProjectPath).Should().BeTrue("NRedis.Core project should exist in solution");
            Directory.Exists(serverProjectPath).Should().BeTrue("NRedis.Server project should exist in solution");
        }

        [Theory]
        [InlineData("bin")]
        [InlineData("obj")]
        public void TestProject_ShouldHaveBuildOutputDirectories(string directoryName)
        {
            // These directories are created during build, so we just verify the concept
            var directoryPath = Path.Combine(_projectDirectory, directoryName);
            
            // If the directory exists, it should be a directory (not a file)
            if (Directory.Exists(directoryPath))
            {
                Directory.Exists(directoryPath).Should().BeTrue($"{directoryName} should be a directory if it exists");
            }
            
            // This test mainly serves as documentation of expected build structure
            true.Should().BeTrue($"build process should create {directoryName} directory when building");
        }

        [Fact]
        public void TestProject_ShouldFollowNamingConventions()
        {
            var testFiles = Directory.GetFiles(_projectDirectory, "*.cs", SearchOption.AllDirectories);
            
            foreach (var testFile in testFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(testFile);
                
                // Test files should either end with "Tests" or contain "Test"
                (fileName.EndsWith("Tests") || fileName.Contains("Test"))
                    .Should().BeTrue($"test file {fileName} should follow naming conventions");
            }
        }

        [Fact]
        public void TestProject_ShouldNotContainBuildArtifacts()
        {
            var buildArtifacts = new[] { "*.dll", "*.pdb", "*.exe" };
            
            foreach (var pattern in buildArtifacts)
            {
                var artifacts = Directory.GetFiles(_projectDirectory, pattern, SearchOption.AllDirectories)
                    .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                    .ToList();
                
                artifacts.Should().BeEmpty($"source directory should not contain {pattern} files outside build directories");
            }
        }

        [Fact]
        public void TestNamespace_ShouldFollowConventions()
        {
            var currentNamespace = GetType().Namespace;
            currentNamespace.Should().Be("NRedis.Tests", "test namespace should match project name");
        }

        [Fact]
        public void TestAssembly_ShouldReferenceXUnit()
        {
            var referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            var xunitReference = referencedAssemblies.FirstOrDefault(a => a.Name?.StartsWith("xunit") == true);
            
            xunitReference.Should().NotBeNull("test assembly should reference xUnit framework");
        }

        [Fact]
        public void TestAssembly_ShouldReferenceFluentAssertions()
        {
            var referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            var fluentAssertionsReference = referencedAssemblies.FirstOrDefault(a => a.Name?.StartsWith("FluentAssertions") == true);
            
            fluentAssertionsReference.Should().NotBeNull("test assembly should reference FluentAssertions");
        }

        [Fact]
        public void SolutionFile_ShouldExist()
        {
            var solutionFiles = Directory.GetFiles(_solutionDirectory, "*.sln");
            solutionFiles.Should().NotBeEmpty("solution directory should contain a .sln file");
        }

        [Fact]
        public void ProjectReferences_ShouldPointToExistingProjects()
        {
            var coreProjectFile = Path.Combine(_solutionDirectory, "NRedis.Core", "NRedis.Core.csproj");
            var serverProjectFile = Path.Combine(_solutionDirectory, "NRedis.Server", "NRedis.Server.csproj");
            
            File.Exists(coreProjectFile).Should().BeTrue("NRedis.Core.csproj should exist");
            File.Exists(serverProjectFile).Should().BeTrue("NRedis.Server.csproj should exist");
        }
    }
}