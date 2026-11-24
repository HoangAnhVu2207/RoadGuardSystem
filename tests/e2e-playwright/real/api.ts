import { randomUUID } from 'node:crypto';
import { request, type APIRequestContext } from '@playwright/test';
import { check, CheckError, guid } from './contract.js';

export type Event = Record<string, string | number | boolean>;
export type RecordEvent = (event: Event) => void;
type Options = Parameters<APIRequestContext['fetch']>[1];

export const profileRoles = {
  SUPERVISOR: 'SUPERVISOR',
  PM: 'PM',
  REPORTER: 'REPORTER',
  CREW: 'REPAIR_CREW',
  OPERATOR: 'DRONE_OPERATOR',
} as const;

type LoginRole = keyof typeof profileRoles;

export const idempotency = () => ({ 'Idempotency-Key': randomUUID() });

export function etag(value: unknown): string {
  check(typeof value === 'string' && /^"[^"\r\n]{1,512}"$/.test(value), 'expected_etag');
  return value;
}

export function safeCode(value: unknown): string {
  return typeof value === 'string' && /^[a-z][a-z0-9_]{1,63}$/.test(value) ? value : 'unknown_code';
}

export class Api {
  private constructor(private readonly context: APIRequestContext, private readonly record: RecordEvent) {}

  static async connect(baseURL: string, record: RecordEvent) {
    return new Api(await request.newContext({ baseURL, timeout: 20_000, ignoreHTTPSErrors: false }), record);
  }

  async dispose() {
    await this.context.dispose();
  }

  async call<T>(step: string, path: string, status: number, options: Options = {}, token?: string) {
    const started = Date.now();
    const response = await this.context.fetch(path, {
      ...options, failOnStatusCode: false, maxRedirects: 0, maxRetries: 0,
      headers: { ...options.headers, ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    }).catch(() => { throw new CheckError(`${step}_transport_error`); });
    try {
      const actual = response.status();
      const event: Event = { step, status: actual, elapsedMs: Date.now() - started };
      const correlationId = response.headers()['x-correlation-id'];
      if (correlationId && /^[0-9a-f-]{32,36}$/i.test(correlationId)) event.correlationId = correlationId;
      if (actual !== status) {
        const problem = await response.json().catch(() => null);
        event.code = safeCode(problem?.code);
        this.record(event);
        throw new CheckError(`${step}_unexpected_status`);
      }
      this.record(event);
      const json = actual !== 204 && response.headers()['content-type']?.includes('json');
      const body = (json ? await response.json().catch(() => { throw new CheckError(`${step}_invalid_json`); }) : undefined) as T;
      return { body, etag: response.headers().etag };
    } finally {
      await response.dispose();
    }
  }
}

type Tokens = {
  accessToken: string; refreshToken: string; expiresIn: number; mustChangePassword: boolean;
  user: { id: string; role: string };
};

export class Session {
  private tokens: Tokens | undefined;
  private expiresAt = 0;
  actorId = '';

  constructor(private readonly api: Api, private readonly role: LoginRole) {}

  private remember(tokens: Tokens) {
    check(tokens?.mustChangePassword === false, 'login_password_change_required_or_contract_mismatch');
    check(tokens.user?.role === this.role && tokens.accessToken && tokens.refreshToken &&
      Number.isFinite(tokens.expiresIn) && tokens.expiresIn > 0, 'session_role_or_tokens_mismatch');
    const id = guid(tokens.user.id);
    check(!this.actorId || id === this.actorId, 'session_identity_changed');
    this.actorId = id;
    this.tokens = tokens;
    this.expiresAt = Date.now() + tokens.expiresIn * 1000;
  }

  async login(email: string, password: string) {
    const { body } = await this.api.call<Tokens>('login', '/api/v1/auth/login', 200, {
      method: 'POST', data: { email, password },
    });
    this.remember(body);
  }

  async verifyProfile(email: string) {
    const { body } = await this.call<{ userId: string; roleCode: string; email?: string; username: string }>(
      'profile', '/api/v1/profile', 200);
    check(guid(body?.userId) === this.actorId && body.roleCode === profileRoles[this.role] &&
      [body.email, body.username].some(value => value?.toLowerCase() === email.toLowerCase()), 'profile_identity_mismatch');
  }

  async refresh() {
    check(this.tokens, 'session_missing');
    const { body } = await this.api.call<Tokens>('refresh', '/api/v1/auth/refresh', 200, {
      method: 'POST', headers: idempotency(), data: { refreshToken: this.tokens.refreshToken },
    });
    this.remember(body);
  }

  async call<T>(step: string, path: string, status: number, options?: Options) {
    if (Date.now() >= this.expiresAt - 60_000) await this.refresh();
    return this.api.call<T>(step, path, status, options, this.tokens?.accessToken);
  }

  async close() {
    try {
      if (this.tokens) {
        await this.call('logout', '/api/v1/auth/logout', 204, { method: 'POST', headers: idempotency() });
        await this.api.call('logged_out_profile', '/api/v1/profile', 401, {}, this.tokens.accessToken);
      }
    } finally {
      this.tokens = undefined;
      await this.api.dispose();
    }
  }
}
