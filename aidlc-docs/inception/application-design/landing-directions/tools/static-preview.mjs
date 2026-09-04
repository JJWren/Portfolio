// Static renders of the artboard parts (no DC runtime) for click-testing the
// CSS-driven interactions and both themes. Preview only; never published.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
const part = (f) => readFileSync('parts/' + f, 'utf8');
const shared = part('shared.css'), header = part('header.html'), footer = part('footer.html');
const FONTS = '<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Fraunces:wght@600;700&family=JetBrains+Mono:wght@400;500&family=Public+Sans:wght@400;500;700&display=swap">';
const LEAD = ['Main.css', 'GamePlan.css', 'LongRoad.css', 'Lead.css'];
const boards = [
  ['main', 'Main.body.html', LEAD], ['quietbelt', 'QuietBelt.body.html', ['Main.css']], ['baseline', 'Baseline.body.html', []],
  ['gameplan', 'GamePlan.body.html', ['GamePlan.css']], ['longroad', 'LongRoad.body.html', ['LongRoad.css']],
  ['matroom', 'MatRoom.body.html', ['MatRoom.css']],
];
mkdirSync('preview', { recursive: true });
for (const [name, body, css] of boards) {
  for (const theme of ['dark', 'light']) {
    let html = header + part(body) + footer;
    html = html
      .replace(/onClick="\{\{toggleTheme\}\}" title="\{\{toggleTitle\}\}"/g, 'title="Switch theme"')
      .replace(/defaultChecked="\{\{ on \}\}"/g, 'checked')
      .replace(/\{\{beltRed\}\}/g, '#a63d40')
      .replace(/<sc-for list="\{\{stripes\}\}" as="s" hint-placeholder-count="2"><i><\/i><\/sc-for>/g, '')
      .replace(/src="logo-sm\.png"/g, 'src="../logo-sm.png"')
      .replace(/src="josh-desk\.jpg"/g, 'src="../josh-desk.jpg"')
      .replace(/src="josh-mat\.jpg"/g, 'src="../josh-mat.jpg"');
    const extra = css.map(part).join('\n');
    const out = `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>${name} ${theme}</title>${FONTS}<style>${shared}\n${extra}\nhtml{scroll-behavior:auto !important}</style></head><body><div class="site" data-theme="${theme}">${html}</div></body></html>`;
    writeFileSync(`preview/static-${name}-${theme}.html`, out);
    console.log(`static-${name}-${theme}.html`);
  }
}
