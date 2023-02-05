using Semver;

namespace ExtravaWallSetup.Stages.InstallCheckSystem {
    public interface IPackage {
        string Name { get; init; }
        bool ShouldMarkToInstall { get; }
        SemVersion Version { get; init; }

        void Deconstruct(out string Name, out SemVersion Version);
        bool Equals(InstallCheckSystemStep.BasePackage? other);
        bool Equals(object? obj);
        int GetHashCode();
        string ToString();
    }
}