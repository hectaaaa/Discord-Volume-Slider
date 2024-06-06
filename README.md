Version 1.0
Discord Volume Mixer is a Windows application that allows users to manage individual user volumes and mute statuses within a Discord voice channel. The application features a sleek dark mode interface with easy-to-use controls, making it simple to adjust the audio settings for each participant in your voice channel.

Features:
Dark Mode Interface: Enjoy a visually appealing and cohesive dark theme throughout the application.
User Management: View and manage the volumes and mute statuses of individual users within a Discord voice channel.
Custom Title Bar: The application features a custom title bar with integrated minimize, maximize, and close buttons that match the dark theme. It also includes the application logo and title for a personalized touch.
Go Live Button: A dedicated button to start and stop streaming, with customizable keystroke emulation.
Dynamic Updates: Automatically refresh the user list and adjust UI elements when users join or leave the voice channel.
Credential Management: Easily clear stored credentials to prompt for new ones on the next run.

How to Install:

1. Download the "Discord Volume Slider.zip" file
2. Go to the Discord developer portal https://discordapp.com/developers (if the link asks you for login and then shows the Discord app, close the window and click this link again) and create an application.
   -You must use the same account in to the Developer portal as in your Discord application, otherwise it won't work. (You can add the other account as app tester though.)
   -You're setting this stuff up for your own account, not for any bot or anything else.
3. Create a new application. You can name it however you like, for example "Discord Volume Mixer".
4. In the newly created application under "Installation" (this page could be hidden under the menu button on the top left corner in smaller windows), set "Install link" to "Discord provided link".
5. Hit "Save changes".
6. Under "OAuth2", add redirect to http://localhost:1337/callback
7. Hit "Save changes".
8. Copy Client ID and Client secret and paste it in your Discord Volume Mixer button settings (the button used to access the volume mixer).
   -If you don't see the client secret, but only the "Reset Secret" button, simply click on the button, it will give you a new secret.
9. Extract the "Discord Volume Slider.zip" file
10. Run the setup.exe file
11. Enter the Client ID (previously saved)
12. Enter the Secret key (previously saved)
13. Enjoy!

Stream button
- In order for the stream button to work you have to configure you macro in the app and in discord. 
