// this one file of a server
// the rest of the servers have the same code but different port numbers
// and myIds
'use strict';
//importing helper libraries
const http = require('http');
const socket = require('socket.io');
const server = http.createServer();
const { v4: uuidV4 } = require('uuid');
const port = 3000; // port of leader server

//socket connection with ping and pong
var io = socket(server, {
  pingInterval: 15000,
  pingTimeout: 5000,
  transports: ["websocket"]
});

const clientNamespace = io.of("/client"); // namespace for client separation only for client-server interaction


// authentication between client and server
clientNamespace.use((socket, next) => {
  if (socket.handshake.query.token === "UNITY") {
    next();
  } else {
    next(new Error("Authentication error"));
  }
});



const messageQueue = []; // queue to process messages in order
let isProcessing = false;
let rooms = new Map();// will store all rooms

clientNamespace.on("connection", (socket) => {
  console.log("A user connected");


  socket.emit("welcome", { serverIdFromServer: currentLeader }); // welcome clients


  socket.on('createRoom', async (data, callback) => { // room is created
    messageQueue.push({ socket, action: "createRoom", data, callback });
    processQueue();

  });

  socket.on('joinRoom', async (data, callback) => { // room is joined
    messageQueue.push({ socket, action: "joinRoom", data, callback });
    processQueue();
  });




  socket.on("drawCard", (data) => { // player draws a card
    // console.log(data)
    messageQueue.push({ socket, action: "drawCard", data });
    processQueue();
  });

  socket.on("sendDeck", (data) => { // player send deck pile to separate
    // console.log(data)
    messageQueue.push({ socket, action: "sendDeck", data });
    processQueue();
  });


  socket.on("sendTopCard", (data) => { //the one card that is on top of the pile
    // console.log(data)
    messageQueue.push({ socket, action: "sendTopCard", data });
    processQueue();
  });

  socket.on("wildcard", (data) => { // when player plays a wildcard
    messageQueue.push({ socket, action: "wildcard", data });
    processQueue();
  })

  socket.on("updateTurnAccess", (data) => { // when a player's turn is done
    messageQueue.push({ socket, action: "updateTurnAccess", data });
    processQueue();
  })




  socket.on("disconnect", () => { // when a disconnection happens
    console.log("A user disconnected");
  });
});


// FIFO to make sure a process is handle one at  a time
async function processQueue() {
  if (isProcessing || messageQueue.length === 0) {
    return;
  }

  isProcessing = true; // lock queue processing

  while (messageQueue.length > 0) {
    const { socket, action, data, callback } = messageQueue.shift();

    console.log("Processing message:", action);
    if (action === 'createRoom') {
      const roomId = data;
      const playerName = uuidV4();
      await socket.join(roomId);
      let reverse = false;
      let skip = false;
      let chosenColor = -1;
      rooms.set(roomId, {
        roomId: roomId,
        players: [{ playerName, socketId: socket.id }], //[player1, player2,  player3]
        reverse,// for when we need to reverse the order of player \
        chosenColor,// for when you place a wildcard
        playerHands: undefined, //hands of all players
        deck: undefined, // deck of game
        topCard: undefined, //current top card of game
        skip,
      });

      console.log(`Room created: ${roomId}`);
      if (callback) {
        callback(roomId, playerName); // that name of the player and the roomID is gonna be stored on Unity
      }
    }

    if (action === 'joinRoom') {
      // check if room exists and has a player waiting
      const roomId = data;
      console.log("Requested room:", roomId);
      const room = rooms.get(roomId);
      console.log(room);
      const playerName = uuidV4();
      // add the joining user's data to the list of players in the room
      if (room && room.players.length < 2) {
        await socket.join(roomId); // make the joining client join the room
        const roomUpdate = {
          ...room,
          players: [
            ...room.players,
            { playerName, socketId: socket.id },
          ],
        };
        rooms.set(roomId, roomUpdate);
        if (callback) {
          callback(roomId, playerName);
        }
        clientNamespace.to(roomId).emit("roomLength", rooms.get(roomId).players.length)
      }
      else {
        if (callback) {
          callback("Error");
        }
      }


    }
    if (action === 'drawCard') {
      handleDrawCard(socket, data);
      broadcastCardCounts(data.roomId);
    } else if (action === 'sendDeck') {
      handleSendDeck(socket, data);
      broadcastCardCounts(data.roomId);
    } else if (action === 'sendPlayerCards') {
      handleSendPlayerCards(socket, data);
      broadcastCardCounts(data.roomId);
    } else if (action === 'sendTopCard') {
      handleTopCard(socket, data);
      broadcastCardCounts(data.roomId);
    } else if (action === "wildcard") {
      handleWildCard(socket, data);
    } else if (action === "updateTurnAccess") {
      handleTurnAccess(socket, data);
      broadcastCardCounts(data.roomId);
    }


  }

  isProcessing = false; // unlock queue processing
}

let snapshotState = null; //snapshot of the system
let TOBtimestamp = 0;

function captureSnapshotState() {
  // takes a snapshot of the system
  const allRooms = {};
  rooms.forEach((room, roomId) => {
    allRooms[roomId] = {
      roomId: room.roomId,
      topCard: room.topCard,
      deck: room.deck,
      playerHands: room.playerHands,
      chosenColor: room.chosenColor,
      players: room.players,
      reverse: room.reverse
    };
  });
  return { id: myId, leader: currentLeader, rooms: allRooms };
}



function broadcastSnapshotToReplicas() {
  if (myId !== currentLeader) return;
  snapshotState = captureSnapshotState();
  snapshotState["timestamp"] = TOBtimestamp++;
  // console.log(`Broadcasting snapshot from Leader server ${myId}`)
  // console.log(JSON.stringify(snapshotState,null,2))
  for (let [port, link] of links) {

      const ioClient = require("socket.io-client")
      const peerSocket = ioClient(link, { transports: ["websocket"] });
      peerSocket.on("connect", () => {
          peerSocket.emit("REPLICA_SNAPSHOT", snapshotState);
          peerSocket.disconnect();
      });
  }
}


function broadcastCardCounts(roomId) {
  // sending how cards opponent has
  let room = rooms.get(roomId);
  if (!room || !room.playerHands) return;

  let counts = {};
  room.players.forEach(player => {
    counts[player.playerName] = room.playerHands[player.playerName] ? room.playerHands[player.playerName].length : 0;
    clientNamespace.to(roomId).emit("updateCardCounts", counts);
  });
}

function handleWildCard(socket, data) {
  // handling of wild cards

  const room = rooms.get(data.roomId);
  const roomUpdate = {
    ...room,
    chosenColor: data.chosenColor
  };
  rooms.set(data.roomId, roomUpdate);
  console.log(data.chosenColor);
  socket.broadcast.to(data.roomId).emit("wildcardColor", data.chosenColor);
}

//example handlers for events 

// handling of drawing a card
function handleDrawCard(socket, data) {
  console.log("Received drawn card:", data);
  const room = rooms.get(data.roomId);
  console.log(rooms);
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
}


// handling of top card
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

    if (data.topCard.includes("Reverse")) {
      room.reverse = !room.reverse;
      console.log(`Reverse card played. Reverse is now: ${room.reverse}`);
    }

    if (data.topCard.includes("Skip")) {
      room.skip = true;
      console.log("Skip card played!");
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

// handling sending deck to all clients
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


// sending player cards to each player
function handleSendPlayerCards(socket, data) {
  console.log("Received deck:", data);
  // gameObjects["playerHand"] = data;
  socket.emit('playerCardsSaved', "Server said why play UNO when Yugioh exists");
  broadcastSnapshotToReplicas();
}


// determines which player's turn it is
function handleTurnAccess(socket, data) {
  console.log(data);
  const room = rooms.get(data.roomId);
  const curr_player = room.players.findIndex((player) => player.playerName == data.playerName);
  console.log(curr_player);
  const len = room.players.length;
  let nextIndex;
  if (room.reverse) {
    if (len === 2) {
      nextIndex = curr_player;
      room.reverse = false;
    }
  }
  else if (room.skip) {
    if (len === 2) {
      nextIndex = curr_player;
      room.skip = false;
    }
  }
  else {
    if (curr_player === len - 1) nextIndex = 0;
    else nextIndex = curr_player + 1;
  }


  console.log("index: ", nextIndex)

  clientNamespace.to(room.players[nextIndex].socketId).emit("allowedTurn", "yourturn");
}



const ringNamespace = io.of("/ring"); // namespace for server-server connectin

// ports and ids of server to map port to next port 
const servers = [
  { id: 4, port: 3000, next: 3001, nextId: 3 },
  { id: 3, port: 3001, next: 3002, nextId: 2 },
  { id: 2, port: 3002, next: 3003, nextId: 1 },
  { id: 1, port: 3003, next: 3004, nextId: 0 },
  { id: 0, port: 3004, next: 3000, nextId: 4 },
];

const links = new Map();
//links of servers
links.set(3000, `http://localhost:3000/ring`);
links.set(3001, `http://localhost:3001/ring`);
links.set(3002, `http://localhost:3002/ring`);
links.set(3003, `http://localhost:3003/ring`);
links.set(3004, `http://localhost:3004/ring`);


const myId = 4;
const myNext = servers.find(s => s.id === myId).next; //next port


let currentLeader = 4; // current leader of system
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
    snapshotState = state; // update local state from leader


    if (snapshotState.timestamp <= TOBtimestamp) return
    TOBtimestamp = snapshotState.timestamp;
    for (let [port, link] of links) {

      const ioClient = require("socket.io-client")
      const peerSocket = ioClient(link, { transports: ["websocket"] });
      peerSocket.on("connect", () => {
        peerSocket.emit("REPLICA_SNAPSHOT", snapshotState);
        peerSocket.disconnect();
      });
    }


    //console.log("the snapshot", snapshotState);

    const new_rooms = new Map();
    Object.values(snapshotState.rooms).forEach(room => {
      new_rooms.set(room.roomId, {
        roomId: room.roomId,
        players: room.players, //[player1, player2,  player3]
        reverse: room.reverse,// for when we need to reverse the order of player \
        chosenColor: room.chosenColor,// for when you place a wildcard
        playerHands: room.playerHands, //hands of all players
        deck: room.deck, // deck of game
        topCard: room.topCard, //current top card of game
      })
    });



    rooms = new_rooms;
    // console.log(new_rooms)
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





//preserver all events when socket swaps to new leader or server
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

// connection to a server
ringSocket.on("connect", () => {

});

// disconnection to a server
ringSocket.on("disconnect", () => {

  const server = servers.find(s => s.id === myId);

  if (server.nextId == currentLeader) {
    server.next = (server.next === 3004) ? 3000 : server.next + 1; //update port  //(port === 3004) ? 3000 : port + 1
    server.nextId = (server.nextId === 0) ? 4 : server.nextId - 1; //update nextId //(id === 0) ? 4 : id - 1 

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

});


io.listen(port);

console.log('UNO Server running on port ' + port);

