
'use strict';

const http = require('http');
const socket = require('socket.io');
const server = http.createServer();
const { v4: uuidV4 } = require('uuid');
const port = 3000;


var io = socket(server, {
    pingInterval: 15000,
    pingTimeout: 5000,
    transports: ["websocket"]
});

const clientNamespace = io.of("/client");

clientNamespace.use((socket, next) => {
    if (socket.handshake.query.token === "UNITY") {
        next();
    } else {
        next(new Error("Authentication error"));
    }
});


// const gameObjects = {}; // store game state
const messageQueue = []; // queue to process messages in order
let isProcessing = false;
const rooms = new Map();// will store all rooms

clientNamespace.on("connection", (socket) => {
    console.log("A user connected");
    // const playerName = socket.handshake.query.playerName || "Unknown Player";
    // console.log(`Player Connected: ${playerName}`);

    socket.emit("welcome", { message: `Welcome!` }); //add running
    // console.log(socket);
    // console.log(messageQueue);

    socket.on('createRoom', async (callback) => {
        messageQueue.push({ socket, action: "createRoom", callback });
        processQueue();

    });

    socket.on('joinRoom', async (data, callback) => {
        messageQueue.push({ socket, action: "joinRoom", data, callback });
        processQueue();
    });


    //? useless function to test
    // Add incoming messages to the queue 
    socket.on("message", (data) => {
        console.log("yo");
        // messageQueue.push({ socket, data });
        processQueue();
    });

    socket.on("drawCard", (data) => {
        // console.log(data)
        messageQueue.push({ socket, action: "drawCard", data });
        processQueue();
    });

    socket.on("sendDeck", (data) => {
        // console.log(data)
        messageQueue.push({ socket, action: "sendDeck", data });
        processQueue();
    });

    socket.on("sendPlayerCards", (data) => {
        // console.log(data)
        messageQueue.push({ socket, action: "sendPlayerCards", data });
        processQueue();
    });

    socket.on("sendTopCard", (data) => { //the one card that is on top of the pile
        // console.log(data)
        messageQueue.push({ socket, action: "sendTopCard", data });
        processQueue();
    });

    socket.on("wildcard", (data) => {
        messageQueue.push({ socket, action: "wildcard", data });
        processQueue();
    })

    socket.on("updateTurnAccess", (data) => {
        messageQueue.push({ socket, action: "updateTurnAccess", data });
        processQueue();
    })




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
        const { socket, action, data, callback } = messageQueue.shift(); // get the next message

        console.log("Processing message:", action);
        if (action === 'createRoom') {
            //const roomId = uuidV4();
            const roomId = "Khevin's Room"
            const playerName = uuidV4();
            await socket.join(roomId);
            let reverse = false;
            rooms.set(roomId, {
                roomId,
                players: [{ playerName, socketId: socket.id }], //[player1, player2,  player3]
                reverse,// for when we need to reverse the order of player \
                chosenColor
                // did you know yugioh is the best turn based game
            });

            console.log(`Room created: ${roomId}`);
            if (callback) {
                callback(roomId, playerName); // that name of the player and the roomID is gonna be stored on Unity
            }
        }

        if (action === 'joinRoom') {
            // check if room exists and has a player waiting
            console.log(data); //data has the roomId
            const room = rooms.get(data); 
            console.log(room);
            const playerName = uuidV4();
            // add the joining user's data to the list of players in the room
            if (room && room.players.length < 2) {
                await socket.join(data); // make the joining client join the room
                const roomUpdate = {
                    ...room,
                    players: [
                        ...room.players,
                        { playerName, socketId: socket.id },
                    ],
                };
                rooms.set(data, roomUpdate);
                if (callback) {
                    callback(data, playerName);
                }
                // console.log(socket.id)
                // console.log(room.players)
                // console.log("Room length " + room.players.length)
                clientNamespace.to(data).emit("roomLength", rooms.get(data).players.length)
            }
            else {
                if (callback) {
                    callback("Error");
                }
            }


        }
        if (action === 'drawCard') {
            handleDrawCard(socket, data);
        } else if (action === 'sendDeck') {
            handleSendDeck(socket, data);
        } else if (action === 'sendPlayerCards') {
            handleSendPlayerCards(socket, data);
        } else if (action === 'sendTopCard') {
            handleTopCard(socket, data);
        } else if (action === "wildcard"){
            handleWildCard(socket, data);
        } else if (action === "updateTurnAccess"){
            handleTurnAccess(socket, data);
        }


    }

    isProcessing = false; // unlock queue processing
}
let snapshotActive = false;
let snapshotState = null;

function captureSnapshotState() {
    const allRooms = {};
    rooms.forEach((room, roomId) => {
        allRooms[roomId] = {
            topCard: room.topCard,
            deck: room.deck,
            playerHands: room.playerHands,
        };
    });
    return {id: myId,leader: currentLeader,rooms: allRooms};
}

function broadcastSnapshotToReplicas(){
    if(myId!==currentLeader) return;
    snapshotState = captureSnapshotState();
    console.log(`Broadcasting snapshot from Leader server ${myId}`)
    console.log(JSON.stringify(snapshotState,null,2))
    for(let[port,link] of links){
        if(port!=3000){
            const ioClient = require("socket.io-client")
            const peerSocket = ioClient(link, { transports: ["websocket"] });
            peerSocket.on("connect", () => {
                peerSocket.emit("REPLICA_SNAPSHOT", snapshotState);
                peerSocket.disconnect();
            });
        }
    }
    
}

//example handlers for events 
function handleDrawCard(socket, data) {
    console.log("Received drawn card:", data);
    const room = rooms.get(data.roomId);
    console.log(room);
    const drawnCard = room.deck.shift();
    console.log(drawnCard);
    room.playerHands[data.playerName].push(drawnCard);
    const roomUpdate = {
        ...room,
        playerHands: room.playerHands,
        deck: room.deck
    };
    rooms.set(data.roomId, roomUpdate);
    socket.broadcast.to(data.roomId).emit('drawnCard', drawnCard);
    broadcastSnapshotToReplicas();


    // if (gameObjects.deck) {
    //     if (gameObjects.deck.length > 0 && gameObjects.deck[0] == data) {
    //         const topCard = gameObjects.deck.shift();
    //         //console.log("here")
    //         socket.emit('drawnCard', "Server said You drew " + topCard);
    //         //here add something to broadcast to all other players in the room/game
    //     }
    // }
}

function handleTopCard(socket, data) {
    console.log(" top card:", data);
    const room = rooms.get(data.roomId);
    //!need to update players hand except when top card is placed for the first time
    let roomUpdate;
    if (!data.firstTime) {
        const hand = room.playerHands[data.playerName];
        const index = hand.indexOf(data.topCard);

        if (index !== -1) {
            hand.splice(index, 1);
        }
        roomUpdate = {
            ...room,
            topCard: data.topCard,
            playerHands: room.playerHands
        };
    }
    else {
        roomUpdate = {
            ...room,
            topCard: data.topCard
        };
    }


    rooms.set(data.roomId, roomUpdate);

    // console.log(rooms.get(data.roomId));
    socket.broadcast.to(data.roomId).emit('topCardUpdate', data.topCard);
    broadcastSnapshotToReplicas();

}

function handleSendDeck(socket, data) {
    console.log("Received deck:", data);
    const room = rooms.get(data.roomId);
    let deck = [...data.deck];
    // gameObjects["deck"] = deck; //! delete this later this is just for testing drawing when drawing is not fully implemented
    const playerHands = {};

    for (const player of room.players) { //player here is the socket
        playerHands[player.playerName] = deck.splice(0, 7); //each player gets 7 cards //? change player.id to player actual name
    }

    const roomUpdate = {
        ...room,
        deck: deck,
        playerHands: playerHands
    };
    rooms.set(data.roomId, roomUpdate);

    console.log(rooms.get(data.roomId));

    for (const player of room.players) {
        // here has to be sockets id
        clientNamespace.to(player.socketId).emit("playerCardsSaved", playerHands[player.playerName]); //find player which has playerId =="Name"  player.socketID
        
    }
    // ! should be broacast to everyone except the host Done Brother
    clientNamespace.to(data.roomId).emit("deckSaved", deck) //send deck to everyone
    clientNamespace.to(room.players[0].socketId).emit("allowedTurn", "yourturn"); 
    broadcastSnapshotToReplicas();
}

function handleSendPlayerCards(socket, data) {
    console.log("Received deck:", data);
    // gameObjects["playerHand"] = data;
    socket.emit('playerCardsSaved', "Server said why play UNO when Yugioh exists");
    broadcastSnapshotToReplicas();
}

function handleTurnAccess(socket, data) {
    console.log(data);
    const room = rooms.get(data.roomId);
    const curr_player=room.players.findIndex((player)=>player.playerName==data.playerName);
    console.log(curr_player);
    const len = room.players.length;
    let nextIndex;
    if(curr_player === len -1) nextIndex = 0;
    else nextIndex = curr_player + 1;
    
    // console.log("index: ", nextIndex)
    
    clientNamespace.to(room.players[nextIndex].socketId).emit("allowedTurn", "yourturn"); 
}



const ringNamespace = io.of("/ring");


const servers = [
    { id: 4, port: 3000, next: 3001, nextId: 3 },
    { id: 3, port: 3001, next: 3002, nextId: 2 },
    { id: 2, port: 3002, next: 3003, nextId: 1 },
    { id: 1, port: 3003, next: 3004, nextId: 0 },
    { id: 0, port: 3004, next: 3000, nextId: 4 },
];

const links = new Map();

links.set(3000, `http://localhost:3000/ring`); //change that here
links.set(3001, `http://localhost:3001/ring`);
links.set(3002, `http://localhost:3002/ring`);
links.set(3003, `http://localhost:3003/ring`);
links.set(3004, `http://localhost:3004/ring`);

//let's do some math here
// 4-> 3 -> 2 -> 1 -> 4
// for next id  -> (id - 1 + 4) % 4 || 4
// for next port -> ((3001-3003) + 1)%3 + 3000 so next port = ((port - 3003) + 1)%3 +3000

const myId = 4;
const myNext = servers.find(s => s.id === myId).next; //next port
//const myAddress = servers.find(s => s.id === myId).address ;

let currentLeader = 4;
let running = false;
const ioClient = require("socket.io-client");
let ringSocket = ioClient(links.get(myNext), {
    transports: ["websocket"]
});






ringNamespace.on("connection", (socket) => {
    console.log(`Server ${myId} received a connection from ${socket.handshake.address}`);

    //election message reception
    //if k > i then send election(k) to Successor(i)
    // if k < i & not runningi then
    //      send election(i) to Successor(i)
    //      runningi = true
    // if k = i then
    //      leaderi = i
    //      send leader(i) to Successor(i)
    socket.on("ELECTION", (data) => {
        console.log(`Server ${myId} received ELECTION message with biggest id: ${data.id}`);
        if (data.id > myId) {
            ringSocket.emit("ELECTION", data);
        } else if (data.id < myId && !running) {
            ringSocket.emit("ELECTION", { id: myId });
            running = true;
        } else if (data.id === myId) {
            console.log(`Server ${myId} is the new leader!`);
            currentLeader = myId;
            //running = false;
            announceLeader();
        }
    });

    socket.on("REPLICA_SNAPSHOT", (state) => {
        console.log(`Server ${myId} received replicated snapshot from Leader`);
        console.log(JSON.stringify(state, null, 2));
        snapshotState = state; // update local state from leader
    });
    // Case message is leader(k):
    // leader message reception
    // leaderi = k
    // running = false
    // if k ≠ i then
    //      send leader(k) to Successor(i)
    // quit election
    socket.on("LEADER", (data) => {
        console.log(`Server ${myId} acknowledges Leader ${data.leader}`);
        currentLeader = data.leader;
        running = false;
        if (data.leader === myId) return;
        ringSocket.emit("LEADER", { leader: data.leader });
    });


    // verifying if all servers are alive
    socket.on("HEARTBEAT", (data) => {
        if (data.id === myId) {
            ringSocket.emit("ALIVE");
        }
    });

    // checks if socket connection is still there
    socket.on("ALIVE", () => {
        console.log("Leader is alive.");
    });

});

//ring has 2 messages
// election
// leader but you just  update the currentleader that is what you send on welcome

//initiate election
// set running to true
// send election(myId) to successor
function startElection() {
    if (running) return; // cannot let same server that started an election start another one again
    running = true;
    console.log(`Server ${myId} starting election.`);
    ringSocket.emit("ELECTION", { id: myId }); //send election message to successor
}




// sending leader to sucessor
function announceLeader() {
    //console.log(`Server ${myId} announcing leadership.`);
    ringSocket.emit("LEADER", { leader: myId });
}





//check if server is alive every 5 seconds by sending a heartbeat
// if it is not the leader
// assigned leader should be started first
// setInterval(() => {
//     // if (myId == currentLeader) {
//     //     //console.log(`Checking if leader ${currentLeader} is alive...`);
//     //     ringSocket.emit("HEARTBEAT", { id: currentLeader });
//     // }
//     ringSocket.emit("HEARTBEAT", { id: myId });
// }, 10000);




//any timeout would cause an election
// setTimeout(() => {


//     const server = servers.find(s => s.id === myId);

//     if (server.nextId == currentLeader) {
//         server.next = ((server.next - 3003) + 1) % 3 + 3000; //update port
//         server.nextId = (server.nextId - 1) % 4 || 4; //update nextId

//         //start new connection with the next server, ignoring crashed ones
//         ringSocket = ioClient(`http://localhost:${server.next}/ring`, {
//             transports: ["websocket"]
//         });
//         startElection();


//     }
//     else {
//         server.next = ((server.next - 3003) + 1) % 3 + 3000; //update port
//         server.nextId = (server.nextId - 1) % 4 || 4; //update nextId
//         ringSocket = ioClient(`http://localhost:${server.next}/ring`, {
//             transports: ["websocket"]
//         });
//     }

// }, 10000);



function preserveEventListeners(oldSocket, newSocket) {
    //get all event listeners from the old socket
    const listeners = oldSocket._callbacks || {};

    //reattach each listener to the new socket
    Object.keys(listeners).forEach(event => {
        listeners[event].forEach(callback => {
            newSocket.on(event.replace("$", ""), callback); //fix Socket.IO's internal event naming
        });
    });
}


ringSocket.on("connect", () => {
    // console.log(`Server ${myId} connected to next server on port ${myNext}`);
});

ringSocket.on("disconnect", () => {
    
    const server = servers.find(s => s.id === myId);

    if (server.nextId == currentLeader) {
        server.next = ((server.next - 3000) + 1) % 4 + 3000; //update port  //((port - 3000) + 1) % 4 + 3000
        server.nextId = (server.nextId - 1) % 4 || 4; //update nextId

        const oldSocket = ringSocket;
        //start new connection with the next server, ignoring crashed ones
        ringSocket.disconnect();
        ringSocket = ioClient(links.get(server.next), {
            transports: ["websocket"]
        });
        //attachRingSocketListeners();
        preserveEventListeners(oldSocket, ringSocket);
        console.log(server.next);
        console.log(server.nextId);
        setTimeout(() => startElection(), 5000);



    }
    // else {
    //     server.next = ((server.next - 3000) + 1) % 4 + 3000; //update port
    //     server.nextId = (server.nextId - 1) % 4 || 4; //update nextId
    //     ringSocket = ioClient(`http://localhost:${server.next}/ring`, {
    //         transports: ["websocket"]
    //     });
    // }

});


io.listen(port);

console.log('UNO Server running on port ' + port);

