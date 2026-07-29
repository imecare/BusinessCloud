import {bundle} from '@remotion/bundler';
import {renderMedia, selectComposition} from '@remotion/renderer';
import fs from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';

const [, , propsFile, outputFile, compositionId = 'BazarPromo'] = process.argv;

if (!propsFile || !outputFile) {
  throw new Error('Usage: node render.mjs <props.json> <output.mp4>');
}

const inputProps = JSON.parse(await fs.readFile(propsFile, 'utf8'));
const serveUrl = await bundle({
  entryPoint: path.resolve('src/index.ts'),
  publicDir: path.resolve('public'),
});
const composition = await selectComposition({
  serveUrl,
  id: compositionId,
  inputProps,
});

await renderMedia({
  composition,
  serveUrl,
  codec: 'h264',
  outputLocation: path.resolve(outputFile),
  inputProps,
  crf: 18,
  imageFormat: 'jpeg',
});
