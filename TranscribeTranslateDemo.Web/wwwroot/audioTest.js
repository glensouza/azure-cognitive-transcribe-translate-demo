var AudioTest = {};

(function () {
    // Basic variables for app
    var soundClips;
    var canvas;
    // visualiser variables
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

        //const clipName = prompt('Enter a name for your sound clip?', 'My unnamed clip');
        const clipName = null;

        const clipContainer = document.createElement('article');
        const clipLabel = document.createElement('p');
        const audio = document.createElement('audio');
        const deleteButton = document.createElement('button');

        clipContainer.classList.add('clip');
        audio.setAttribute('controls', '');
        deleteButton.textContent = 'Delete';
        deleteButton.className = 'delete';

        if (clipName === null) {
            clipLabel.textContent = 'My unnamed clip';
        } else {
            clipLabel.textContent = clipName;
        }

        clipContainer.appendChild(audio);
        clipContainer.appendChild(clipLabel);
        clipContainer.appendChild(deleteButton);
        soundClips.appendChild(clipContainer);

        audio.controls = true;
        blob = new Blob(chunks, { type: 'audio/webm' });

        chunks = [];
        const audioURL = window.URL.createObjectURL(blob);
        audio.src = audioURL;
        console.log("recorder stopped");
        dotnetCaller.invokeMethodAsync('OnAudioUrl', audioURL);

        deleteButton.onclick = function (e) {
            e.target.closest(".clip").remove();
        }

        clipLabel.onclick = function () {
            const existingName = clipLabel.textContent;
            const newClipName = prompt('Enter a new name for your sound clip?');
            if (newClipName === null) {
                clipLabel.textContent = existingName;
            } else {
                clipLabel.textContent = newClipName;
            }
        }
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

        // set up basic variables for app
        soundClips = document.querySelector('.sound-clips');
        canvas = document.querySelector('.visualizer');

        // visualiser setup - create web audio api context and canvas
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
    //    xhr.addEventListener("load", transferComplete);
    //    xhr.addEventListener("error", transferFailed)
    //    xhr.addEventListener("abort", transferFailed)
        xhr.open("POST", apiUrl + "api/Transcribe", true);
        xhr.send(fd);

        //const formData = new FormData();
        //formData.append('file', blob);
        //formData.append('userId', userId);
        //formData.append("languageFrom", this.languageSelected.languageCode);
        //formData.append("languageTo", this.languageSelected.languageCode === "en-US" ? "es-MX" : "en-US");

        //this.http.post("/api/Transcribe", formData).subscribe(
        //    (res) => {
        //        console.log(res);
        //    },
        //    (err) => console.log(err)
        //);
    }
})();
