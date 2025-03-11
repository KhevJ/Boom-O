const WebSocket = require("ws");
//socket.io and websocket 
const server = new WebSocket.Server({ port: 3000 });

server.on("connection", (socket) => {
    console.log("A user connected");

    // Send a welcome message
    socket.send(JSON.stringify({ action: "connected", message: "Welcome to UNO Server!" }));

    socket.on("message", (data) => {
        console.log("Received:", data.toString());

        // Example: Handle drawing a card
        const parsedData = JSON.parse(data);
        if (parsedData.action === "drawCard") {
            socket.send(JSON.stringify({ action: "cardDrawn", message: "You drew a card!" }));
        }
    });

    socket.on("close", () => {
        console.log("User disconnected");
    });
});

console.log("UNO Server running on port 3000");
