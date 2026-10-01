using System;

namespace Metrics.GitHub;

/// <summary>
/// Represents the data wrapper containing the viewer information returned from the API.
/// </summary>
/// <param name="viewer">The viewer object containing user details.</param>
public record ViewerData(Viewer viewer);

/// <summary>
/// Represents the root object for a viewer API response.
/// </summary>
/// <param name="data">The data object containing the viewer information.</param>
public record ViewerRoot(ViewerData data);

/// <summary>
/// Represents a GitHub user (viewer) with login, name, and email information.
/// </summary>
/// <param name="login">The login name of the viewer.</param>
/// <param name="name">The full name of the viewer.</param>
/// <param name="email">The email address of the viewer.</param>
public record Viewer(string login, string name, string email);
