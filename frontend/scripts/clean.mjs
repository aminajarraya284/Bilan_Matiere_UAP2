import { rm } from 'node:fs/promises';
import { resolve } from 'node:path';

for (const target of ['dist', 'server.js']) {
  await rm(resolve(target), { recursive: true, force: true });
}
