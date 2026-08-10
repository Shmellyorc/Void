namespace Void.Engine.Assets.Mounts;

public interface IMount
{
    string Name { get; }

    bool HasFile(string virtualPath);

    byte[] ReadFile(string virtualPath);
}
