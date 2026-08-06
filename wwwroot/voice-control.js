window.aeroVoice = (() => {
    let recognition = null;
    let dotNetReference = null;
    let listening = false;
    let speaking = false;

    function initialize(dotNetRef) {
        dotNetReference = dotNetRef;

        const Recognition =
            window.SpeechRecognition ||
            window.webkitSpeechRecognition;

        if (!Recognition) {
            return {
                supported: false,
                message:
                    "Speech recognition is unsupported."
            };
        }

        recognition = new Recognition();

        recognition.continuous = true;
        recognition.interimResults = false;
        recognition.lang = "en-GB";

        recognition.onresult = async event => {
            if (speaking || !dotNetReference) {
                return;
            }

            const result =
                event.results[
                    event.results.length - 1
                ][0];

            const transcript =
                result.transcript?.trim() ?? "";

            if (!transcript) {
                return;
            }

            try {
                await dotNetReference.invokeMethodAsync(
                    "ReceiveVoiceTranscript",
                    transcript,
                    result.confidence ?? 0);
            } catch (error) {
                console.error(
                    "Could not send voice transcript to Blazor:",
                    error);
            }
        };

        recognition.onerror = async event => {
            const error =
                event.error ?? "unknown";

            if (speaking &&
                error === "aborted") {
                return;
            }

            if (error !== "no-speech" &&
                error !== "aborted") {
                listening = false;
            }

            if (!dotNetReference) {
                return;
            }

            try {
                await dotNetReference.invokeMethodAsync(
                    "VoiceRecognitionError",
                    error);
            } catch (invokeError) {
                console.error(
                    "Could not report voice recognition error:",
                    invokeError);
            }
        };

        recognition.onend = () => {
            if (!listening ||
                !recognition ||
                speaking) {
                return;
            }

            window.setTimeout(() => {
                if (!listening ||
                    !recognition ||
                    speaking) {
                    return;
                }

                try {
                    recognition.start();
                } catch {
                    // Recognition may already be starting.
                }
            }, 300);
        };

        return {
            supported: true,
            message:
                "Voice control is available."
        };
    }

    function start() {
        if (!recognition) {
            return false;
        }

        listening = true;

        try {
            recognition.start();
            return true;
        } catch {
            // Recognition may already be running.
            return true;
        }
    }

    function stop() {
        listening = false;

        if (!recognition) {
            return;
        }

        try {
            recognition.stop();
        } catch {
            // Recognition may already be stopped.
        }
    }

    function restartRecognitionAfterSpeech() {
        speaking = false;

        if (!recognition || !listening) {
            return;
        }

        window.setTimeout(() => {
            if (!recognition ||
                !listening ||
                speaking) {
                return;
            }

            try {
                recognition.start();
            } catch {
                // Recognition may already be restarting.
            }
        }, 300);
    }

    function speak(text) {
        if (!text ||
            !window.speechSynthesis) {
            return;
        }

        speaking = true;

        if (recognition && listening) {
            try {
                recognition.stop();
            } catch {
                // Recognition may already be stopped.
            }
        }

        window.speechSynthesis.cancel();

        const utterance =
            new SpeechSynthesisUtterance(text);

        utterance.lang = "en-GB";

        utterance.onend =
            restartRecognitionAfterSpeech;

        utterance.onerror =
            restartRecognitionAfterSpeech;

        window.speechSynthesis.speak(
            utterance);
    }

    function dispose() {
        listening = false;
        speaking = false;

        if (recognition) {
            try {
                recognition.abort();
            } catch {
                // Recognition may already be stopped.
            }
        }

        if (window.speechSynthesis) {
            window.speechSynthesis.cancel();
        }

        recognition = null;
        dotNetReference = null;
    }

    return {
        initialize,
        start,
        stop,
        speak,
        dispose
    };
})();