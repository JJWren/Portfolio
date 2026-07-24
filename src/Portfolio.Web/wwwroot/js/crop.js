// Admin header-image crop tool (rebuilt for #40): the chosen image renders
// in a stage and a movable, corner-resizable crop box locked to 16:9 sits
// on top, with zoom (slider / wheel) and pan, rule-of-thirds guides, and
// live previews of what the card and the post hero will actually show.
// Apply bakes the boxed region onto a canvas and hands the result to the
// hidden Blazor InputFile, so the normal upload path (ImageUploadService)
// still validates and stores the file. Loaded as an ES module per circuit
// from OnAfterRenderAsync via JS interop `import` — tying the fetch to a
// live circuit, not to the document's original script tags — so a tab that
// outlives a redeploy re-fetches the current module (#37). The import URL
// must be "./"-prefixed (see JsModuleUrl); a bare fingerprinted path is a
// bare module specifier the browser rejects before any request (#40).

// Post heroes render width-capped at 420px tall (app.css .post-hero-image)
// inside the 1080px container: at desktop width that shows the middle
// 420 / (1080 * 9 / 16) of a 16:9 crop. Narrower viewports show more.
var HERO_BAND_FRACTION = 420 / (1080 * 9 / 16);

var ASPECT = 16 / 9;

// Output is capped at 1920×1080 and never upscaled.
var OUTPUT_MAX_WIDTH = 1920;

// Smallest crop box edge on the stage, in CSS pixels — keeps the handles
// grabbable and the selection meaningful.
var MIN_BOX_WIDTH = 90;

var MAX_ZOOM = 4;

// Vector / animated sources skip the crop: canvas would rasterize an SVG
// and freeze a GIF to its first frame.
var PASS_THROUGH = ['.svg', '.gif'];

// prefix → { show } registries so open() can reuse an initialized editor.
// Module-scoped: the browser caches the module per document, so both
// editors share one registry.
var tools = {};

function extensionOf(name) {
    var i = name.lastIndexOf('.');
    return i < 0 ? '' : name.slice(i).toLowerCase();
}

export function init(prefix, maxBytes) {
    var source = document.getElementById(prefix + '-crop-source');
    var target = document.getElementById(prefix + '-crop-target');
    var panel = document.getElementById(prefix + '-crop-panel');
    var stage = document.getElementById(prefix + '-crop-stage');
    var image = document.getElementById(prefix + '-crop-image');
    var frame = document.getElementById(prefix + '-crop-frame');
    var zoom = document.getElementById(prefix + '-crop-zoom');
    var zoomValue = document.getElementById(prefix + '-crop-zoom-value');
    var readout = document.getElementById(prefix + '-crop-readout');
    var cardPreview = document.getElementById(prefix + '-crop-card-preview');
    var applyButton = document.getElementById(prefix + '-crop-apply');
    var fullButton = document.getElementById(prefix + '-crop-full');
    var cancelButton = document.getElementById(prefix + '-crop-cancel');
    if (!source || !target || !panel || !stage || !image || !frame || !zoom
        || !zoomValue || !readout || !cardPreview
        || !applyButton || !fullButton || !cancelButton) {
        // Throwing surfaces a JSException to the invoking component so it
        // renders its plain-uploader fallback instead of a dead field.
        throw new Error('cropTool: missing crop element for prefix "' + prefix + '"');
    }

    // Post editors also carry a hero-band guide and hero preview; the
    // project editor has neither. Their absence just skips that work.
    var heroGuide = frame.querySelector('.crop-hero-guide');
    var heroPreview = document.getElementById(prefix + '-crop-hero-preview');
    if (heroGuide) {
        heroGuide.style.height = (HERO_BAND_FRACTION * 100).toFixed(2) + '%';
    }

    // Re-running init must not stack duplicate listeners — neither a
    // retried interop call from this module nor a leftover binding from a
    // previous module generation (a surviving tab can import a new
    // fingerprint whose module scope cannot reach the old one's closures).
    // The controller rides on the element so any generation can abort its
    // predecessor's listeners before binding fresh.
    if (source.__cropAbort) {
        source.__cropAbort.abort();
    }
    var abort = new AbortController();
    source.__cropAbort = abort;
    var listen = { signal: abort.signal };

    var current = null;  // { file, url }
    var natural = { width: 0, height: 0 };
    var minScale = 1;    // scale at which the image fits inside the stage (zoom 1)
    var scale = 1;       // current stage pixels per source pixel
    var offset = { x: 0, y: 0 };            // image top-left in stage coords
    var box = { x: 0, y: 0, width: 0, height: 0 }; // crop box in stage coords
    var gesture = null;
    var previewQueued = false;

    function stageSize() {
        var rect = stage.getBoundingClientRect();
        return { width: rect.width, height: rect.height };
    }

    function zoomFactor() {
        return scale / minScale;
    }

    function reset() {
        if (current) {
            URL.revokeObjectURL(current.url);
            current = null;
        }
        gesture = null;
        image.removeAttribute('src');
        // Visibility is a class, not the hidden attribute: the UA's
        // [hidden] { display: none } loses to the author display rule.
        panel.classList.remove('open');
        source.value = '';
        zoom.value = '1';
        zoomValue.textContent = '1.00×';
        readout.textContent = '';
    }

    // Feeds a file into the hidden Blazor InputFile; the synthetic change
    // event runs the component's normal OnChange upload handler.
    function handOff(file) {
        var transfer = new DataTransfer();
        transfer.items.add(file);
        target.files = transfer.files;
        target.dispatchEvent(new Event('change', { bubbles: true }));
        reset();
    }

    // The crop box in source-image pixels.
    function selection() {
        return {
            x: (box.x - offset.x) / scale,
            y: (box.y - offset.y) / scale,
            width: box.width / scale,
            height: box.height / scale
        };
    }

    function outputSize(sel) {
        var width = Math.floor(Math.min(OUTPUT_MAX_WIDTH, sel.width));
        return { width: width, height: Math.floor(width * 9 / 16) };
    }

    // Keeps the box 16:9, at least MIN_BOX_WIDTH wide where possible, and
    // inside both the displayed image and the stage.
    function clampBox() {
        var size = stageSize();
        var maxWidth = Math.min(
            natural.width * scale,
            natural.height * scale * ASPECT,
            size.width,
            size.height * ASPECT);
        box.width = Math.max(Math.min(box.width, maxWidth), Math.min(MIN_BOX_WIDTH, maxWidth));
        box.height = box.width / ASPECT;
        var loX = Math.max(offset.x, 0);
        var hiX = Math.min(offset.x + natural.width * scale, size.width) - box.width;
        var loY = Math.max(offset.y, 0);
        var hiY = Math.min(offset.y + natural.height * scale, size.height) - box.height;
        box.x = Math.min(Math.max(box.x, loX), Math.max(loX, hiX));
        box.y = Math.min(Math.max(box.y, loY), Math.max(loY, hiY));
    }

    // The image must always cover the crop box.
    function clampImage() {
        offset.x = Math.min(box.x, Math.max(offset.x, box.x + box.width - natural.width * scale));
        offset.y = Math.min(box.y, Math.max(offset.y, box.y + box.height - natural.height * scale));
    }

    function layout() {
        if (!current) {
            return;
        }
        clampBox();
        clampImage();
        image.style.width = (natural.width * scale) + 'px';
        image.style.height = (natural.height * scale) + 'px';
        image.style.left = offset.x + 'px';
        image.style.top = offset.y + 'px';
        frame.style.left = box.x + 'px';
        frame.style.top = box.y + 'px';
        frame.style.width = box.width + 'px';
        frame.style.height = box.height + 'px';
        var factor = zoomFactor();
        stage.classList.toggle('pannable', factor > 1.001);
        zoom.value = String(factor);
        zoomValue.textContent = factor.toFixed(2) + '×';
        if (!previewQueued) {
            previewQueued = true;
            requestAnimationFrame(function () {
                previewQueued = false;
                renderPreviews();
            });
        }
    }

    function renderPreviews() {
        if (!current) {
            return;
        }
        var sel = selection();
        var out = outputSize(sel);
        readout.textContent =
            'selection ' + Math.round(sel.width) + '×' + Math.round(sel.height)
            + 'px → saves ' + out.width + '×' + out.height
            + ' (cap 1920×1080 — never upscaled)';
        drawPreview(cardPreview, sel.x, sel.y, sel.width, sel.height, ASPECT);
        if (heroPreview) {
            var bandHeight = sel.height * HERO_BAND_FRACTION;
            drawPreview(
                heroPreview,
                sel.x, sel.y + (sel.height - bandHeight) / 2,
                sel.width, bandHeight,
                1080 / 420);
        }
    }

    function drawPreview(canvas, sourceX, sourceY, sourceWidth, sourceHeight, aspect) {
        var cssWidth = canvas.clientWidth;
        if (!cssWidth) {
            return;
        }
        var cssHeight = cssWidth / aspect;
        var ratio = window.devicePixelRatio || 1;
        canvas.style.height = cssHeight + 'px';
        canvas.width = Math.round(cssWidth * ratio);
        canvas.height = Math.round(cssHeight * ratio);
        var context = canvas.getContext('2d');
        if (!context) {
            return;
        }
        context.imageSmoothingQuality = 'high';
        context.drawImage(
            image, sourceX, sourceY, sourceWidth, sourceHeight,
            0, 0, canvas.width, canvas.height);
    }

    // Fits the freshly decoded image into the stage and centers the largest
    // 16:9 box the displayed image can hold.
    function fit() {
        var size = stageSize();
        minScale = Math.min(size.width / natural.width, size.height / natural.height);
        scale = minScale;
        offset.x = (size.width - natural.width * scale) / 2;
        offset.y = (size.height - natural.height * scale) / 2;
        box.width = natural.width * scale;
        box.height = box.width / ASPECT;
        if (box.height > natural.height * scale) {
            box.height = natural.height * scale;
            box.width = box.height * ASPECT;
        }
        box.x = offset.x + (natural.width * scale - box.width) / 2;
        box.y = offset.y + (natural.height * scale - box.height) / 2;
        layout();
    }

    // Stage size changed (viewport resize): keep the same source selection
    // and zoom factor, re-centered on the selection.
    function refit() {
        if (!current) {
            return;
        }
        var sel = selection();
        var factor = zoomFactor();
        var size = stageSize();
        minScale = Math.min(size.width / natural.width, size.height / natural.height);
        scale = minScale * factor;
        offset.x = size.width / 2 - (sel.x + sel.width / 2) * scale;
        offset.y = size.height / 2 - (sel.y + sel.height / 2) * scale;
        box.width = sel.width * scale;
        box.height = box.width / ASPECT;
        box.x = offset.x + sel.x * scale;
        box.y = offset.y + sel.y * scale;
        layout();
    }

    // Changes zoom while keeping the stage point (focusX, focusY) still;
    // defaults to the box center so the selection stays anchored.
    function setZoom(factor, focusX, focusY) {
        if (!current) {
            return;
        }
        factor = Math.min(MAX_ZOOM, Math.max(1, factor));
        if (focusX === undefined) {
            focusX = box.x + box.width / 2;
            focusY = box.y + box.height / 2;
        }
        var next = minScale * factor;
        offset.x = focusX - (focusX - offset.x) * (next / scale);
        offset.y = focusY - (focusY - offset.y) * (next / scale);
        scale = next;
        if (factor === 1) {
            var size = stageSize();
            offset.x = (size.width - natural.width * scale) / 2;
            offset.y = (size.height - natural.height * scale) / 2;
        }
        layout();
    }

    // Loads a raster file into the stage. The panel must be open before
    // measuring: a display:none stage has no size and the fit math would
    // collapse to zero.
    function show(file) {
        if (current) {
            URL.revokeObjectURL(current.url);
        }
        current = { file: file, url: URL.createObjectURL(file) };
        var loadedUrl = current.url;
        image.onload = function () {
            // No-op when the load is stale: Cancel may have reset the
            // tool (or a newer pick replaced the image) before this
            // decode completed — fit() must not run against null.
            if (!current || current.url !== loadedUrl) {
                return;
            }
            natural.width = image.naturalWidth;
            natural.height = image.naturalHeight;
            panel.classList.add('open');
            fit();
        };
        image.onerror = reset;
        image.src = current.url;
    }

    source.addEventListener('change', function () {
        var file = source.files && source.files[0];
        if (!file) {
            return;
        }
        if (PASS_THROUGH.indexOf(extensionOf(file.name)) >= 0) {
            handOff(file);
            return;
        }
        show(file);
    }, listen);

    zoom.addEventListener('input', function () {
        setZoom(parseFloat(zoom.value));
    }, listen);

    stage.addEventListener('wheel', function (event) {
        if (!current) {
            return;
        }
        event.preventDefault();
        var rect = stage.getBoundingClientRect();
        setZoom(zoomFactor() * (event.deltaY < 0 ? 1.1 : 1 / 1.1),
            event.clientX - rect.left, event.clientY - rect.top);
    }, { passive: false, signal: abort.signal });

    stage.addEventListener('pointerdown', function (event) {
        if (!current) {
            return;
        }
        event.preventDefault();
        var rect = stage.getBoundingClientRect();
        var x = event.clientX - rect.left;
        var y = event.clientY - rect.top;
        var handle = event.target.closest ? event.target.closest('.crop-handle') : null;
        if (handle) {
            var corner = handle.getAttribute('data-h');
            gesture = {
                kind: 'resize',
                west: corner.indexOf('w') >= 0,
                north: corner.indexOf('n') >= 0,
                // The dragged corner moves; the opposite corner stays put.
                anchorX: corner.indexOf('w') >= 0 ? box.x + box.width : box.x,
                anchorY: corner.indexOf('n') >= 0 ? box.y + box.height : box.y
            };
        } else if (x >= box.x && x <= box.x + box.width && y >= box.y && y <= box.y + box.height) {
            gesture = { kind: 'move', dx: x - box.x, dy: y - box.y };
        } else {
            gesture = { kind: 'pan', dx: x - offset.x, dy: y - offset.y };
            stage.classList.add('panning');
        }
        stage.setPointerCapture(event.pointerId);
    }, listen);

    stage.addEventListener('pointermove', function (event) {
        if (!current || !gesture) {
            return;
        }
        var rect = stage.getBoundingClientRect();
        var x = event.clientX - rect.left;
        var y = event.clientY - rect.top;
        if (gesture.kind === 'move') {
            box.x = x - gesture.dx;
            box.y = y - gesture.dy;
        } else if (gesture.kind === 'pan') {
            offset.x = x - gesture.dx;
            offset.y = y - gesture.dy;
        } else {
            // Aspect-locked resize: grow toward the cursor from the anchored
            // corner, capped by the image and stage edges on that side.
            var size = stageSize();
            var availWidth = gesture.west
                ? gesture.anchorX - Math.max(offset.x, 0)
                : Math.min(offset.x + natural.width * scale, size.width) - gesture.anchorX;
            var availHeight = gesture.north
                ? gesture.anchorY - Math.max(offset.y, 0)
                : Math.min(offset.y + natural.height * scale, size.height) - gesture.anchorY;
            var wantWidth = gesture.west ? gesture.anchorX - x : x - gesture.anchorX;
            var wantHeight = gesture.north ? gesture.anchorY - y : y - gesture.anchorY;
            var width = Math.max(wantWidth, wantHeight * ASPECT, MIN_BOX_WIDTH);
            width = Math.min(width, availWidth, availHeight * ASPECT);
            box.width = width;
            box.height = width / ASPECT;
            box.x = gesture.west ? gesture.anchorX - box.width : gesture.anchorX;
            box.y = gesture.north ? gesture.anchorY - box.height : gesture.anchorY;
        }
        layout();
    }, listen);

    function endGesture() {
        gesture = null;
        stage.classList.remove('panning');
    }
    stage.addEventListener('pointerup', endGesture, listen);
    stage.addEventListener('pointercancel', endGesture, listen);

    window.addEventListener('resize', refit, listen);

    applyButton.addEventListener('click', function () {
        if (!current) {
            return;
        }
        var sel = selection();
        var out = outputSize(sel);
        // Degenerate sources (an image a pixel or two wide) can floor the
        // 16:9 output to zero; upload the original instead of an empty crop.
        if (out.width < 1 || out.height < 1) {
            fallbackToOriginal();
            return;
        }
        var canvas = document.createElement('canvas');
        canvas.width = out.width;
        canvas.height = out.height;
        var context = canvas.getContext('2d');
        if (!context) {
            fallbackToOriginal();
            return;
        }
        // The browser decodes EXIF orientation into the <img>, so drawing
        // from it keeps preview and output consistent.
        context.imageSmoothingQuality = 'high';
        context.drawImage(
            image, sel.x, sel.y, sel.width, sel.height, 0, 0, out.width, out.height);
        var type = current.file.type === 'image/jpeg' || current.file.type === 'image/webp'
            ? current.file.type
            : 'image/png';
        var baseName = current.file.name.replace(/\.[^.]+$/, '');
        encode(canvas, type, function (blob, finalType) {
            var extension = finalType === 'image/jpeg' ? '.jpg'
                : finalType === 'image/webp' ? '.webp'
                : '.png';
            handOff(new File([blob], baseName + extension, { type: finalType }));
        });
    }, listen);

    function encode(canvas, type, done) {
        canvas.toBlob(function (blob) {
            if (!blob) {
                fallbackToOriginal();
                return;
            }
            // Canvas PNGs of photographic crops can outgrow the upload
            // cap; fall back to JPEG rather than let the server reject it.
            if (type === 'image/png' && maxBytes && blob.size > maxBytes) {
                canvas.toBlob(function (jpeg) {
                    if (jpeg) {
                        done(jpeg, 'image/jpeg');
                    } else {
                        fallbackToOriginal();
                    }
                }, 'image/jpeg', 0.9);
                return;
            }
            done(blob, type);
        }, type, 0.9);
    }

    // Canvas export failed: upload the original uncropped rather than
    // leaving the Apply click with no effect.
    function fallbackToOriginal() {
        if (current) {
            handOff(current.file);
        }
    }

    fullButton.addEventListener('click', function () {
        if (!current) {
            return;
        }
        handOff(current.file);
    }, listen);

    cancelButton.addEventListener('click', reset, listen);

    // Clear anything a displaced generation left behind (open panel, image
    // src, pending source value) — its state is unreachable, but its DOM
    // leftovers are ours now. No-op on freshly rendered elements.
    reset();

    tools[prefix] = { show: show };
}

// Loads an already-stored same-origin image (e.g. /uploads/...) into the
// crop tool so its framing can be adjusted and re-saved. Returns the fetch
// promise so .NET interop surfaces failures as JSException.
export function open(prefix, url) {
    var tool = tools[prefix];
    if (!tool) {
        throw new Error('cropTool: not initialized for prefix "' + prefix + '"');
    }
    // Query strings and fragments are not part of the file name — a
    // cache-busted "x.svg?v=1" must still pass through.
    var path = url.split('#')[0].split('?')[0];
    if (PASS_THROUGH.indexOf(extensionOf(path)) >= 0) {
        return Promise.resolve();
    }
    return fetch(url)
        .then(function (response) {
            if (!response.ok) {
                throw new Error('cropTool: stored image fetch failed (' + response.status + ')');
            }
            return response.blob();
        })
        .then(function (blob) {
            var name = path.split('/').pop() || 'image';
            tool.show(new File([blob], name, { type: blob.type }));
        });
}
