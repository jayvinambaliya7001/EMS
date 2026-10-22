/**
 * Employee Management System (.NET Core MVC)
 * Face Scanner Attendance powered by face-api.js & webcam
 */

let modelsLoaded = false;
let webcamStream = null;
let detectionInterval = null;

// Initialize models on load
window.addEventListener('DOMContentLoaded', async () => {
    await loadFaceModels();
});

async function loadFaceModels() {
    updateScannerStatus('Loading face detection AI models from /models...', '#4f46e5');
    try {
        // Attempt local load from /models first
        await Promise.all([
            faceapi.nets.tinyFaceDetector.loadFromUri('/models'),
            faceapi.nets.faceLandmark68Net.loadFromUri('/models'),
            faceapi.nets.faceRecognitionNet.loadFromUri('/models')
        ]);
        modelsLoaded = true;
        updateScannerStatus('Face recognition AI models ready. Click "Start Camera" to begin.', '#059669');
    } catch (err) {
        console.warn('Local models failed to load, attempting online fallback...', err);
        try {
            const cdnPath = 'https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights';
            await Promise.all([
                faceapi.nets.tinyFaceDetector.loadFromUri(cdnPath),
                faceapi.nets.faceLandmark68Net.loadFromUri(cdnPath),
                faceapi.nets.faceRecognitionNet.loadFromUri(cdnPath)
            ]);
            modelsLoaded = true;
            updateScannerStatus('Face recognition models loaded via CDN. Click "Start Camera".', '#059669');
        } catch (fallbackErr) {
            console.error('All model loading attempts failed:', fallbackErr);
            updateScannerStatus('Could not load face detection models. Check your network or browser settings.', '#dc2626');
        }
    }
}

// Start Webcam
async function startWebcam() {
    if (!modelsLoaded) {
        showToast('Face recognition models are still loading. Please wait a moment.', 'warning');
        return;
    }

    const video = document.getElementById('webcamVideo');
    const placeholder = document.getElementById('cameraPlaceholder');

    updateScannerStatus('Requesting webcam access...', '#4f46e5');

    try {
        webcamStream = await navigator.mediaDevices.getUserMedia({
            video: { width: 640, height: 480, facingMode: 'user' }
        });

        video.srcObject = webcamStream;
        video.style.display = 'block';
        if (placeholder) placeholder.style.display = 'none';

        video.onloadedmetadata = () => {
            video.play();
            const startBtn = document.getElementById('startCameraBtn');
            const stopBtn = document.getElementById('stopCameraBtn');
            const enrollBtn = document.getElementById('enrollFaceBtn');
            const punchBtn = document.getElementById('facePunchBtn');

            if (startBtn) startBtn.style.display = 'none';
            if (stopBtn) stopBtn.style.display = 'inline-flex';
            if (enrollBtn) enrollBtn.disabled = false;
            if (punchBtn) punchBtn.disabled = false;

            updateScannerStatus('Camera active. Center your face inside the box.', '#059669');
            startLiveDetectionLoop();
        };
    } catch (err) {
        console.error('Webcam error:', err);
        updateScannerStatus('Webcam access denied: ' + err.message, '#dc2626');
        showToast('Unable to access webcam. Please allow camera permissions in browser.', 'error');
    }
}

// Stop Webcam
function stopWebcam() {
    if (detectionInterval) {
        clearInterval(detectionInterval);
        detectionInterval = null;
    }

    if (webcamStream) {
        webcamStream.getTracks().forEach(track => track.stop());
        webcamStream = null;
    }

    const video = document.getElementById('webcamVideo');
    const placeholder = document.getElementById('cameraPlaceholder');
    const canvas = document.getElementById('overlayCanvas');

    if (video) video.style.display = 'none';
    if (placeholder) placeholder.style.display = 'block';
    if (canvas) {
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);
    }

    const startBtn = document.getElementById('startCameraBtn');
    const stopBtn = document.getElementById('stopCameraBtn');
    const enrollBtn = document.getElementById('enrollFaceBtn');
    const punchBtn = document.getElementById('facePunchBtn');

    if (startBtn) startBtn.style.display = 'inline-flex';
    if (stopBtn) stopBtn.style.display = 'none';
    if (enrollBtn) enrollBtn.disabled = true;
    if (punchBtn) punchBtn.disabled = true;

    updateScannerStatus('Camera stopped.', '#64748b');
}

// Face Detection Loop
function startLiveDetectionLoop() {
    const video = document.getElementById('webcamVideo');
    const canvas = document.getElementById('overlayCanvas');

    detectionInterval = setInterval(async () => {
        if (!video || video.paused || video.ended || !webcamStream) return;

        const displaySize = { width: video.videoWidth || 640, height: video.videoHeight || 480 };
        faceapi.matchDimensions(canvas, displaySize);

        try {
            const detection = await faceapi
                .detectSingleFace(video, new faceapi.TinyFaceDetectorOptions({ inputSize: 320, scoreThreshold: 0.5 }))
                .withFaceLandmarks();

            const ctx = canvas.getContext('2d');
            ctx.clearRect(0, 0, canvas.width, canvas.height);

            if (detection) {
                const resized = faceapi.resizeResults(detection, displaySize);
                faceapi.draw.drawDetections(canvas, resized);
            }
        } catch (e) {
            // Ignore background frame exceptions
        }
    }, 300);
}

// Capture Face Descriptor
async function captureFaceDescriptor() {
    const video = document.getElementById('webcamVideo');
    if (!webcamStream || !video || video.paused) {
        updateScannerStatus('Please start the camera first.', '#dc2626');
        return null;
    }

    updateScannerStatus('Detecting face & analyzing landmarks...', '#4f46e5');

    try {
        const detection = await faceapi
            .detectSingleFace(video, new faceapi.TinyFaceDetectorOptions({ inputSize: 416, scoreThreshold: 0.5 }))
            .withFaceLandmarks()
            .withFaceDescriptor();

        if (!detection) {
            updateScannerStatus('No face detected. Please face the camera directly.', '#dc2626');
            showToast('No face detected. Please face the camera directly.', 'warning');
            return null;
        }

        return Array.from(detection.descriptor);
    } catch (err) {
        console.error('Face capture error:', err);
        updateScannerStatus('Detection failed: ' + err.message, '#dc2626');
        return null;
    }
}

// Enroll Face Action
async function enrollCurrentFace() {
    const enrollBtn = document.getElementById('enrollFaceBtn');
    if (enrollBtn) {
        enrollBtn.disabled = true;
        enrollBtn.textContent = 'Enrolling...';
    }

    try {
        const descriptor = await captureFaceDescriptor();
        if (!descriptor) return;

        updateScannerStatus('Saving face descriptor in XAMPP MySQL...', '#4f46e5');

        const res = await fetch('/api/employee/face/enroll', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ descriptor })
        });

        const data = await res.json();
        if (res.ok && data.success) {
            updateScannerStatus('Face enrolled successfully in database!', '#059669');
            showToast('Face biometric enrolled successfully!', 'success');
            const badge = document.getElementById('enrollmentStatusBadge');
            if (badge) {
                badge.innerHTML = '<span class="status-pill pill-emerald"><i class="fa-solid fa-shield-check"></i> Face Biometric Enrolled</span>';
            }
        } else {
            updateScannerStatus(data.message || 'Face enrollment failed.', '#dc2626');
            showToast(data.message || 'Face enrollment failed.', 'error');
        }
    } catch (err) {
        updateScannerStatus('Network error during face enrollment.', '#dc2626');
        showToast('Network error during face enrollment.', 'error');
    } finally {
        if (enrollBtn) {
            enrollBtn.disabled = false;
            enrollBtn.textContent = 'Enroll Face';
        }
    }
}

// Face Punch Action
async function punchWithCurrentFace() {
    const punchBtn = document.getElementById('facePunchBtn');
    if (punchBtn) {
        punchBtn.disabled = true;
        punchBtn.textContent = 'Verifying Face...';
    }

    try {
        const descriptor = await captureFaceDescriptor();
        if (!descriptor) return;

        updateScannerStatus('Matching face against enrolled biometric template in MySQL...', '#4f46e5');

        const res = await fetch('/api/employee/face/punch', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ descriptor })
        });

        const data = await res.json();
        if (res.ok && data.success) {
            let msg = data.message || 'Attendance logged successfully.';
            if (data.action === 'PUNCH_OUT' && data.workHours) {
                msg += ` Total work duration: ${data.workHours}`;
            }
            updateScannerStatus(msg, '#059669');
            showToast(msg, 'success');
            setTimeout(() => window.location.reload(), 1200);
        } else {
            updateScannerStatus(data.message || 'Face attendance verification failed.', '#dc2626');
            showToast(data.message || 'Face attendance verification failed.', 'error');
        }
    } catch (err) {
        updateScannerStatus('Server error during face punch.', '#dc2626');
        showToast('Server error during face punch.', 'error');
    } finally {
        if (punchBtn) {
            punchBtn.disabled = false;
            punchBtn.textContent = 'Face Punch (In / Out)';
        }
    }
}

function updateScannerStatus(message, color = '#334155') {
    const msgEl = document.getElementById('scannerMessage');
    if (msgEl) {
        msgEl.textContent = message;
        msgEl.style.color = color;
    }
}
