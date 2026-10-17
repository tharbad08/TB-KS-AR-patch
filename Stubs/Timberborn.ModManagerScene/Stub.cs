// Compile-time only; NEVER distribute this DLL in the mod ZIP.
namespace Timberborn.ModManagerScene
{
    public interface IModStarter { void StartMod(IModEnvironment environment); }
    public interface IModEnvironment { string ModPath { get; } }
}
