window.webSocket = {
    connect: function (url, dotNetRef) {
        window.socket = new WebSocket(url);
        window.socket.onopen = function () {
            dotNetRef.invokeMethodAsync('Open');
            console.log("WebSocket onopen");
        };
        window.socket.onmessage = function (event) {
            dotNetRef.invokeMethodAsync('ReceiveMessage', event.data);
            console.log("WebSocket onmessage");
        };
        window.socket.onerror = function (event) {
            console.error("WebSocket error:", event);
        };
        window.socket.onclose = function (event) {
            console.warn("WebSocket closed:", event);
        };
    },
    send: function (message) {
        window.socket.send(message);
    },
    close: function () {
        window.socket.close();
    }
};