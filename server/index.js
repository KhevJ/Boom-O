// const WebSocket = require("ws");

// const server = new WebSocket.Server({ port: 3000 });
// const gameObjects = {}; // store game state
// const messageQueue = []; // queue to process messages one at a time
// let isProcessing = false; // flag to track if a message is being processed

// server.on("connection", (socket) => {
//     console.log("A user connected");

//     // send a welcome message
//     socket.send(JSON.stringify({ action: "connected", message: "Welcome to UNO Server!" }));

//     socket.on("message", (data) => {
//         const parsedData = JSON.parse(data);
//         console.log("Received:", parsedData);

//         // add message to the queue
//         messageQueue.push({ socket, parsedData });

//         // process queue if not already processing
//         processQueue();
//     });

//     socket.on("close", () => {
//         console.log("User disconnected");
//     });
// });

// async function processQueue() {
//     if (isProcessing || messageQueue.length === 0) {
//         return;
//     }

//     isProcessing = true; // lock queue processing

//     while (messageQueue.length > 0) {
//         const { socket, parsedData } = messageQueue.shift(); // get the next message in queue

//         console.log("Processing message:", parsedData.action);

//         if (parsedData.action === 'drawCard') {
//             await sendResponse(socket, { action: "cardDrawn", message: "You drew a card!" });
//         } else if (parsedData.action === 'sendDeck') {
//             console.log("Received deck:", parsedData.cards);
//             gameObjects["deck"] = parsedData.cards;
//             await sendResponse(socket, { action: "deckSaved", message: "Deck Saved!" });
//         } else if (parsedData.action === 'sendPlayerCards') {
//             console.log("Received player cards:", parsedData.cards);
//             gameObjects["playerCards"] = parsedData.cards;
//             await sendResponse(socket, { action: "playerCardsSaved", message: "Player Cards Saved!" });
//         }
//     }

//     isProcessing = false; // unlock queue processing
// }

// // helper function to send response with a delay to simulate processing time
// function sendResponse(socket, response) {
//     return new Promise((resolve) => {
//         setTimeout(() => {
//             socket.send(JSON.stringify(response));
//             resolve();
//         }, 500); // simulate processing delay (adjust as needed)
//     });
// }

// console.log("UNO Server running on port 3000");

'use strict';

const http = require('http');
const socket = require('socket.io');
const server = http.createServer();
const port = 3000;

var io = socket(server, {
    pingInterval: 15000,
    pingTimeout: 5000,
    transports: ["websocket"]
});

io.use((socket, next) => {
    if (socket.handshake.query.token === "UNITY") {
        next();
    } else {
        next(new Error("Authentication error"));
    }
});



// io.on('connection', socket => {
//   socket.emit('connection', {date: new Date().getTime(), data: "Hello Unity"})

//   socket.on('hello', (data) => {
//     socket.emit('hello', {date: new Date().getTime(), data: data});
//   });

//   socket.on('spin', (data) => {
//     socket.emit('spin', {date: new Date().getTime()});
//   });

//   socket.on('class', (data) => {
//     socket.emit('class', {date: new Date().getTime(), data: data});
//   });
// });



const gameObjects = {}; // store game state
const messageQueue = []; // queue to process messages in order
let isProcessing = false;

io.on("connection", (socket) => {
    console.log("A user connected");
    const playerName = socket.handshake.query.playerName || "Unknown Player";
    console.log(`Player Connected: ${playerName}`); 

    socket.emit("welcome", { message: `Welcome, ${playerName}!` });
    // console.log(socket);
    // console.log(messageQueue);
   

    // Add incoming messages to the queue
    socket.on("message", (data) => {
        console.log("yo");
        // messageQueue.push({ socket, data });
        processQueue();
    });

    socket.on("drawCard", (data) => {
        // console.log(data)
        messageQueue.push({ socket, action:"drawCard", data });
        processQueue();
    });

    socket.on("sendDeck", (data) => {
        // console.log(data)
        messageQueue.push({ socket, action:"sendDeck", data });
        processQueue();
    });

    socket.on("playerCardsSaved", (data) => {
        // console.log(data)
        messageQueue.push({ socket, action:"playerCardsSaved", data });
        processQueue();
    });



    socket.on("disconnect", () => {
        console.log("A user disconnected");
    });
});

async function processQueue() {
    console.log("Heoolo queue")
    if (isProcessing || messageQueue.length === 0) {
        return;
    }

    isProcessing = true; // lock queue processing

    while (messageQueue.length > 0) {
        const { socket, action,  data } = messageQueue.shift(); // get the next message

        console.log("Processing message:", action);

        if (action === 'drawCard') {
            handleDrawCard(socket);
        } else if (action === 'sendDeck') {
            await handleSendDeck(socket, data);
        } else if (action === 'sendPlayerCards') {
            await handleSendPlayerCards(socket, data);
        }
    }

    isProcessing = false; // unlock queue processing
}

//example handlers for events 
function handleDrawCard(socket) {
    // Simulate some processing delay
    socket.emit('drawnCard', "Server said Draw for turn Khevin");
}

function handleSendDeck(socket, data) {
    console.log("Received deck:", data);
    gameObjects["deck"] = data;
    socket.emit('deckSaved', "Server said Yugioh is better than UNO");
}

async function handleSendPlayerCards(socket, data) {
    console.log("Received deck:", data);
    gameObjects["playerHand"] = data;
    socket.emit('playerCardsSaved', "Server said why play UNO when Yugioh exists");
}

//helper function to send response with delay for testing
// function sendResponse(socket, response) {
//     return new Promise((resolve) => {
//         setTimeout(() => {
//             socket.emit("response", response);
//             resolve();
//         }, 500); // simulate delay
//     });
// }

io.listen(port);

console.log('UNO Server running on port ' + port);