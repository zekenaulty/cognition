using Cognition.Clients.Scope;

// TRACK-482: centralised factory for tests that still need direct construction.
#pragma warning disable RS0030 // Do not use banned APIs

namespace Cognition.Testing.Utilities;

public static class ScopePathBuilderTestHelper
{
    public static IScopePathBuilder CreateBuilder()
    {
        return new ScopePathBuilder();
    }
}

#pragma warning restore RS0030
