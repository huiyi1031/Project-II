const http = require('http');

const loginData = JSON.stringify({ email: 'tenant20@demo.com', password: 'Password123!' });

const loginReq = http.request({
  hostname: 'localhost',
  port: 5004,
  path: '/api/Auth/login',
  method: 'POST',
  headers: { 'Content-Type': 'application/json', 'Content-Length': loginData.length }
}, (res) => {
  let data = '';
  res.on('data', chunk => data += chunk);
  res.on('end', () => {
    const token = JSON.parse(data).token;
    console.log("Token:", token);

    // Call get my chats to get chat IDs
    const chatReq = http.request({
      hostname: 'localhost',
      port: 5004,
      path: '/api/Chats/my',
      method: 'GET',
      headers: { 'Authorization': `Bearer ${token}` }
    }, (chatRes) => {
      let chatData = '';
      chatRes.on('data', chunk => chatData += chunk);
      chatRes.on('end', () => {
        const chats = JSON.parse(chatData);
        console.log("Chats:", JSON.stringify(chats));

        const c = chats.find(x => x.requestNumber === 'REQ-2026-0036');
        if (c) {
          const partReq = http.request({
            hostname: 'localhost',
            port: 5004,
            path: `/api/Chats/${c.chatID}/participants`,
            method: 'GET',
            headers: { 'Authorization': `Bearer ${token}` }
          }, (partRes) => {
            let pData = '';
            partRes.on('data', chunk => pData += chunk);
            partRes.on('end', () => {
              console.log("Participants:", pData);
            });
          });
          partReq.end();
        }
      });
    });
    chatReq.end();
  });
});

loginReq.write(loginData);
loginReq.end();
