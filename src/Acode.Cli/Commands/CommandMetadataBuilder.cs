namespace Acode.Cli.Commands;

/// <summary>
/// Fluent builder for constructing CommandMetadata.
/// </summary>
public sealed class CommandMetadataBuilder
{
    private readonly string _name;
    private readonly string _description;
    private readonly List<string> _aliases = new();
    private readonly List<CommandOption> _options = new();
    private readonly List<CommandExample> _examples = new();
    private readonly List<string> _relatedCommands = new();
    private string _usage;
    private bool _isVisible = true;
    private string? _group;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandMetadataBuilder"/> class.
    /// </summary>
    /// <param name="name">Command name.</param>
    /// <param name="description">Command description.</param>
    public CommandMetadataBuilder(string name, string description)
    {
        _name = name;
        _description = description;
        _usage = $"acode {name}";
    }

    /// <summary>
    /// Sets the usage pattern.
    /// </summary>
    /// <param name="usage">Usage pattern string.</param>
    /// <returns>The builder for chaining.</returns>
    public CommandMetadataBuilder WithUsage(string usage)
    {
        _usage = usage;
        return this;
    }

    /// <summary>
    /// Adds an alias for the command.
    /// </summary>
    /// <param name="alias">Alias name.</param>
    /// <returns>The builder for chaining.</returns>
    public CommandMetadataBuilder WithAlias(string alias)
    {
        _aliases.Add(alias);
        return this;
    }

    /// <summary>
    /// Adds an option to the command.
    /// </summary>
    /// <param name="option">The command option.</param>
    /// <returns>The builder for chaining.</returns>
    public CommandMetadataBuilder WithOption(CommandOption option)
    {
        _options.Add(option);
        return this;
    }

    /// <summary>
    /// Adds an example to the command.
    /// </summary>
    /// <param name="example">The command example.</param>
    /// <returns>The builder for chaining.</returns>
    public CommandMetadataBuilder WithExample(CommandExample example)
    {
        _examples.Add(example);
        return this;
    }

    /// <summary>
    /// Adds an example to the command.
    /// </summary>
    /// <param name="commandLine">The example command line.</param>
    /// <param name="description">Description of what the example does.</param>
    /// <returns>The builder for chaining.</returns>
    public CommandMetadataBuilder WithExample(string commandLine, string description)
    {
        _examples.Add(new CommandExample(commandLine, description));
        return this;
    }

    /// <summary>
    /// Adds a related command reference.
    /// </summary>
    /// <param name="commandName">Name of the related command.</param>
    /// <returns>The builder for chaining.</returns>
    public CommandMetadataBuilder WithRelatedCommand(string commandName)
    {
        _relatedCommands.Add(commandName);
        return this;
    }

    /// <summary>
    /// Sets the visibility of the command in help listings.
    /// </summary>
    /// <param name="isVisible">Whether the command should be visible.</param>
    /// <returns>The builder for chaining.</returns>
    public CommandMetadataBuilder WithVisibility(bool isVisible)
    {
        _isVisible = isVisible;
        return this;
    }

    /// <summary>
    /// Sets the command group for help organization.
    /// </summary>
    /// <param name="group">Group name.</param>
    /// <returns>The builder for chaining.</returns>
    public CommandMetadataBuilder InGroup(string group)
    {
        _group = group;
        return this;
    }

    /// <summary>
    /// Builds the CommandMetadata instance.
    /// </summary>
    /// <returns>The constructed CommandMetadata.</returns>
    public CommandMetadata Build() =>
        new(
            Name: _name,
            Description: _description,
            Usage: _usage,
            Aliases: _aliases.AsReadOnly(),
            Options: _options.AsReadOnly(),
            Examples: _examples.AsReadOnly(),
            RelatedCommands: _relatedCommands.AsReadOnly(),
            IsVisible: _isVisible,
            Group: _group
        );
}
