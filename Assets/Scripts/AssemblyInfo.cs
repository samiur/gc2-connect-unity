// ABOUTME: Assembly-level attributes for OpenRange assembly.
// ABOUTME: Provides InternalsVisibleTo for test assemblies.

using System.Runtime.CompilerServices;

// Allow test assemblies to access internal members
[assembly: InternalsVisibleTo("OpenRange.Tests.EditMode")]
[assembly: InternalsVisibleTo("OpenRange.Tests.PlayMode")]
