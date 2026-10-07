import { build } from 'esbuild';
import pbjs from 'protobufjs-cli/pbjs.js';
import { writeFileSync } from 'node:fs';
// Static serialization avoids protobuf reflection's runtime Function() calls under CSP.
const generated = await new Promise((resolve,reject) => pbjs.main(['-t','static-module','-w','es6',
  '--no-service','--no-verify','--no-convert','--no-delimited','--no-comments',
  '../../src/AndroidDesktop/Protocols/emulator_controller.proto'], (error, output) => error ? reject(error) : resolve(output)));
writeFileSync('emulator.js', generated);
await build({entryPoints:['audio-worklet.mjs'],outfile:'../../src/AndroidDesktop/Assets/Viewport/audio-worklet.js',minify:true,target:'chrome120'});
await build({ entryPoints: ['viewport.mjs'], bundle: true, minify: true,
  outfile: '../../src/AndroidDesktop/Assets/Viewport/viewport.js', format: 'iife', target: 'chrome120', legalComments: 'eof' });
