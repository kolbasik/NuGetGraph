using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NuGet;

namespace NuGetGraph.Components
{
    public static class DotNetCore
    {
        public abstract class PackageRepositoryBase : IPackageLookup
        {
            protected PackageRepositoryBase(string source)
            {
                Source = source;
            }

            public string Source { get; }
            public bool SupportsPrereleasePackages { get; } = true;
            public PackageSaveModes PackageSaveMode { get; set; } = PackageSaveModes.Nupkg;

            public bool Exists(string packageId, SemanticVersion version)
            {
                return FindPackage(packageId, version) != null;
            }

            public virtual IPackage FindPackage(string packageId, SemanticVersion version)
            {
                return GetPackages().FirstOrDefault(package => string.Equals(package.Id, packageId) && package.Version.Equals(version));
            }

            public virtual IEnumerable<IPackage> FindPackagesById(string packageId)
            {
                return GetPackages().Where(package => string.Equals(package.Id, packageId));
            }

            public virtual IQueryable<IPackage> GetPackages()
            {
                throw new NotSupportedException();
            }

            public void AddPackage(IPackage package)
            {
                throw new NotSupportedException();
            }

            public void RemovePackage(IPackage package)
            {
                throw new NotSupportedException();
            }
        }

        public sealed class PackageReferenceRepository : PackageRepositoryBase
        {
            private readonly IPackageLookup sharedPackageRepository;

            public PackageReferenceRepository(string source, IPackageLookup sharedPackageRepository) : base(source)
            {
                this.sharedPackageRepository = sharedPackageRepository;
            }

            public override IPackage FindPackage(string packageId, SemanticVersion version)
            {
                return sharedPackageRepository.FindPackage(packageId, version);
            }

            public override IQueryable<IPackage> GetPackages()
            {
                var packages = new List<IPackage>();
                var xDocument = XDocument.Load(Source);
                var xPackageReference = xDocument.Descendants()
                    .Where(x => string.Equals("PackageReference", x.Name.LocalName, StringComparison.OrdinalIgnoreCase));
                foreach (var xElement in xPackageReference)
                {
                    var packageId = xElement.GetOptionalAttributeValue("Include") ?? xElement.GetOptionalElementValue("Include");
                    var packageVersion = new SemanticVersion(xElement.GetOptionalAttributeValue("Version") ?? xElement.GetOptionalElementValue("Version"));
                    var package = FindPackage(packageId, packageVersion);
                    if (package != null)
                    {
                        packages.Add(package);
                    }
                }
                return packages.AsQueryable();
            }
        }

        public sealed class SharedPackageRepository : PackageRepositoryBase
        {
            private readonly IPackagePathResolver pathResolver;
            private readonly IFileSystem fileSystem;

            public SharedPackageRepository(string path) : this(new PhysicalFileSystem(path))
            {
            }

            public SharedPackageRepository(IFileSystem fileSystem) : this(new PackagePathResolver(fileSystem), fileSystem)
            {
            }

            public SharedPackageRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem) : base(fileSystem.Root)
            {
                this.pathResolver = pathResolver;
                this.fileSystem = fileSystem;
            }

            public override IPackage FindPackage(string packageId, SemanticVersion version)
            {
                var packageDirectory = pathResolver.GetPackageDirectory(packageId, version);
                if (fileSystem.DirectoryExists(packageDirectory))
                {
                    var packageFileName = pathResolver.GetPackageFileName(packageId, version);
                    var packageFilePath = Path.Combine(packageDirectory, packageFileName);
                    if (fileSystem.FileExists(packageFilePath))
                    {
                        return new OptimizedZipPackage(fileSystem, packageFilePath);
                    }
                }
                return null;
            }

            private sealed class PackagePathResolver : DefaultPackagePathResolver
            {
                public PackagePathResolver(IFileSystem fileSystem) : base(fileSystem, true)
                {
                }

                public override string GetPackageDirectory(string packageId, SemanticVersion version)
                {
                    return string.Concat(packageId, @"/", version);
                }
            }
        }
    }
}