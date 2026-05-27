---
description: React and Next.js performance optimization guidelines from Vercel Engineering. Use when writing, reviewing, or refactoring React/Next.js code — components, data fetching, bundle optimization, or performance improvements.
---

# React Best Practices

Comprehensive performance optimization guide for React and Next.js. 40+ rules across 8 categories from Vercel Engineering.

## Priority-Ordered Guidelines

| Priority | Category | Impact |
|----------|----------|--------|
| 1 | Eliminating Waterfalls | CRITICAL |
| 2 | Bundle Size Optimization | CRITICAL |
| 3 | Server-Side Performance | HIGH |
| 4 | Client-Side Data Fetching | MEDIUM-HIGH |
| 5 | Re-render Optimization | MEDIUM |
| 6 | Rendering Performance | MEDIUM |
| 7 | JavaScript Performance | LOW-MEDIUM |
| 8 | Advanced Patterns | LOW |

---

## 1. Eliminating Waterfalls (CRITICAL)

### Defer await until needed
```javascript
// ❌ Sequential — second awaits first unnecessarily
async function Page() {
  const user = await getUser();
  const posts = await getPosts(); // waits for user even though it doesn't need it
  return <Layout user={user} posts={posts} />;
}

// ✅ Parallel — both start at the same time
async function Page() {
  const [user, posts] = await Promise.all([getUser(), getPosts()]);
  return <Layout user={user} posts={posts} />;
}
```

### Start promises early, await late
```javascript
// ✅ Fire requests immediately, await only when value is needed
async function Page() {
  const userPromise = getUser();      // starts immediately
  const postsPromise = getPosts();    // starts immediately
  
  const user = await userPromise;     // await just before use
  const posts = await postsPromise;
  
  return <Layout user={user} posts={posts} />;
}
```

### `better-all` for partial dependencies
```javascript
// ✅ When B depends on part of A's result
import { betterAll } from 'better-all';

const [user, posts] = await betterAll([
  getUser(),
  ({ user }) => getPosts(user.id)  // starts as soon as user.id is available
]);
```

### Suspense boundaries for streaming
```jsx
// ✅ Start rendering immediately, stream content as it resolves
export default function Page() {
  return (
    <div>
      <Header />
      <Suspense fallback={<PostsSkeleton />}>
        <Posts />   {/* streams in when ready */}
      </Suspense>
      <Suspense fallback={<CommentsSkeleton />}>
        <Comments />  {/* streams independently */}
      </Suspense>
    </div>
  );
}
```

---

## 2. Bundle Size Optimization (CRITICAL)

### Avoid barrel file imports
```javascript
// ❌ Barrel import — can load 1,583 modules (e.g. lucide-react)
import { Home, User, Settings } from 'lucide-react';

// ✅ Direct import — only loads 3 modules
import { Home } from 'lucide-react/dist/esm/icons/home';
import { User } from 'lucide-react/dist/esm/icons/user';
import { Settings } from 'lucide-react/dist/esm/icons/settings';
```

### next/dynamic for heavy components
```javascript
// ❌ Eager import — included in main bundle
import HeavyEditor from './HeavyEditor';

// ✅ Dynamic import — excluded from main bundle
const HeavyEditor = dynamic(() => import('./HeavyEditor'), {
  ssr: false,
  loading: () => <EditorSkeleton />
});
```

### Defer analytics and non-critical third parties
```javascript
// ❌ Analytics loads immediately, blocks TTI
import Analytics from '@analytics/google-analytics';
Analytics.init();

// ✅ Defer until after first interaction or idle
if ('requestIdleCallback' in window) {
  requestIdleCallback(() => import('./analytics').then(m => m.init()));
} else {
  setTimeout(() => import('./analytics').then(m => m.init()), 2000);
}
```

### Preload on user intent
```javascript
// ✅ Preload on hover (200ms before click)
function NavLink({ href, children }) {
  const prefetch = () => router.prefetch(href);
  return (
    <a href={href} onMouseEnter={prefetch} onFocus={prefetch}>
      {children}
    </a>
  );
}
```

---

## 3. Server-Side Performance (HIGH)

### React.cache() for per-request deduplication
```javascript
import { cache } from 'react';

// ✅ Multiple components calling getUser() share one request per render
const getUser = cache(async (id) => {
  const user = await db.user.findUnique({ where: { id } });
  return user;
});
```

### LRU cache for cross-request caching
```javascript
import { LRUCache } from 'lru-cache';

const userCache = new LRUCache({ max: 500, ttl: 1000 * 60 * 5 }); // 5 min

export async function getUser(id) {
  const cached = userCache.get(id);
  if (cached) return cached;
  const user = await db.user.findUnique({ where: { id } });
  userCache.set(id, user);
  return user;
}
```

### Minimize RSC boundary serialization
```jsx
// ❌ Large object serialized across RSC boundary
async function ServerComponent() {
  const allUserData = await fetchUserWithAllRelations(); // 50KB object
  return <ClientComponent user={allUserData} />;
}

// ✅ Pass only what the client component needs
async function ServerComponent() {
  const { name, avatar, role } = await fetchUser(); // 200 bytes
  return <ClientComponent name={name} avatar={avatar} role={role} />;
}
```

### Parallel data fetching via component composition
```jsx
// ✅ Parent fetches its data, children fetch theirs — all in parallel
async function DashboardPage() {
  const stats = await getStats(); // starts here
  return (
    <div>
      <StatsPanel stats={stats} />
      <RecentOrders />   {/* fetches its own data in parallel */}
      <UserActivity />   {/* fetches its own data in parallel */}
    </div>
  );
}
```

---

## 4. Client-Side Data Fetching (MEDIUM-HIGH)

### SWR for automatic deduplication
```javascript
// ✅ Multiple components calling useUser(id) share one request
function useUser(id) {
  const { data, error, isLoading } = useSWR(`/api/users/${id}`, fetcher, {
    revalidateOnFocus: false,
    dedupingInterval: 2000
  });
  return { user: data, error, isLoading };
}
```

### useSWRSubscription for global events
```javascript
// ✅ Deduplicate WebSocket connections across components
function useStockPrice(ticker) {
  return useSWRSubscription(`stock:${ticker}`, (key, { next }) => {
    const ws = getSharedWebSocket();
    const handler = (data) => next(null, data.price);
    ws.on(ticker, handler);
    return () => ws.off(ticker, handler); // cleanup
  });
}
```

---

## 5. Re-render Optimization (MEDIUM)

### Defer state reads to usage point
```javascript
// ❌ Reading state in parent causes full re-render on change
function Parent() {
  const [count, setCount] = useState(0);
  return (
    <div>
      <Counter count={count} onChange={setCount} />
      <ExpensiveChild />  {/* re-renders on every count change */}
    </div>
  );
}

// ✅ Move state down to the component that uses it
function Counter() {
  const [count, setCount] = useState(0); // state lives here
  return <button onClick={() => setCount(c => c + 1)}>{count}</button>;
}

function Parent() {
  return (
    <div>
      <Counter />         {/* manages its own state */}
      <ExpensiveChild />  {/* never re-renders */}
    </div>
  );
}
```

### Memoize expensive components
```javascript
// ✅ Only re-renders when props change
const ExpensiveChart = React.memo(({ data, options }) => {
  return <Chart data={data} options={options} />;
}, (prevProps, nextProps) => {
  return prevProps.data === nextProps.data; // custom comparison
});
```

### Narrow effect dependencies to primitives
```javascript
// ❌ Object identity changes on every render — effect runs constantly
useEffect(() => {
  fetchData(config.endpoint);
}, [config]); // config is a new object every render

// ✅ Primitive dependency — stable reference
useEffect(() => {
  fetchData(endpoint);
}, [endpoint]); // only runs when the string changes
```

### startTransition for non-urgent updates
```javascript
// ✅ Mark expensive state updates as non-urgent
const [isPending, startTransition] = useTransition();

function handleSearch(query) {
  setInputValue(query);           // urgent: update input immediately
  startTransition(() => {
    setSearchResults(search(query)); // non-urgent: can be interrupted
  });
}
```

---

## 6. Rendering Performance (MEDIUM)

### Animate SVG wrapper, not SVG element
```jsx
// ❌ Animating SVG directly — repaints entire SVG tree
<motion.svg animate={{ rotate: 360 }} />

// ✅ Wrap in div — GPU-composited transform
<motion.div animate={{ rotate: 360 }}>
  <svg />
</motion.div>
```

### content-visibility: auto for long lists
```css
/* ✅ Browser skips rendering off-screen items */
.list-item {
  content-visibility: auto;
  contain-intrinsic-size: 0 80px; /* estimated item height */
}
```

### Prevent hydration mismatch with inline scripts
```html
<!-- ✅ Set class before hydration to prevent FOUC -->
<script>
  document.documentElement.classList.toggle(
    'dark',
    localStorage.theme === 'dark' ||
    (!localStorage.theme && window.matchMedia('(prefers-color-scheme: dark)').matches)
  );
</script>
```

### Explicit conditional rendering
```jsx
// ❌ Short-circuit can render "0" when count is 0
{count && <List items={items} />}

// ✅ Explicit boolean
{count > 0 && <List items={items} />}

// ✅ Or ternary
{count ? <List items={items} /> : null}
```

---

## 7. JavaScript Performance (LOW-MEDIUM)

### Build index maps for O(1) lookups
```javascript
// ❌ O(n) lookup in a loop → O(n²) total
const getUser = (id) => users.find(u => u.id === id);
orders.map(order => getUser(order.userId));

// ✅ Build index once, O(1) lookups
const userMap = new Map(users.map(u => [u.id, u]));
orders.map(order => userMap.get(order.userId));
```

### Use toSorted() instead of sort()
```javascript
// ❌ sort() mutates original array
const sorted = items.sort((a, b) => a.name.localeCompare(b.name));

// ✅ toSorted() returns new array — no mutation
const sorted = items.toSorted((a, b) => a.name.localeCompare(b.name));
```

### Cache property access in loops
```javascript
// ❌ Accesses .length on every iteration
for (let i = 0; i < items.length; i++) { }

// ✅ Cache length
const len = items.length;
for (let i = 0; i < len; i++) { }
```

### Early return for array comparisons
```javascript
// ✅ Fast path for obvious mismatch
function arraysEqual(a, b) {
  if (a.length !== b.length) return false; // O(1) early exit
  return a.every((item, i) => item === b[i]);
}
```

---

## 8. Advanced Patterns (LOW)

### Store event handlers in refs
```javascript
// ✅ Stable handler reference without re-registering listeners
function useEventListener(event, handler) {
  const handlerRef = useRef(handler);
  useEffect(() => { handlerRef.current = handler; });
  
  useEffect(() => {
    const listener = (e) => handlerRef.current(e);
    window.addEventListener(event, listener);
    return () => window.removeEventListener(event, listener);
  }, [event]); // stable — never re-registers
}
```

### useLatest for stable callback refs
```javascript
// ✅ Always calls the latest version of callback without stale closures
function useLatest(value) {
  const ref = useRef(value);
  useEffect(() => { ref.current = value; });
  return ref;
}

function useInterval(callback, delay) {
  const savedCallback = useLatest(callback);
  useEffect(() => {
    const id = setInterval(() => savedCallback.current(), delay);
    return () => clearInterval(id);
  }, [delay]);
}
```
