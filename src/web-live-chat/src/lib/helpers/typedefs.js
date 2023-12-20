/**
 * @typedef {Object} UserModel
 * @property {string} id - The user id.
 * @property {string} first_name - The user first name.
 * @property {string} last_name - The user last name.
 * @property {string} full_name - The user full name.
 * @property {string} email - The user email.
 * @property {string} role - The user role.
 */

/**
 * @typedef {Object} PluginDefModel
 * @property {string} id - The plugin full name.
 * @property {string} name - The plugin name.
 * @property {string} description - The plugin description.
 * @property {string} assembly - The plugin assembly.
 */

/**
 * @typedef {Object} AgentWelcomeInfo
 * @property {string[]} messages - The welcome messages in Rich content format.
 */

/**
 * @typedef {Object} AgentModel
 * @property {string} id - Agent Id.
 * @property {string} name - Agent name.
 * @property {string} description - Agent description.
 * @property {AgentWelcomeInfo} welcome_info - Welcome information.
 */

/**
 * @typedef {Object} ConversationModel
 * @property {string} id - The conversation id.
 * @property {string} title - The conversation title.
 * @property {UserModel} user - The conversation initializer.
 * @property {string} agent_id - The conversation agent id.
 * @property {number} unread_msg_count - The unread message count.
 * @property {Date} updated_time - The conversation updated time.
 * @property {Date} created_time - The conversation created time.
 */

/**
 * @interface
 * @class
 * @classdesc A basic rich content interface.
 */
function IRichContent() {}

/**
 * The type of the rich content.
 *
 * @name rich_type
 * @type {string}
 * @instance
 */
IRichContent.prototype.rich_type;

/**
 * The text of the rich content.
 *
 * @name text
 * @type {string}
 * @instance
 */
IRichContent.prototype.text;

/**
 * @typedef {Object} TextMessage
 * @property {string} text
 * @property {string} rich_type 
 */

/**
 * @typedef {Object} QuickReplyElement
 * @property {string} content_type
 * @property {string} title
 * @property {string} payload
 * @property {string} image_url 
 */

/**
 * @typedef {Object} QuickReplyMessage
 * @property {string} text
 * @property {string} rich_type
 * @property {QuickReplyElement[]} quick_replies
 */

/**
 * @typedef {Object} RichContent
 * @property {string} messaging_type
 * @property {IRichContent} message
 */

/**
 * @typedef {Object} ChatResponseModel
 * @property {string} conversation_id - The conversation id.
 * @property {UserModel} sender - The message sender.
 * @property {string} message_id - The message id.
 * @property {string} text - The message content.
 * @property {RichContent} rich_content - Rich content
 * @property {Date} created_at - The message sent time.
 */

/**
 * Invoked when a new conersation is created.
 * This callback type is called `requestCallback` and is displayed as a global symbol.
 *
 * @callback OnConversationInitialized
 * @param {ConversationModel} conversation
 */

/**
 * Invoked when message is received form chatHub.
 * This callback type is called `requestCallback` and is displayed as a global symbol.
 *
 * @callback OnMessageReceived
 * @param {ChatResponseModel} message
 */

// having to export an empty object here is annoying, 
// but required for vscode to pass on your types. 
export default {};