import { once } from 'node:events';
import { createServer } from 'node:http';
import { expect, test } from '@playwright/test';
import { Api, type Event, Session } from './api.js';
import { assertMutation, byteRange, configuredOrigin, requireStorageUrl, telemetryEvidence, workProgress } from './contract.js';
import { assertWritableProject } from './video.js';

test('only the explicit mutation flag permits the real run', () => {
  for (const value of [undefined, '1', 'TRUE', 'false']) {
    expect(() => assertMutation(value)).toThrow('mutation_not_enabled');
  }
  expect(() => assertMutation('true')).not.toThrow();
});

test('real writes reject projects listed by the external protection manifest', () => {
  const protectedIds = new Set(['11111111-1111-1111-1111-111111111111']);
  expect(() => assertWritableProject('11111111-1111-1111-1111-111111111111', protectedIds))
    .toThrow('protected_project_write_blocked');
  expect(() => assertWritableProject('22222222-2222-2222-2222-222222222222', protectedIds)).not.toThrow();
});

test('only the configured TLS storage origin receives video bytes', () => {
  const origin = configuredOrigin('https://upload.example:9443', 'storage');
  const signed = 'https://upload.example:9443/bucket/video?X-Amz-Signature=opaque';
  expect(requireStorageUrl(signed, origin)).toBe(signed);
  for (const url of [
    'http://upload.example:9443/bucket/video?X-Amz-Signature=opaque',
    'https://upload.example/bucket/video?X-Amz-Signature=opaque',
    'https://upload.example:9443@other.example/bucket/video?X-Amz-Signature=opaque',
    'https://user@upload.example:9443/bucket/video?X-Amz-Signature=opaque',
    'https://upload.example:9443/bucket/video',
    'https://upload.example:9443/bucket/video?X-Amz-Signature=opaque#discarded',
  ]) {
    expect(() => requireStorageUrl(url, origin)).toThrow('storage_origin_or_signature_mismatch');
  }
});

test('ranges include the final partial part and reject invalid parts', () => {
  expect(byteRange(11, 5, 1)).toEqual({ start: 0, end: 4, length: 5 });
  expect(byteRange(11, 5, 3)).toEqual({ start: 10, end: 10, length: 1 });
  expect(() => byteRange(11, 5, 4)).toThrow('invalid_part_range');
  expect(() => byteRange(11, 0, 1)).toThrow('invalid_part_range');
});

test('only ingested REAL work is done', () => {
  expect(workProgress('Ingested')).toBe('done');
  for (const state of ['Pending', 'Admitted', 'Running', 'ArtifactsReady']) {
    expect(workProgress(state)).toBe('pending');
  }
  for (const state of ['RetryableFailed', 'TerminalFailed', 'Cancelled', 'Completed']) {
    expect(workProgress(state)).toBe('failed');
  }
});

test('a video with no telemetry or user revision still has valid readback', () => {
  expect(telemetryEvidence({
    headRevisionId: '00000000-0000-0000-0000-000000000000',
    measuredTelemetryStatus: 'MISSING_TELEMETRY',
    positionMeaning: 'DRONE_REFERENCE_NOT_GROUND_LOCATION',
    intervals: [],
  })).toEqual({ revisionIdPrefix: 'ABSENT', measuredTelemetryStatus: 'MISSING_TELEMETRY', positionIntervals: 0 });
});

test('embedded SRT remains a drone reference without a user revision', () => {
  expect(telemetryEvidence({
    headRevisionId: '00000000-0000-0000-0000-000000000000',
    measuredTelemetryStatus: 'EMBEDDED_SRT',
    positionMeaning: 'DRONE_REFERENCE_NOT_GROUND_LOCATION',
    intervals: [{}],
  }).measuredTelemetryStatus).toBe('EMBEDDED_SRT');
});

test('Operator uses an isolated login, profile, refresh and logout over the API client', async () => {
  const email = 'operator@example.test';
  const id = 'a1111111-1111-1111-1111-111111111111';
  const tokens = (version: number) => ({
    accessToken: `fake-access-${version}`, refreshToken: `fake-refresh-${version}`, expiresIn: 900,
    mustChangePassword: false, user: { id, role: 'OPERATOR' },
  });
  let revoked = false;
  const observed: { path?: string; bearer?: string; body: string }[] = [];
  const server = createServer(async (req, res) => {
    const chunks: Buffer[] = [];
    for await (const chunk of req) chunks.push(chunk);
    observed.push({ path: req.url, bearer: req.headers.authorization, body: Buffer.concat(chunks).toString() });
    res.setHeader('content-type', 'application/json');
    if (req.url === '/api/v1/auth/login') return res.end(JSON.stringify(tokens(1)));
    if (req.url === '/api/v1/auth/refresh') return res.end(JSON.stringify(tokens(2)));
    if (req.url === '/api/v1/auth/logout') {
      revoked = true;
      res.statusCode = 204;
      return res.end();
    }
    if (revoked) res.statusCode = 401;
    return res.end(JSON.stringify({ userId: id, roleCode: 'DRONE_OPERATOR', email }));
  }).listen(0, '127.0.0.1');
  await once(server, 'listening');
  const address = server.address();
  if (!address || typeof address === 'string') throw new Error('test_listener_missing');
  const events: Event[] = [];
  const api = await Api.connect(`http://127.0.0.1:${address.port}`, event => events.push(event));
  const session = new Session(api, 'OPERATOR');
  try {
    await session.login(email, 'fake-password');
    await session.verifyProfile(email);
    await session.refresh();
    await session.verifyProfile(email);
    await session.close();
    expect(observed.map(row => [row.path, row.bearer])).toEqual([
      ['/api/v1/auth/login', undefined],
      ['/api/v1/profile', 'Bearer fake-access-1'],
      ['/api/v1/auth/refresh', undefined],
      ['/api/v1/profile', 'Bearer fake-access-2'],
      ['/api/v1/auth/logout', 'Bearer fake-access-2'],
      ['/api/v1/profile', 'Bearer fake-access-2'],
    ]);
    expect(JSON.parse(observed[0].body)).toEqual({ email, password: 'fake-password' });
    expect(JSON.parse(observed[2].body)).toEqual({ refreshToken: 'fake-refresh-1' });
    expect(JSON.stringify(events)).not.toMatch(/fake-|operator@example/);
  } finally {
    await api.dispose();
    await new Promise<void>(resolve => server.close(() => resolve()));
  }
});
