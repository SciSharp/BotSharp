Facebook Messenger
==================

The BotSharp Facebook integration allows you to easily create a Facebook Messenger bot with natural language understanding, based on the BotSharp technology.

**Setting Up Facebook**

In order to set up the Facebook integration for your agent, you'll need the following:

* A Facebook account

* A Facebook page to add your agent to

When a user visits your page and sends you a message, they'll be talking to your agent.

**Create a Facebook App**

1. Log into the `Facebook Developer Console`_.
2. Click on **My Apps** in the upper right hand corner.
3. Click on **Add a New App** and enter a display name and contact email.
4. Click **Create App ID**.
5. On the next page, click the **Set up** button for the **Messenger** option.
6. Under the **Token Generation** section, choose one of your Facebook pages (**Create a new page** if not exist).

This will generate a **Page Access Token**. Keep this token handy, as you'll need to enter it in BotSharp.

**Setting Up BotSharp**

1. Click on the Integrations option in the left menu and switch on Facebook Messenger. In the dialog that opens, enter the following information:
    * **Verify Token** - This can be any string and is solely for your purposes
    * **Page Access Token** - Enter the token generated in the Facebook Developer Console
2. Or edit agents.json under App_Data\DbInitializer\Agents, update **Page Access Token** and **Verify Token**.
3. Click the **Start** button.

**Webhook Configuration**

To configure your agent's webhook, return to the Facebook Developer Console:

1. Click the Setup Webhooks button under the Webhooks section and enter the following information:
    * **Callback URL** - This is the URL provided on the Facebook Messenger integration page
    * **Verify Token** - This is the token you created
    * Check the **messages** and **messaging_postbacks** options under Subscription Fields
2. Click the **Verify and Save** button.

**Testing**

In order to make your agent available for testing, you'll need to make your app public:

1. Click on **App Review** in the left menu of the Facebook Developer Console.
2. Click on the switch under **Make APP_NAME public**? You'll be prompted to choose a category for your app.
3. Choose **Apps for Messenger** from the list
4. Click the **Confirm** button.

You will also need to set a username for your page. This is the username users will chat with when using your agent. To set the username, click the **Create Page @Username** link under your page's profile picture and title.

.. _Facebook Developer Console: https://developers.facebook.com