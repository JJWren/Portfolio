// Admin header-image crop tool: a fixed 16:9 window the admin zooms and pans
// a chosen image within. Apply bakes the framed region onto a canvas and
// hands the result to the hidden Blazor InputFile, so the normal upload path
// (ImageUploadService) still validates and stores the file. Initialized per
// editor from OnAfterRenderAsync via window.__cropTool.init(prefix, maxBytes).
(function () {
    'use strict';

    // Post heroes render width-capped at 420px tall (app.css .post-hero-image)
    // inside the 1080px container: at desktop width that shows the middle
    // 420 / (1080 * 9 / 16) of a 16:9 crop. Narrower viewports show more.
    var HERO_BAND_FRACTION = 420 / (1080 * 9 / 16);

    // Vector / animated sources skip the crop: canvas would rasterize an SVG
    // and freeze a GIF to its first frame.
    var PASS_THROUGH = ['.svg', '.gif'];

    function extensionOf(name) {
        var i = name.lastIndexOf('.');
        return i < 0 ? '' : name.slice(i).toLowerCase();
    }

    function init(prefix, maxBytes) {
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
            return;
        }
        // Re-running init (e.g. after a retried interop call) must not
        // stack duplicate listeners.
        if (source.dataset.cropInitialized) {
            return;
        }
        source.dataset.cropInitialized = 'true';

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
            panel.hidden = true;
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

        source.addEventListener('change', function () {
            var file = source.files && source.files[0];
            if (!file) {
                return;
            }
            if (PASS_THROUGH.indexOf(extensionOf(file.name)) >= 0) {
                handOff(file);
                return;
            }
            if (current) {
                URL.revokeObjectURL(current.url);
            }
            current = { file: file, url: URL.createObjectURL(file), scale: 1, x: 0, y: 0 };
            zoom.value = '1';
            panel.hidden = false;
            image.onload = center;
            image.src = current.url;
        });

        zoom.addEventListener('input', function () {
            setZoom(parseFloat(zoom.value));
        });

        box.addEventListener('wheel', function (event) {
            if (!current) {
                return;
            }
            event.preventDefault();
            var rect = box.getBoundingClientRect();
            setZoom(current.scale * (event.deltaY < 0 ? 1.1 : 1 / 1.1),
                event.clientX - rect.left, event.clientY - rect.top);
        }, { passive: false });

        var drag = null;
        box.addEventListener('pointerdown', function (event) {
            if (!current) {
                return;
            }
            event.preventDefault();
            drag = { x: event.clientX - current.x, y: event.clientY - current.y };
            box.setPointerCapture(event.pointerId);
        });
        box.addEventListener('pointermove', function (event) {
            if (!current || !drag) {
                return;
            }
            current.x = event.clientX - drag.x;
            current.y = event.clientY - drag.y;
            layout();
        });
        box.addEventListener('pointerup', function () {
            drag = null;
        });
        box.addEventListener('pointercancel', function () {
            drag = null;
        });

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
            var outputHeight = Math.round(outputWidth * 9 / 16);
            var canvas = document.createElement('canvas');
            canvas.width = outputWidth;
            canvas.height = outputHeight;
            var context = canvas.getContext('2d');
            if (!context) {
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
        });

        function encode(canvas, type, done) {
            canvas.toBlob(function (blob) {
                if (!blob) {
                    return;
                }
                // Canvas PNGs of photographic crops can outgrow the upload
                // cap; fall back to JPEG rather than let the server reject it.
                if (type === 'image/png' && maxBytes && blob.size > maxBytes) {
                    canvas.toBlob(function (jpeg) {
                        if (jpeg) {
                            done(jpeg, 'image/jpeg');
                        }
                    }, 'image/jpeg', 0.9);
                    return;
                }
                done(blob, type);
            }, type, 0.9);
        }

        fullButton.addEventListener('click', function () {
            if (!current) {
                return;
            }
            handOff(current.file);
        });

        cancelButton.addEventListener('click', reset);
    }

    window.__cropTool = { init: init };
})();
