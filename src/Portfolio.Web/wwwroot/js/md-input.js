// Live markdown mirror for composer textareas (Discord-style feedback).
// Paints a styled copy of the textarea's markdown into a backdrop div while
// the textarea keeps native input behavior (typing, IME, undo, maxlength).
// Both layers share identical glyph metrics — the mono font keeps advance
// widths fixed even under bold/italic — so the invisible textarea text and
// the styled copy stay caret-aligned. Display-only approximation: the server
// pipeline (MarkdownService.ToSafeHtml) remains the renderer of record.

const FENCE_OPEN = /^ {0,3}(`{3,}|~{3,})[ \t]*(\S*)/;
const FENCE_CLOSE = /^ {0,3}(`{3,}|~{3,})[ \t]*$/;
const HEADING = /^( {0,3}#{1,6}[ \t])([\s\S]*)$/;
const QUOTE = /^( {0,3}>[ \t]?)([\s\S]*)$/;
const LIST = /^([ \t]{0,8}(?:[-*+]|\d{1,9}[.)])[ \t])([\s\S]*)$/;
// Alternatives, first match wins: inline code, bold, strikethrough, italic,
// [label](url) link, bare URL. Code is first so its content suppresses the
// other markers, mirroring how the real pipeline treats code spans.
const INLINE = /(`+)([\s\S]*?)\1|(\*\*|__)((?:(?!\3)[\s\S])+?)\3|(~~)([\s\S]+?)\5|([*_])((?:(?!\7)[\s\S])+?)\7|\[([^\]\n]*)\]\(([^)\s]*)\)|(https?:\/\/[^\s<>]+)/g;

const instances = new Map();

function esc(text) {
    return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function syn(text) {
    return '<span class="mdt-syntax">' + esc(text) + '</span>';
}

// One level of nesting keeps common shapes styled (bold inside a link
// label, code inside bold) without recursive-tokenizer complexity.
function nest(text, depth) {
    return depth > 0 ? inline(text, depth - 1) : esc(text);
}

function inline(text, depth) {
    let out = '';
    let last = 0;
    for (const m of text.matchAll(INLINE)) {
        out += esc(text.slice(last, m.index));
        last = m.index + m[0].length;
        if (m[1] !== undefined) {
            out += syn(m[1]) + '<span class="mdt-code">' + esc(m[2]) + '</span>' + syn(m[1]);
        } else if (m[3] !== undefined) {
            out += syn(m[3]) + '<span class="mdt-bold">' + nest(m[4], depth) + '</span>' + syn(m[3]);
        } else if (m[5] !== undefined) {
            out += syn(m[5]) + '<span class="mdt-strike">' + nest(m[6], depth) + '</span>' + syn(m[5]);
        } else if (m[7] !== undefined) {
            out += syn(m[7]) + '<span class="mdt-italic">' + nest(m[8], depth) + '</span>' + syn(m[7]);
        } else if (m[9] !== undefined) {
            out += syn('[') + '<span class="mdt-link">' + nest(m[9], depth) + '</span>'
                + syn('](') + '<span class="mdt-url">' + esc(m[10]) + '</span>' + syn(')');
        } else {
            out += '<span class="mdt-link">' + esc(m[11]) + '</span>';
        }
    }
    return out + esc(text.slice(last));
}

// The fence body highlights live whenever the vendored Prism bundle knows
// the tag — Prism.languages doubles as the "common languages" registry
// (js, ts, cs, py, sh, yml, html, css, sql, json, and their full names).
function flushFence(fence) {
    const body = fence.body.join('\n');
    const grammar = fence.lang && window.Prism ? window.Prism.languages[fence.lang] : null;
    let html;
    if (grammar) {
        try {
            html = window.Prism.highlight(body, grammar, fence.lang);
        } catch {
            html = esc(body);
        }
    } else {
        html = esc(body);
    }

    return '<span class="mdt-fence">' + html + '</span>';
}

function render(value) {
    const lines = value.split('\n');
    const out = [];
    let fence = null;
    for (const line of lines) {
        if (fence) {
            const close = line.match(FENCE_CLOSE);
            if (close && close[1][0] === fence.marker[0] && close[1].length >= fence.marker.length) {
                if (fence.body.length > 0) {
                    out.push(flushFence(fence));
                }
                out.push(syn(line));
                fence = null;
            } else {
                fence.body.push(line);
            }
            continue;
        }

        const open = line.match(FENCE_OPEN);
        if (open) {
            out.push(syn(line));
            fence = { marker: open[1], lang: (open[2] || '').toLowerCase(), body: [] };
            continue;
        }

        let m;
        if ((m = line.match(HEADING))) {
            out.push(syn(m[1]) + '<span class="mdt-heading">' + inline(m[2], 1) + '</span>');
        } else if ((m = line.match(QUOTE))) {
            out.push(syn(m[1]) + '<span class="mdt-quote">' + inline(m[2], 1) + '</span>');
        } else if ((m = line.match(LIST))) {
            out.push(syn(m[1]) + inline(m[2], 1));
        } else {
            out.push(inline(line, 1));
        }
    }

    // An unclosed fence styles to the end of the draft, Discord-style.
    if (fence && fence.body.length > 0) {
        out.push(flushFence(fence));
    }

    return out.join('\n');
}

// Binds the mirror inside the given wrapper. Re-init on the same id is
// safe: the previous binding is aborted first (reconnect hygiene).
export function init(id) {
    const root = document.getElementById(id);
    const textarea = root ? root.querySelector('textarea') : null;
    const mirror = root ? root.querySelector('.md-input-mirror') : null;
    if (!root || !textarea || !mirror) {
        throw new Error('md-input: missing elements for #' + id);
    }

    dispose(id);

    const controller = new AbortController();
    let pending = false;

    const sync = () => {
        mirror.scrollTop = textarea.scrollTop;
        mirror.scrollLeft = textarea.scrollLeft;
    };
    const paint = () => {
        pending = false;
        // A value ending in a newline shows a caret line in the textarea that
        // a pre-wrap div would collapse; the zero-width space keeps the line
        // box (and scrollHeight) identical without adding visible glyphs.
        mirror.innerHTML = render(textarea.value)
            + (textarea.value.endsWith('\n') ? '&#8203;' : '');
        sync();
    };
    const schedule = () => {
        if (pending) {
            return;
        }
        pending = true;
        requestAnimationFrame(paint);
    };

    textarea.addEventListener('input', schedule, { signal: controller.signal });
    // Some browsers withhold input events until composition ends; without
    // repaints during composition the transparent textarea would show
    // nothing while an IME user types.
    textarea.addEventListener('compositionupdate', schedule, { signal: controller.signal });
    textarea.addEventListener('compositionend', schedule, { signal: controller.signal });
    textarea.addEventListener('scroll', sync, { signal: controller.signal });
    instances.set(id, { controller, schedule });
    paint();
}

// Repaints after a programmatic value change (Blazor re-render), which
// fires no input event.
export function refresh(id) {
    const instance = instances.get(id);
    if (instance) {
        instance.schedule();
    }
}

export function dispose(id) {
    const instance = instances.get(id);
    if (instance) {
        instance.controller.abort();
        instances.delete(id);
    }
}
