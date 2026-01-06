# TypeScript Language Guidelines

## Type Definitions

Use explicit types, avoid 'any':

**✅ CORRECT**:
```typescript
interface User {
  id: number;
  name: string;
  email: string;
  createdAt: Date;
}

function getUser(id: number): Promise<User> {
  return fetch(`/api/users/${id}`).then(r => r.json());
}
```

**❌ WRONG**:
```typescript
function getUser(id: any): any {
  return fetch(`/api/users/${id}`).then(r => r.json());
}
```

## Strict Mode

Enable strict mode in tsconfig.json:

```json
{
  "compilerOptions": {
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true,
    "strictFunctionTypes": true,
    "strictPropertyInitialization": true
  }
}
```

## Null Safety

Handle null/undefined explicitly:

```typescript
interface UserProfile {
  bio?: string; // Optional property
  avatar: string | null; // Nullable property
}

function displayBio(profile: UserProfile): string {
  // Option 1: Null coalescing
  return profile.bio ?? "No bio available";

  // Option 2: Optional chaining
  return profile.bio?.substring(0, 100) ?? "No bio";

  // Option 3: Type guard
  if (profile.bio) {
    return profile.bio;
  }
  return "No bio available";
}
```

## Type Guards

Use type guards for runtime type checking:

```typescript
function isString(value: unknown): value is string {
  return typeof value === 'string';
}

function processValue(value: string | number) {
  if (typeof value === 'string') {
    return value.toUpperCase(); // TypeScript knows value is string
  }
  return value.toFixed(2); // TypeScript knows value is number
}
```

## Generics

Use generics for reusable components:

```typescript
interface ApiResponse<T> {
  data: T;
  status: number;
  message: string;
}

async function fetchData<T>(url: string): Promise<ApiResponse<T>> {
  const response = await fetch(url);
  return response.json();
}

// Usage
const userResponse = await fetchData<User>('/api/user');
const postsResponse = await fetchData<Post[]>('/api/posts');
```

## Utility Types

Use built-in utility types:

```typescript
interface User {
  id: number;
  name: string;
  email: string;
  password: string;
}

// Pick specific properties
type UserPublic = Pick<User, 'id' | 'name' | 'email'>;

// Omit properties
type UserWithoutPassword = Omit<User, 'password'>;

// Make all properties optional
type PartialUser = Partial<User>;

// Make all properties readonly
type ReadonlyUser = Readonly<User>;

// Make all properties required
type RequiredUser = Required<User>;
```

## Enum vs Union Types

Prefer union types over enums for simple cases:

**✅ GOOD - Union type**:
```typescript
type OrderStatus = 'pending' | 'processing' | 'completed' | 'cancelled';

function updateStatus(orderId: number, status: OrderStatus) {
  // TypeScript provides autocomplete and validation
}
```

**✅ ALSO GOOD - Const enum for performance**:
```typescript
const enum OrderStatus {
  Pending = 'pending',
  Processing = 'processing',
  Completed = 'completed',
  Cancelled = 'cancelled'
}
```

## Type Assertions

Use type assertions sparingly:

```typescript
// Prefer type guards
function processElement(element: HTMLElement | null) {
  if (element instanceof HTMLInputElement) {
    console.log(element.value); // TypeScript knows it's HTMLInputElement
  }
}

// Use 'as' when you know more than TypeScript
const input = document.querySelector('#email') as HTMLInputElement;

// Avoid double assertions (usually indicates design issue)
const value = someValue as unknown as TargetType; // Generally avoid this
```

## Async/Await

Always use async/await over .then():

**✅ CORRECT**:
```typescript
async function loadUser(id: number): Promise<User> {
  try {
    const response = await fetch(`/api/users/${id}`);
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    return await response.json();
  } catch (error) {
    console.error('Failed to load user:', error);
    throw error;
  }
}
```

**❌ WRONG**:
```typescript
function loadUser(id: number): Promise<User> {
  return fetch(`/api/users/${id}`)
    .then(response => response.json())
    .catch(error => console.error(error));
}
```

## Discriminated Unions

Use discriminated unions for complex state:

```typescript
type LoadingState<T> =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; data: T }
  | { status: 'error'; error: string };

function renderUser(state: LoadingState<User>) {
  switch (state.status) {
    case 'idle':
      return <div>Not loaded</div>;
    case 'loading':
      return <div>Loading...</div>;
    case 'success':
      return <div>User: {state.data.name}</div>; // TypeScript knows data exists
    case 'error':
      return <div>Error: {state.error}</div>; // TypeScript knows error exists
  }
}
```
