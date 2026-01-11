// <copyright file="FileDiff.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.Events;

/// <summary>
/// Represents diff information for file changes.
/// </summary>
/// <param name="LinesAdded">Number of lines added.</param>
/// <param name="LinesRemoved">Number of lines removed.</param>
public sealed record FileDiff(int LinesAdded, int LinesRemoved);
