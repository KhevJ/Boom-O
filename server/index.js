
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
    if (isProcessing || messageQueue.length === 0) {
        return;
    }

    isProcessing = true; // lock queue processing

    while (messageQueue.length > 0) {
        const { socket, action,  data } = messageQueue.shift(); // get the next message

        console.log("Processing message:", action);

        if (action === 'drawCard') {
            handleDrawCard(socket, data);
        } else if (action === 'sendDeck') {
            handleSendDeck(socket, data);
        } else if (action === 'sendPlayerCards') {
            handleSendPlayerCards(socket, data);
        }
    }

    isProcessing = false; // unlock queue processing
}

//example handlers for events 
function handleDrawCard(socket, data) {
    console.log("Received drawn card:",data);
    
    if (gameObjects.deck) {
        if(gameObjects.deck.length > 0 && gameObjects.deck[0] == data){
            const topCard = gameObjects.deck.shift();
            //console.log("here")
            socket.emit('drawnCard', "Server said You drew " + topCard);
            //here add something to broadcast to all other players in the room/game
        }
    }
}

function handleSendDeck(socket, data) {
    console.log("Received deck:", data);
    gameObjects["deck"] = data;
    socket.emit('deckSaved', "Server said Yugioh is better than UNO");
}

function handleSendPlayerCards(socket, data) {
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


