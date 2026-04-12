import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const projectRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const specUrl =
  process.env.OPENAPI_SPEC_URL ?? 'http://host.docker.internal:5088/swagger/v1/swagger.json';
const image = 'openapitools/openapi-generator-cli:v7.21.0';

const result = spawnSync(
  'docker',
  [
    'run',
    '--rm',
    '-v',
    `${projectRoot.replace(/\\/g, '/')}:/local`,
    image,
    'generate',
    '-i',
    specUrl,
    '-g',
    'typescript-angular',
    '-o',
    '/local/src/app/api/generated',
    '--additional-properties=ngVersion=19.0.0,providedInRoot=true,stringEnums=true',
  ],
  { stdio: 'inherit', shell: false }
);

process.exit(result.status ?? 1);
