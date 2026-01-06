# React Framework Guidelines

## Component Patterns

Use functional components with hooks (not class components):

**✅ CORRECT - Functional component**:
```typescript
interface UserCardProps {
  user: User;
  onEdit: (id: number) => void;
}

export function UserCard({ user, onEdit }: UserCardProps) {
  return (
    <div className="user-card">
      <h3>{user.name}</h3>
      <p>{user.email}</p>
      <button onClick={() => onEdit(user.id)}>Edit</button>
    </div>
  );
}
```

**❌ WRONG - Class component**:
```typescript
export class UserCard extends React.Component<UserCardProps> {
  render() {
    return <div>...</div>;
  }
}
```

## Hooks Best Practices

### useState

```typescript
// Simple state
const [count, setCount] = useState(0);
const [user, setUser] = useState<User | null>(null);

// Functional updates when new state depends on old
setCount(prevCount => prevCount + 1);

// Initialize with function for expensive computation
const [data, setData] = useState(() => expensiveComputation());
```

### useEffect

```typescript
// Effect with cleanup
useEffect(() => {
  const subscription = api.subscribe(userId);

  // Cleanup function
  return () => {
    subscription.unsubscribe();
  };
}, [userId]); // Dependencies array - re-run when userId changes

// One-time effect (mount only)
useEffect(() => {
  loadInitialData();
}, []); // Empty array = run once

// Effect without cleanup
useEffect(() => {
  document.title = `User: ${user.name}`;
}, [user.name]);
```

### useMemo

Memoize expensive computations:

```typescript
const sortedUsers = useMemo(() => {
  return users
    .filter(u => u.isActive)
    .sort((a, b) => a.name.localeCompare(b.name));
}, [users]); // Recompute only when users changes
```

### useCallback

Memoize callback functions:

```typescript
const handleClick = useCallback((userId: number) => {
  console.log('Clicked user:', userId);
  updateUser(userId);
}, [updateUser]); // Recreate only when updateUser changes

// Pass to child to prevent unnecessary re-renders
<UserList users={users} onUserClick={handleClick} />
```

### Custom Hooks

Extract reusable logic:

```typescript
function useUser(userId: number) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadUser() {
      try {
        setLoading(true);
        const data = await fetchUser(userId);
        if (!cancelled) {
          setUser(data);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    loadUser();

    return () => {
      cancelled = true;
    };
  }, [userId]);

  return { user, loading, error };
}

// Usage
function UserProfile({ userId }: { userId: number }) {
  const { user, loading, error } = useUser(userId);

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;
  if (!user) return <div>User not found</div>;

  return <div>{user.name}</div>;
}
```

## State Management

### Context for Global State

```typescript
interface AppContextType {
  user: User | null;
  theme: 'light' | 'dark';
  setTheme: (theme: 'light' | 'dark') => void;
}

const AppContext = createContext<AppContextType | undefined>(undefined);

export function AppProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [theme, setTheme] = useState<'light' | 'dark'>('light');

  const value = { user, theme, setTheme };

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>;
}

export function useApp() {
  const context = useContext(AppContext);
  if (!context) {
    throw new Error('useApp must be used within AppProvider');
  }
  return context;
}
```

### useReducer for Complex State

```typescript
type State = {
  users: User[];
  loading: boolean;
  error: string | null;
};

type Action =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: User[] }
  | { type: 'FETCH_ERROR'; payload: string };

function userReducer(state: State, action: Action): State {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, loading: true, error: null };
    case 'FETCH_SUCCESS':
      return { ...state, loading: false, users: action.payload };
    case 'FETCH_ERROR':
      return { ...state, loading: false, error: action.payload };
    default:
      return state;
  }
}

function UserList() {
  const [state, dispatch] = useReducer(userReducer, {
    users: [],
    loading: false,
    error: null,
  });

  useEffect(() => {
    dispatch({ type: 'FETCH_START' });
    fetchUsers()
      .then(users => dispatch({ type: 'FETCH_SUCCESS', payload: users }))
      .catch(err => dispatch({ type: 'FETCH_ERROR', payload: err.message }));
  }, []);

  return <div>...</div>;
}
```

## Performance Optimization

### React.memo

Prevent unnecessary re-renders:

```typescript
interface UserCardProps {
  user: User;
  onEdit: (id: number) => void;
}

export const UserCard = React.memo(function UserCard({ user, onEdit }: UserCardProps) {
  return <div onClick={() => onEdit(user.id)}>{user.name}</div>;
});

// Custom comparison function
export const UserCard = React.memo(
  function UserCard({ user, onEdit }: UserCardProps) {
    return <div>...</div>;
  },
  (prevProps, nextProps) => prevProps.user.id === nextProps.user.id
);
```

### Lazy Loading

```typescript
// Code splitting with lazy and Suspense
const UserProfile = lazy(() => import('./UserProfile'));

function App() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <UserProfile />
    </Suspense>
  );
}
```

## Error Boundaries

```typescript
interface ErrorBoundaryState {
  hasError: boolean;
  error?: Error;
}

class ErrorBoundary extends React.Component<
  { children: React.ReactNode },
  ErrorBoundaryState
> {
  constructor(props: { children: React.ReactNode }) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Error caught by boundary:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return <div>Something went wrong: {this.state.error?.message}</div>;
    }

    return this.props.children;
  }
}

// Usage
<ErrorBoundary>
  <UserProfile userId={123} />
</ErrorBoundary>
```

## Form Handling

```typescript
function LoginForm() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [errors, setErrors] = useState<{ email?: string; password?: string }>({});

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const newErrors: typeof errors = {};
    if (!email) newErrors.email = 'Email is required';
    if (!password) newErrors.password = 'Password is required';

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    try {
      await login(email, password);
    } catch (error) {
      setErrors({ email: 'Invalid credentials' });
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <input
        type="email"
        value={email}
        onChange={e => setEmail(e.target.value)}
        aria-invalid={!!errors.email}
      />
      {errors.email && <span className="error">{errors.email}</span>}

      <input
        type="password"
        value={password}
        onChange={e => setPassword(e.target.value)}
        aria-invalid={!!errors.password}
      />
      {errors.password && <span className="error">{errors.password}</span>}

      <button type="submit">Login</button>
    </form>
  );
}
```

## Testing

Write tests for components:

```typescript
import { render, screen, fireEvent } from '@testing-library/react';
import { UserCard } from './UserCard';

describe('UserCard', () => {
  const mockUser = {
    id: 1,
    name: 'John Doe',
    email: 'john@example.com',
  };

  it('renders user information', () => {
    render(<UserCard user={mockUser} onEdit={() => {}} />);

    expect(screen.getByText('John Doe')).toBeInTheDocument();
    expect(screen.getByText('john@example.com')).toBeInTheDocument();
  });

  it('calls onEdit when button clicked', () => {
    const onEdit = jest.fn();
    render(<UserCard user={mockUser} onEdit={onEdit} />);

    fireEvent.click(screen.getByRole('button', { name: /edit/i }));

    expect(onEdit).toHaveBeenCalledWith(1);
  });
});
```
