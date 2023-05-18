var AudioTest = {};

(function () {
    // visualiser variables
    var canvas;
    var audioCtx;
    var canvasCtx;
    // Main variables
    var getUserMediaSupported = false;
    var mediaRecorder;
    var chunks = [];
    const constraints = { audio: true };
    var dotnetCaller;
    var blob;

    var mediaRecorderOnStop = function (e) {
        console.log("data available after MediaRecorder.stop() called.");

        blob = new Blob(chunks, { type: 'audio/webm' });

        chunks = [];
        const audioURL = window.URL.createObjectURL(blob);
        console.log("recorder stopped");
        dotnetCaller.invokeMethodAsync('OnAudioUrl', audioURL);
    }

    function visualize(stream) {
        if (!audioCtx) {
            audioCtx = new AudioContext();
        }

        const source = audioCtx.createMediaStreamSource(stream);

        const analyser = audioCtx.createAnalyser();
        analyser.fftSize = 2048;
        const bufferLength = analyser.frequencyBinCount;
        const dataArray = new Uint8Array(bufferLength);

        source.connect(analyser);
        //analyser.connect(audioCtx.destination);

        draw()

        function draw() {
            const WIDTH = canvas.width
            const HEIGHT = canvas.height;

            requestAnimationFrame(draw);

            analyser.getByteTimeDomainData(dataArray);

            canvasCtx.fillRect(0, 0, WIDTH, HEIGHT);
            canvasCtx.beginPath();

            let sliceWidth = WIDTH * 1.0 / bufferLength;
            let x = 0;

            for (let i = 0; i < bufferLength; i++) {

                let v = dataArray[i] / 128.0;
                let y = v * HEIGHT / 2;

                if (i === 0) {
                    canvasCtx.moveTo(x, y);
                } else {
                    canvasCtx.lineTo(x, y);
                }

                x += sliceWidth;
            }

            canvasCtx.lineTo(canvas.width, canvas.height / 2);
            canvasCtx.stroke();
        }
    }

    AudioTest.Init = function (caller) {
        dotnetCaller = caller;

        // visualiser setup - create web audio api context and canvas
        canvas = document.querySelector('.visualizer');
        canvasCtx = canvas.getContext("2d");
        canvasCtx.fillStyle = 'rgb(200, 200, 200)';
        canvasCtx.lineWidth = 2;
        canvasCtx.strokeStyle = 'rgb(0, 0, 0)';

        //main block for doing the audio recording
        if (navigator.mediaDevices.getUserMedia) {
            console.log('getUserMedia supported.');
            getUserMediaSupported = true;
            chunks = [];

            let onSuccess = function (stream) {
                if (!mediaRecorder) {
                    visualize(stream);
                    mediaRecorder = new MediaRecorder(stream, { mimeType: 'audio/webm' });
                    mediaRecorder.onstop = mediaRecorderOnStop;
                    mediaRecorder.ondataavailable = function (e) {
                        chunks.push(e.data);
                    };
                }
            }

            let onError = function (err) {
                console.log('The following error occured: ' + err);
            }

            navigator.mediaDevices.getUserMedia(constraints).then(onSuccess, onError);

        } else {
            console.log('getUserMedia not supported on your browser!');
        }
    }

    AudioTest.Record = function () {
        if (!getUserMediaSupported) {
            return;
        }

        mediaRecorder.start();
        console.log(mediaRecorder.state);
        console.log("recorder started");
    }

    AudioTest.Stop = function () {
        if (!getUserMediaSupported) {
            return;
        }

        mediaRecorder.stop();
        console.log(mediaRecorder.state);
        console.log("recorder stopped");
    };

    AudioTest.Upload = function(apiUrl, userId) {
        let filename = new Date().toISOString().replaceAll(':', "");
        let fd = new FormData();
        fd.append("userId", userId);
        fd.append("file", blob, filename);
        let xhr = new XMLHttpRequest();
        xhr.open("POST", apiUrl + "api/Transcribe", true);
        xhr.send(fd);
    }
})();
