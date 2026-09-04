// Assembles the .dc.html artboards from shared parts so the token set,
// header and footer stay identical across directions.
import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const part = (f) => readFileSync(join(here, 'parts', f), 'utf8');

const shared = part('shared.css');
const header = part('header.html');
const footer = part('footer.html');
const FONTS = '<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Fraunces:wght@600;700&amp;family=JetBrains+Mono:wght@400;500&amp;family=Public+Sans:wght@400;500;700&amp;display=swap">';

const THEME = { editor: 'enum', options: ['dark', 'light'], default: 'dark', section: 'Theme' };
const DEGREES = { editor: 'int', min: 0, max: 6, default: 0, section: 'Belt' };
const BELT_RED = { editor: 'color', options: ['#a63d40', '#c8102e'], default: '#a63d40', section: 'Belt' };
const LEAD_CSS = ['Main.css', 'GamePlan.css', 'LongRoad.css', 'Lead.css'];
const LEAD_PROPS = { theme: THEME, degrees: DEGREES, beltRed: BELT_RED };

const boards = [
  { name: 'Main', body: 'Main.body.html', css: LEAD_CSS, script: 'Main.js', props: LEAD_PROPS, size: [1440, 2650] },
  { name: 'MainMobile', body: 'Main.body.html', css: LEAD_CSS, script: 'Main.js', props: LEAD_PROPS, size: [390, 4500] },
  { name: 'QuietBelt', body: 'QuietBelt.body.html', css: ['Main.css'], script: 'Main.js', props: LEAD_PROPS, size: [1440, 1750] },
  { name: 'Baseline', body: 'Baseline.body.html', css: [], script: 'Theme.js', props: { theme: THEME }, size: [1440, 1300] },
  { name: 'GamePlan', body: 'GamePlan.body.html', css: ['GamePlan.css'], script: 'Theme.js', props: { theme: THEME }, size: [1440, 2350] },
  { name: 'LongRoad', body: 'LongRoad.body.html', css: ['LongRoad.css'], script: 'Theme.js', props: { theme: THEME, degrees: DEGREES }, size: [1440, 1950] },
  { name: 'MatRoom', body: 'MatRoom.body.html', css: ['MatRoom.css'], script: 'Theme.js', props: { theme: THEME }, size: [1440, 1550] },
];

for (const b of boards) {
  const props = { ...b.props, $preview: { width: b.size[0], height: b.size[1] } };
  const propsJson = JSON.stringify(props).replace(/&/g, '&amp;').replace(/'/g, '&#39;');
  const extra = b.css.map(part).join('\n');
  const html = `<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <script src="./support.js"></script>
</head>
<body>
<x-dc>
<helmet>
  ${FONTS}
  <style>
${shared}
${extra}
  </style>
</helmet>
<div class="site" data-theme="{{theme}}">
${header}
${part(b.body)}
${footer}
</div>
</x-dc>
<script data-dc-script data-props='${propsJson}'>
${part(b.script)}
</script>
</body>
</html>
`;
  writeFileSync(join(here, `${b.name}.dc.html`), html);
  console.log(`${b.name}.dc.html ${html.length} bytes`);
}
