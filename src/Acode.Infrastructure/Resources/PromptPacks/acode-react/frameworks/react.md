# React Framework Guidelines

## Component Structure

```
src/
├── components/      # Reusable components
├── pages/          # Page components (routes)
├── hooks/          # Custom hooks
├── utils/          # Utility functions
├── types/          # TypeScript types
└── services/       # API calls
```

## Common Patterns

### Functional Components
```tsx
interface ButtonProps {
  label: string;
  onClick: () => void;
}

export function Button({ label, onClick }: ButtonProps) {
  return (
    <button onClick={onClick}>
      {label}
    </button>
  );
}
```

### Custom Hooks
```tsx
function useCounter(initial = 0) {
  const [count, setCount] = useState(initial);
  
  const increment = useCallback(() => {
    setCount(c => c + 1);
  }, []);
  
  return { count, increment };
}
```

### useEffect Patterns
```tsx
// Fetch data
useEffect(() => {
  let cancelled = false;
  
  async function fetchData() {
    const data = await api.get('/items');
    if (!cancelled) setItems(data);
  }
  
  fetchData();
  return () => { cancelled = true; };
}, []);
```

### Context
```tsx
const ThemeContext = createContext<Theme | null>(null);

export function useTheme() {
  const theme = useContext(ThemeContext);
  if (!theme) throw new Error('useTheme must be within ThemeProvider');
  return theme;
}
```

## Testing

```tsx
// React Testing Library
import { render, screen, fireEvent } from '@testing-library/react';

test('button calls onClick', () => {
  const handleClick = jest.fn();
  render(<Button label="Click" onClick={handleClick} />);
  
  fireEvent.click(screen.getByRole('button'));
  
  expect(handleClick).toHaveBeenCalledTimes(1);
});
```

## Avoid

- Inline function definitions in JSX (unless simple)
- Direct DOM manipulation
- Prop drilling (use Context or state management)
- Over-optimization (premature useMemo/useCallback)
