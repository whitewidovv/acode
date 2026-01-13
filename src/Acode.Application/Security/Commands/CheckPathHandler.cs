namespace Acode.Application.Security.Commands;

/// <summary>
/// Handler for CheckPathCommand that validates paths against protection rules.
/// </summary>
public sealed class CheckPathHandler
{
    private readonly IProtectedPathValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckPathHandler"/> class.
    /// </summary>
    /// <param name="validator">The path validator.</param>
    public CheckPathHandler(IProtectedPathValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// Handles the CheckPathCommand.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <returns>Validation result indicating if path is protected.</returns>
    public PathValidationResult Handle(CheckPathCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Operation.HasValue)
        {
            return _validator.Validate(command.Path, command.Operation.Value);
        }

        return _validator.Validate(command.Path);
    }
}
