namespace Hdt.Core.Services;

public static class ReservedPhaseCommand
{
    public static string BuildMessage(string commandName, int phase) =>
        $"{commandName} is reserved for Phase {phase} and is not implemented in the Phase 1 foundation build.";
}
