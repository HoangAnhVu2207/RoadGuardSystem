// Browser-only preparation example. Final-candidate smoke verification is pending.
// No automatic replay, token logging, TLS override or framework dependency.
export function createRoadGuardWebClient(baseUrl, fetchImpl = globalThis.fetch) {
  const base = new URL(baseUrl);
  if (base.protocol !== 'https:' || base.username || base.password || base.search || base.hash || base.pathname !== '/') {
    throw new TypeError('Use an HTTPS API origin without credentials, path, query or fragment.');
  }
  let csrf;
  const pendingKeys = new Set();
  async function request(path, { method = 'GET', body, key, etag } = {}) {
    if (!path.startsWith('/api/v1/') || path.includes('\\')) throw new TypeError('Use an API v1 absolute path.');
    const url = new URL(path, base);
    if (url.origin !== base.origin || !url.pathname.startsWith('/api/v1/')) throw new TypeError('Invalid API path.');
    const headers = new Headers({ Accept: 'application/json' });
    const mutation = !['GET', 'HEAD', 'OPTIONS'].includes(method);
    if (mutation) {
      if (!csrf) throw new Error('Bootstrap CSRF for the current identity first.');
      headers.set(csrf.headerName, csrf.requestToken);
    }
    if (body !== undefined) headers.set('Content-Type', 'application/json');
    if (key !== undefined) {
      if (typeof key !== 'string' || !key.trim()) throw new TypeError('An operation key must be nonempty.');
      if (pendingKeys.has(key)) throw new Error('This operation key already has a request in flight.');
      headers.set('Idempotency-Key', key);
    }
    if (etag !== undefined) headers.set('If-Match', etag);
    if (key !== undefined) pendingKeys.add(key);
    try {
      const response = await fetchImpl(url.href, { method, headers, credentials: 'include', cache: 'no-store',
        redirect: 'error', body: body === undefined ? undefined : JSON.stringify(body) });
      const text = await response.text();
      let data;
      if (text && /(?:application\/json|application\/problem\+json)/i.test(response.headers.get('content-type') ?? '')) {
        try { data = JSON.parse(text); } catch { /* Status remains available for malformed server JSON. */ }
      }
      // Keep this object private. The response may contain private resource fields.
      return { ok: response.ok, status: response.status, data, etag: response.headers.get('ETag'),
        correlationId: response.headers.get('X-Correlation-ID') };
    } finally {
      if (key !== undefined) pendingKeys.delete(key);
    }
  }
  async function bootstrapCsrf() {
    csrf = undefined;
    const result = await request('/api/v1/auth/web/csrf');
    if (result.ok && result.data?.headerName === 'X-CSRF-TOKEN' && typeof result.data.requestToken === 'string' && result.data.requestToken) {
      csrf = result.data;
    } else throw new Error('CSRF bootstrap failed. Inspect the HTTP status without logging credentials.');
  }
  return {
    request,
    bootstrapCsrf,
    session: () => request('/api/v1/auth/web/session'),
    async login(email, password) {
      await bootstrapCsrf();
      const result = await request('/api/v1/auth/web/login', { method: 'POST', body: { email, password } });
      csrf = undefined;
      if (result.ok) await bootstrapCsrf();
      return result;
    },
    async renew() {
      await bootstrapCsrf();
      const result = await request('/api/v1/auth/web/renew', { method: 'POST' });
      csrf = undefined;
      if (result.ok) await bootstrapCsrf();
      return result;
    },
    async logout(key) {
      try { return await request('/api/v1/auth/web/logout', { method: 'POST', key }); }
      finally { csrf = undefined; }
    }
  };
}
