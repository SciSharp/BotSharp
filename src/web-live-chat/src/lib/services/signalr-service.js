import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { chatHubUrl } from '$lib/services/api-endpoints.js';
import { getUserStore } from '$lib/helpers/store.js';

// create a writable store to store the connection object
/** @type {HubConnection} */
let connection;

// create a SignalR service object that exposes methods to interact with the hub
export const signalr = {

  /** @type {function} */
  onConversationInitFromClient: () => {},

  /** @type {function} */
  onMessageReceivedFromClient: () => {},

  /** @type {function} */
  onMessageReceivedFromCsr: () => {},
  
  /** @type {function} */
  onMessageReceivedFromAssistant: () => {},

  // start the connection
  /** @param {string} conversationId */
  async start(conversationId) {
    // create a new connection object with the hub URL and some options
    let user = getUserStore();
    connection = new HubConnectionBuilder()
      .withUrl(chatHubUrl + `?conversationId=${conversationId}&access_token=${user.token}`) // the hub URL, change it according to your server
      .withAutomaticReconnect() // enable automatic reconnection
      .configureLogging(LogLevel.Information) // configure the logging level
      .build();

    // start the connection
    try {
      await connection.start();
      console.log('Connected to SignalR hub');
    } catch (err) {
      console.error(err);
    }

    // register handlers for the hub methods
    connection.on('OnConversationInitFromClient', (conversation) => {
      // do something when receiving a message, such as updating the UI or showing a notification
      console.log(`[OnConversationInitFromClient] ${conversation.id}: ${conversation.title}`);
      this.onConversationInitFromClient(conversation);
    });

    // register handlers for the hub methods
    connection.on('OnMessageReceivedFromClient', (message) => {
      // do something when receiving a message, such as updating the UI or showing a notification
      console.log(`[OnMessageReceivedFromClient] ${message.sender.role}: ${message.text}`);
      this.onMessageReceivedFromClient(message);
    });

    connection.on('OnMessageReceivedFromCsr', (message) => {
      // do something when receiving a message, such as updating the UI or showing a notification
      console.log(`[OnMessageReceivedFromCsr] ${message.role}: ${message.content}`);
      this.onMessageReceivedFromCsr(message);
    });

    connection.on('OnMessageReceivedFromAssistant', (message) => {
      // do something when receiving a message, such as updating the UI or showing a notification
      console.log(`[OnMessageReceivedFromAssistant] ${message.sender.role}: ${message.text}`);
      this.onMessageReceivedFromAssistant(message);
    });
  },

  // stop the connection
  async stop() {
    // get the connection object from the store
    // const connection = connection.get();
    // stop the connection if it exists
    if (connection) {
      try {
        await connection.stop();
        console.log('Disconnected from SignalR hub');
      } catch (err) {
        console.error(err);
      }
    }
  },

      // get the connection object from the store
    // const connection = connection.get();
    // invoke the hub method if the connection exists and is connected
    /*if (connection && connection.state === HubConnectionState.Connected) {
        try {
        await connection.invoke('OnMessageReceivedFromClient', message);
        console.log(`Sent message from client: ${message}`);
        } catch (err) {
        console.error(err);
        }
    }*/
};
