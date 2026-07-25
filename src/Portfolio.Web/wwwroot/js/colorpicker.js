// Admin theme-editor color picker: a native <dialog> with an HSV
// saturation/brightness square (draggable circular thumb), a hue slider, and
// a live hex readout. Apply writes the chosen color into the token's hex
// <input> and dispatches a synthetic bubbling `input` event so the editor's
// normal @oninput handler runs — the same hand-off crop.js uses for its
// hidden InputFile. No .NET calls happen from here, and open() returns as
// soon as showModal() runs, so no interop call sits pending (and can time
// out) while the dialog stays open. Loaded as an ES module per circuit from
// OnAfterRenderAsync via JS interop `import` (see crop.js for why: a
// document script tag would strand tabs across redeploys, and the import
// URL must be "./"-prefixed — JsModuleUrl).

// Shared HSV state; the module is cached per document, so one dialog and one
// registry suffice (the crop.js `tools` idiom, without prefixes).
var state = { h: 0, s: 0, v: 1, targetId: null };
var picker = null; // { dialog, seed } — set by init()

function clamp01(value) {
    return Math.min(1, Math.max(0, value));
}

function hexToRgb(hex) {
    var match = /^#([0-9a-f]{3}|[0-9a-f]{6})$/i.exec(hex.trim());
    if (!match) {
        return null;
    }
    var digits = match[1];
    if (digits.length === 3) {
        digits = digits[0] + digits[0] + digits[1] + digits[1] + digits[2] + digits[2];
    }
    return {
        r: parseInt(digits.slice(0, 2), 16),
        g: parseInt(digits.slice(2, 4), 16),
        b: parseInt(digits.slice(4, 6), 16)
    };
}

function rgbToHex(r, g, b) {
    return '#' + [r, g, b].map(function (channel) {
        return channel.toString(16).padStart(2, '0');
    }).join('');
}

function rgbToHsv(r, g, b) {
    r /= 255; g /= 255; b /= 255;
    var max = Math.max(r, g, b);
    var min = Math.min(r, g, b);
    var delta = max - min;
    var h = 0;
    if (delta > 0) {
        if (max === r) {
            h = 60 * (((g - b) / delta) % 6);
        } else if (max === g) {
            h = 60 * (((b - r) / delta) + 2);
        } else {
            h = 60 * (((r - g) / delta) + 4);
        }
    }
    if (h < 0) {
        h += 360;
    }
    return { h: h, s: max === 0 ? 0 : delta / max, v: max };
}

function hsvToRgb(h, s, v) {
    var c = v * s;
    var x = c * (1 - Math.abs(((h / 60) % 2) - 1));
    var m = v - c;
    var r = 0, g = 0, b = 0;
    if (h < 60) { r = c; g = x; }
    else if (h < 120) { r = x; g = c; }
    else if (h < 180) { g = c; b = x; }
    else if (h < 240) { g = x; b = c; }
    else if (h < 300) { r = x; b = c; }
    else { r = c; b = x; }
    return {
        r: Math.round((r + m) * 255),
        g: Math.round((g + m) * 255),
        b: Math.round((b + m) * 255)
    };
}

export function init(dialogId) {
    var dialog = document.getElementById(dialogId);
    if (!dialog) {
        // Throwing surfaces a JSException to the invoking component so it
        // disables the swatch buttons and leaves hex editing as the path.
        throw new Error('colorPicker: missing dialog "' + dialogId + '"');
    }
    var sv = dialog.querySelector('.picker-sv');
    var thumb = dialog.querySelector('.picker-thumb');
    var hue = dialog.querySelector('.picker-hue');
    var swatch = dialog.querySelector('.picker-swatch');
    var hexInput = dialog.querySelector('.picker-hex');
    var applyButton = dialog.querySelector('.picker-apply');
    var cancelButton = dialog.querySelector('.picker-cancel');
    if (!sv || !thumb || !hue || !swatch || !hexInput || !applyButton || !cancelButton) {
        throw new Error('colorPicker: missing picker element');
    }

    // Re-running init must not stack duplicate listeners — neither a retried
    // interop call nor a leftover binding from a previous module generation.
    // The controller rides on the element so any generation can abort its
    // predecessor's listeners before binding fresh (the crop.js idiom).
    if (dialog.__pickerAbort) {
        dialog.__pickerAbort.abort();
    }
    var abort = new AbortController();
    dialog.__pickerAbort = abort;
    var listen = { signal: abort.signal };

    function currentHex() {
        var rgb = hsvToRgb(state.h, state.s, state.v);
        return rgbToHex(rgb.r, rgb.g, rgb.b);
    }

    // skipHex leaves the hex field alone while the user is typing in it, so
    // a parseable prefix ("#abc" on the way to "#abcdef") isn't clobbered.
    function paint(skipHex) {
        sv.style.setProperty('--picker-hue', String(state.h));
        thumb.style.left = (state.s * 100).toFixed(1) + '%';
        thumb.style.top = ((1 - state.v) * 100).toFixed(1) + '%';
        hue.value = String(Math.round(state.h));
        var hex = currentHex();
        swatch.style.background = hex;
        thumb.style.background = hex;
        if (!skipHex) {
            hexInput.value = hex;
        }
        // Slider semantics need a numeric value; brightness (the vertical
        // axis, 0-100 per the markup's min/max) is the fallback number, while
        // aria-valuetext announces both axes and takes precedence when read.
        sv.setAttribute('aria-valuenow', String(Math.round(state.v * 100)));
        sv.setAttribute('aria-valuetext',
            'Saturation ' + Math.round(state.s * 100) + '%, brightness ' + Math.round(state.v * 100) + '%');
    }

    function seed(hex) {
        var rgb = hexToRgb(hex) || { r: 128, g: 128, b: 128 };
        var hsv = rgbToHsv(rgb.r, rgb.g, rgb.b);
        state.h = hsv.h;
        state.s = hsv.s;
        state.v = hsv.v;
        paint();
    }

    function updateFromPointer(event) {
        var rect = sv.getBoundingClientRect();
        state.s = clamp01((event.clientX - rect.left) / rect.width);
        state.v = clamp01(1 - ((event.clientY - rect.top) / rect.height));
        paint();
    }

    var dragging = false;
    sv.addEventListener('pointerdown', function (event) {
        event.preventDefault();
        // preventDefault also suppresses the browser's native focus-on-press,
        // so focus explicitly (without scrolling the dialog) — the arrow-key
        // nudging must work immediately after a pointer interaction.
        sv.focus({ preventScroll: true });
        dragging = true;
        sv.setPointerCapture(event.pointerId);
        updateFromPointer(event);
    }, listen);
    sv.addEventListener('pointermove', function (event) {
        if (dragging) {
            updateFromPointer(event);
        }
    }, listen);
    function endDrag() {
        dragging = false;
    }
    sv.addEventListener('pointerup', endDrag, listen);
    sv.addEventListener('pointercancel', endDrag, listen);

    // Keyboard operation for the slider-role square: arrows nudge by 1%,
    // Shift+arrows by 10%.
    sv.addEventListener('keydown', function (event) {
        var step = event.shiftKey ? 0.1 : 0.01;
        var handled = true;
        if (event.key === 'ArrowLeft') { state.s = clamp01(state.s - step); }
        else if (event.key === 'ArrowRight') { state.s = clamp01(state.s + step); }
        else if (event.key === 'ArrowUp') { state.v = clamp01(state.v + step); }
        else if (event.key === 'ArrowDown') { state.v = clamp01(state.v - step); }
        else { handled = false; }
        if (handled) {
            event.preventDefault();
            paint();
        }
    }, listen);

    hue.addEventListener('input', function () {
        state.h = parseFloat(hue.value) || 0;
        paint();
    }, listen);

    hexInput.addEventListener('input', function () {
        var rgb = hexToRgb(hexInput.value);
        if (rgb) {
            var hsv = rgbToHsv(rgb.r, rgb.g, rgb.b);
            state.h = hsv.h;
            state.s = hsv.s;
            state.v = hsv.v;
            paint(true);
        }
    }, listen);

    applyButton.addEventListener('click', function () {
        var target = state.targetId ? document.getElementById(state.targetId) : null;
        if (target) {
            target.value = currentHex();
            // The synthetic event runs the editor's @oninput handler, which
            // revalidates and refreshes swatch, preview, and warnings —
            // identical to typing the value.
            target.dispatchEvent(new Event('input', { bubbles: true }));
        }
        dialog.close();
    }, listen);

    cancelButton.addEventListener('click', function () {
        dialog.close();
    }, listen);
    // Esc is handled natively: the dialog's cancel behavior closes it.

    picker = { dialog: dialog, seed: seed };
}

export function open(hex, targetInputId) {
    if (!picker) {
        throw new Error('colorPicker: not initialized');
    }
    if (picker.dialog.open) {
        // Re-entrant open (a double-clicked swatch): the dialog is already
        // up with this token's color — a second showModal() would throw
        // InvalidStateError and surface as a spurious JSException.
        return;
    }
    state.targetId = targetInputId;
    picker.seed(hex);
    // showModal gives focus containment, an inert background, Esc-to-close,
    // and focus restoration to the opening swatch on close.
    picker.dialog.showModal();
}
