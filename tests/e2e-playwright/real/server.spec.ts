import { randomUUID } from 'node:crypto';
import { mkdir, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { test } from '@playwright/test';
import { Api, type Event, profileRoles, Session } from './api.js';
import { assertMutation, check, CheckError, configuredOrigin } from './contract.js';
import { clips, inspectVideo, preflightTarget, runVideo, targetFromEnv } from './video.js';

// Run role smoke before the Operator retains their session for video work.
const roles = ['SUPERVISOR', 'PM', 'REPORTER', 'CREW', 'OPERATOR'] as const satisfies readonly (keyof typeof profileRoles)[];

test('RoadGuard FE-shaped API session and video workflow', async () => {
  const mode = process.env.E2E_REAL_MODE ?? 'dry-run';
  const runId = `${new Date().toISOString().replace(/[-:.]/g, '')}-${randomUUID().slice(0, 8)}`;
  const results = resolve(import.meta.dirname, '../results');
  const events: Event[] = [];
  const actorIds = new Set<string>();
  let scope = 'run';
  let release: string | undefined;
  let host: string | undefined;
  let failure: string | undefined;
  const record = (event: Event) => events.push({ scope, ...event });

  try {
    check(['dry-run', 'auth', 'preflight', 'real'].includes(mode), 'invalid_run_mode');
    const candidate = process.env.E2E_EXPECTED_RELEASE;
    if (candidate) {
      check(/^[0-9a-f]{40}$/i.test(candidate), 'invalid_expected_release');
      release = candidate;
    }
    await mkdir(results, { recursive: true });
    // Create the result file before making any remote request.
    await writeFile(resolve(results, `real-${runId}.json`), '{}\n', { flag: 'wx', mode: 0o600 });
    if (mode === 'real') assertMutation(process.env.E2E_ALLOW_MUTATION);

    const videos = [];
    if (mode === 'dry-run' || mode === 'real') {
      for (const clip of clips) {
        scope = clip.key;
        videos.push(await inspectVideo(clip, record));
      }
    }
    scope = 'run';
    if (mode === 'dry-run' && !process.env.E2E_BASE_URL) {
      record({ step: 'api_origin', status: 'NOT_CONFIGURED' });
    } else {
      const origin = configuredOrigin(process.env.E2E_BASE_URL, 'api');
      host = new URL(origin).host;
    }
    const storageEnv = process.env.E2E_STORAGE_ORIGIN;
    const storage = mode === 'real' || storageEnv ? configuredOrigin(storageEnv, 'storage') : undefined;
    if (mode === 'dry-run') {
      if (process.env.E2E_KENH_DONG_TASK_ID || process.env.E2E_DUONG_SO_2_TASK_ID) {
        const [first, second] = clips.map(targetFromEnv);
        check(first.taskId !== second.taskId && first.projectId !== second.projectId, 'targets_must_be_distinct');
      }
      record({ step: 'local_validation', status: 'PASS' });
      return;
    }

    check(!process.env.DEBUG && !process.env.PWDEBUG && !process.env.NODE_DEBUG && !process.env.NODE_DEBUG_NATIVE &&
      process.env.NODE_TLS_REJECT_UNAUTHORIZED !== '0', 'unsafe_debug_or_tls_environment');
    const withVideo = mode === 'preflight' || mode === 'real';
    const targets = withVideo ? clips.map(targetFromEnv) : [];
    if (withVideo) {
      check(targets[0].taskId !== targets[1].taskId && targets[0].projectId !== targets[1].projectId,
        'targets_must_be_distinct');
    }
    const credentials = roles.map(key => {
      const email = process.env[`E2E_${key}_EMAIL`]?.trim();
      const password = process.env[`E2E_${key}_PASSWORD`];
      check(Boolean(email) === Boolean(password), `incomplete_${key.toLowerCase()}_credentials`);
      return { key, email, password };
    });
    const supplied = credentials.filter(entry => entry.email);
    check(supplied.length > 0, 'missing_credentials');
    if (withVideo) check(credentials.at(-1)?.email, 'operator_credentials_required');
    check(new Set(supplied.map(entry => entry.email?.toLowerCase())).size === supplied.length, 'duplicate_role_credentials');

    const origin = `https://${host}`;
    const anonymous = await Api.connect(origin, record);
    try {
      await anonymous.call('health', '/health', 200);
      await anonymous.call('anonymous_profile', '/api/v1/profile', 401);
    } finally {
      await anonymous.dispose();
    }
    for (const entry of credentials) {
      scope = entry.key;
      if (!entry.email || !entry.password) {
        record({ step: 'auth', status: 'NOT_VERIFIED' });
        continue;
      }
      const session = new Session(await Api.connect(origin, record), entry.key);
      let closingScope = entry.key as string;
      let sessionFailed = false;
      try {
        await session.login(entry.email, entry.password);
        await session.verifyProfile(entry.email);
        check(!actorIds.has(session.actorId), 'role_session_actor_reused');
        actorIds.add(session.actorId);
        await session.refresh();
        await session.verifyProfile(entry.email);
        record({ step: 'login_and_refresh', status: 'PASS', evidence: 'PUBLIC_E2E_VERIFIED' });
        if (entry.key === 'OPERATOR' && withVideo) {
          for (const [index, target] of targets.entries()) {
            scope = clips[index].key;
            await preflightTarget(session, target);
            record({ step: 'video_preflight', status: 'PASS', evidence: 'BE_VERIFIED' });
          }
          if (mode === 'real' && storage) {
            for (const [index, video] of videos.entries()) {
              scope = video.clip.key;
              await runVideo(session, targets[index], video, storage, runId, record);
              record({ step: 'video_e2e', status: 'PASS', evidence: 'PUBLIC_E2E_VERIFIED' });
            }
          }
        }
      } catch (error) {
        closingScope = scope;
        sessionFailed = true;
        throw error;
      } finally {
        scope = entry.key;
        try {
          await session.close();
          if (!sessionFailed) record({ step: 'session_logout', status: 'PASS' });
        } catch (error) {
          if (!sessionFailed) throw error;
          record({ step: 'session_logout', status: 'FAIL' });
        } finally {
          if (sessionFailed) scope = closingScope;
        }
      }
    }
  } catch (error) {
    failure = error instanceof CheckError ? error.message : 'unexpected_runner_error';
    record({ step: 'failure', status: 'FAIL', code: failure });
  } finally {
    const report = {
      runId, mode: ['dry-run', 'auth', 'preflight', 'real'].includes(mode) ? mode : 'INVALID',
      host, expectedRelease: release, status: failure ? 'FAIL' : 'PASS', events,
    };
    try {
      await writeFile(resolve(results, `real-${runId}.json`), `${JSON.stringify(report, null, 2)}\n`, { mode: 0o600 });
    } catch {
      process.stdout.write(`RoadGuard E2E ${runId} FAIL report_write_failed\n`);
      throw new Error('report_write_failed');
    }
    process.stdout.write(`RoadGuard E2E ${runId} ${failure ? `FAIL ${failure}` : 'PASS'}\n`);
  }
  if (failure) throw new Error(failure);
});
