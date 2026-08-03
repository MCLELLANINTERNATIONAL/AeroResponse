window.aeroVoice = (() => {
    let recognition = null;
    let dotNetReference = null;
    let listening = false;

    function initialize(dotNetRef) {
        dotNetReference = dotNetRef;
        const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!Recognition) return { supported: false, message: "Speech recognition is unsupported." };

        recognition = new Recognition();
        recognition.continuous = true;
        recognition.interimResults = false;
        recognition.lang = "en-GB";

        recognition.onresult = async event => {
            const result = event.results[event.results.length - 1][0];
            await dotNetReference.invokeMethodAsync(
                "ReceiveVoiceTranscript", result.transcript, result.confidence ?? 0);
        };

        recognition.onerror = async event => {
            listening = false;
            await dotNetReference.invokeMethodAsync("VoiceRecognitionError", event.error ?? "unknown");
        };

        recognition.onend = () => {
            if (listening && recognition) recognition.start();
        };

        return { supported: true, message: "Voice control is available." };
    }

    function start() {
        if (!recognition) return false;
        listening = true;
        recognition.start();
        return true;
    }

    function stop() {
        listening = false;
        if (recognition) recognition.stop();
    }

    function speak(text) {
        if (!text || !window.speechSynthesis) return;
        window.speechSynthesis.cancel();
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = "en-GB";
        window.speechSynthesis.speak(utterance);
    }

    function dispose() { stop(); recognition = null; dotNetReference = null; }
    return { initialize, start, stop, speak, dispose };
})();