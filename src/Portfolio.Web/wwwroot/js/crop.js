// Admin header-image crop tool: a fixed 16:9 window the admin zooms and pans
// a chosen image within. Apply bakes the framed region onto a canvas and
// hands the result to the hidden Blazor InputFile, so the normal upload path
// (ImageUploadService) still validates and stores the file. Loaded as an ES
// module by each editor from OnAfterRenderAsync via JS interop `import` —
// tying the fetch to a live circuit, not to the document's original script
// tags — so a tab that outlives a redeploy re-fetches the current module
// instead of keeping a dead global forever (#37).

// Post heroes render width-capped at 420px tall (app.css .post-hero-image)
// inside the 1080px container: at desktop width that shows the middle
// 420 / (1080 * 9 / 16) of a 16:9 crop. Narrower viewports show more.
var HERO_BAND_FRACTION = 420 / (1080 * 9 / 16);

// Vector / animated sources skip the crop: canvas would rasterize an SVG
// and freeze a GIF to its first frame.
var PASS_THROUGH = ['.svg', '.gif'];

// prefix → { show } registries so open() can reuse an initialized editor.
// Module-scoped: the browser caches the module per document, so both
// editors share one registry exactly as the old window global did.
var tools = {};

function extensionOf(name) {
    var i = name.lastIndexOf('.');
    return i < 0 ? '' : name.slice(i).toLowerCase();
}

export function init(prefix, maxBytes) {
    var source = document.getElementById(prefix + '-crop-source');
    var target = document.getElementById(prefix + '-crop-target');
    var panel = document.getElementById(prefix + '-crop-panel');
    var box = document.getElementById(prefix + '-crop-box');
    var image = document.getElementById(prefix + '-crop-image');
    var zoom = document.getElementById(prefix + '-crop-zoom');
    var applyButton = document.getElementById(prefix + '-crop-apply');
    var fullButton = document.getElementById(prefix + '-crop-full');
    var cancelButton = document.getElementById(prefix + '-crop-cancel');
    if (!source || !target || !panel || !box || !image || !zoom
        || !applyButton || !fullButton || !cancelButton) {
        // Throwing surfaces a JSException to the invoking component so it
        // renders its plain-uploader fallback instead of a dead field.
        throw new Error('cropTool: missing crop element for prefix "' + prefix + '"');
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

    var guide = panel.querySelector('.crop-hero-guide');
    if (guide) {
        guide.style.height = (HERO_BAND_FRACTION * 100).toFixed(2) + '%';
    }

    var current = null; // { file, url, scale, x, y } — x/y: image top-left in box pixels (<= 0)

    function reset() {
        if (current) {
            URL.revokeObjectURL(current.url);
            current = null;
        }
        image.removeAttribute('src');
        // Visibility is a class, not the hidden attribute: the UA's
        // [hidden] { display: none } loses to the author display rule.
        panel.classList.remove('open');
        source.value = '';
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

    // Scale at which the image exactly covers the box (slider value 1).
    function coverScale() {
        var rect = box.getBoundingClientRect();
        return Math.max(rect.width / image.naturalWidth, rect.height / image.naturalHeight);
    }

    function layout() {
        if (!current) {
            return;
        }
        var rect = box.getBoundingClientRect();
        var scale = coverScale() * current.scale;
        var width = image.naturalWidth * scale;
        var height = image.naturalHeight * scale;
        current.x = Math.min(0, Math.max(rect.width - width, current.x));
        current.y = Math.min(0, Math.max(rect.height - height, current.y));
        image.style.width = width + 'px';
        image.style.height = height + 'px';
        image.style.left = current.x + 'px';
        image.style.top = current.y + 'px';
    }

    function center() {
        var rect = box.getBoundingClientRect();
        var scale = coverScale() * current.scale;
        current.x = (rect.width - image.naturalWidth * scale) / 2;
        current.y = (rect.height - image.naturalHeight * scale) / 2;
        layout();
    }

    // Changes zoom while keeping the image point under (focusX, focusY) still.
    function setZoom(next, focusX, focusY) {
        if (!current) {
            return;
        }
        var rect = box.getBoundingClientRect();
        var previous = coverScale() * current.scale;
        next = Math.min(4, Math.max(1, next));
        var fx = focusX === undefined ? rect.width / 2 : focusX;
        var fy = focusY === undefined ? rect.height / 2 : focusY;
        var imageX = (fx - current.x) / previous;
        var imageY = (fy - current.y) / previous;
        current.scale = next;
        var scale = coverScale() * current.scale;
        current.x = fx - imageX * scale;
        current.y = fy - imageY * scale;
        zoom.value = String(next);
        layout();
    }

    // Loads a raster file into the crop box. The panel stays closed until
    // the image has decoded: before then naturalWidth/Height are 0 and
    // the crop math would be NaN. The box is only measurable once
    // visible, so open, then center.
    function show(file) {
        if (current) {
            URL.revokeObjectURL(current.url);
        }
        current = { file: file, url: URL.createObjectURL(file), scale: 1, x: 0, y: 0 };
        zoom.value = '1';
        var loadedUrl = current.url;
        image.onload = function () {
            // No-op when the load is stale: Cancel may have reset the
            // tool (or a newer pick replaced the image) before this
            // decode completed — center() must not run against null.
            if (!current || current.url !== loadedUrl) {
                return;
            }
            panel.classList.add('open');
            center();
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

    box.addEventListener('wheel', function (event) {
        if (!current) {
            return;
        }
        event.preventDefault();
        var rect = box.getBoundingClientRect();
        setZoom(current.scale * (event.deltaY < 0 ? 1.1 : 1 / 1.1),
            event.clientX - rect.left, event.clientY - rect.top);
    }, { passive: false, signal: abort.signal });

    var drag = null;
    box.addEventListener('pointerdown', function (event) {
        if (!current) {
            return;
        }
        event.preventDefault();
        drag = { x: event.clientX - current.x, y: event.clientY - current.y };
        box.setPointerCapture(event.pointerId);
    }, listen);
    box.addEventListener('pointermove', function (event) {
        if (!current || !drag) {
            return;
        }
        current.x = event.clientX - drag.x;
        current.y = event.clientY - drag.y;
        layout();
    }, listen);
    box.addEventListener('pointerup', function () {
        drag = null;
    }, listen);
    box.addEventListener('pointercancel', function () {
        drag = null;
    }, listen);

    applyButton.addEventListener('click', function () {
        if (!current) {
            return;
        }
        var rect = box.getBoundingClientRect();
        var scale = coverScale() * current.scale;
        var sourceX = -current.x / scale;
        var sourceY = -current.y / scale;
        var sourceWidth = rect.width / scale;
        var sourceHeight = rect.height / scale;
        // Never upscale: output is the framed region capped at 1920×1080
        // (floor so rounding can't nudge the output above the source).
        var outputWidth = Math.floor(Math.min(1920, sourceWidth));
        var outputHeight = Math.floor(outputWidth * 9 / 16);
        var canvas = document.createElement('canvas');
        canvas.width = outputWidth;
        canvas.height = outputHeight;
        var context = canvas.getContext('2d');
        if (!context) {
            fallbackToOriginal();
            return;
        }
        // The browser decodes EXIF orientation into the <img>, so drawing
        // from it keeps preview and output consistent.
        context.drawImage(
            image, sourceX, sourceY, sourceWidth, sourceHeight, 0, 0, outputWidth, outputHeight);
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
// crop box so its framing can be adjusted and re-saved. Returns the fetch
// promise so .NET interop surfaces failures as JSException.
export function open(prefix, url) {
    var tool = tools[prefix];
    if (!tool) {
        throw new Error('cropTool: not initialized for prefix "' + prefix + '"');
    }
    if (PASS_THROUGH.indexOf(extensionOf(url)) >= 0) {
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
            var name = url.split('/').pop().split('?')[0] || 'image';
            tool.show(new File([blob], name, { type: blob.type }));
        });
}
