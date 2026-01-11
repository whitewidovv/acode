# TypeScript Language Guidelines

## Code Style

### Naming Conventions
- **PascalCase**: Types, Interfaces, Classes, Enums, Components
- **camelCase**: Variables, functions, methods, properties
- **UPPER_SNAKE_CASE**: Constants
- **I prefix**: Avoid for interfaces (use Props, State suffixes instead)

### Modern TypeScript Features
- **Const assertions**: `as const` for literal types
- **Template literal types**: `` `${Prefix}${Name}` ``
- **Satisfies operator**: `config satisfies Config`
- **Type narrowing**: Use type guards effectively

### Common Patterns

```typescript
// Interface for props
interface ButtonProps {
  label: string;
  onClick: () => void;
  disabled?: boolean;
}

// Type for union
type Status = 'idle' | 'loading' | 'success' | 'error';

// Generic function
function getValue<T>(key: string): T | undefined {
  // ...
}

// Type guard
function isError(value: unknown): value is Error {
  return value instanceof Error;
}

// Const object for enum-like behavior
const Colors = {
  Red: '#ff0000',
  Blue: '#0000ff',
} as const;
```

## Avoid

- Using `any` (use `unknown` instead)
- Type assertions (`as Type`) when narrowing is possible
- Non-null assertions (`!`) without good reason
- Complex nested generics
