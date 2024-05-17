using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Discord;
using Discord.WebSocket;
using Dec.DiscordIPC;
using Dec.DiscordIPC.Commands;
using Newtonsoft.Json;
using System.Drawing;
using System.IO;

namespace DiscordVolumeMixer
{
    public partial class Form1 : Form
    {
        private DiscordSocketClient _botClient;
        private DiscordIPC _ipcClient;
        private FlowLayoutPanel _userPanel;
        private AppConfig _config;
        private Dictionary<ulong, bool> _userMuteStatus;
        private Dictionary<ulong, Panel> _userPanels;

        public Form1()
        {
            InitializeComponent();
            this.Load += new EventHandler(Form1_Load);
            this.FormClosing += new FormClosingEventHandler(OnFormClosing);
            InitializeForm();
            InitializeMenu();
            ApplyDarkTheme();
            _userMuteStatus = new Dictionary<ulong, bool>();
            _userPanels = new Dictionary<ulong, Panel>();
        }

        private void InitializeForm()
        {
            this.Text = "Discord Volume Mixer";
            this.Width = 800;
            this.Height = 600;
            this.BackColor = System.Drawing.Color.FromArgb(54, 57, 63);

            _userPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10),
                BackColor = System.Drawing.Color.FromArgb(47, 49, 54)
            };
            this.Controls.Add(_userPanel);
        }

        private void InitializeMenu()
        {
            var menuStrip = new MenuStrip();
            menuStrip.BackColor = System.Drawing.Color.FromArgb(32, 34, 37);
            menuStrip.ForeColor = System.Drawing.Color.White;

            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.ForeColor = System.Drawing.Color.White;

            var clearCredentialsMenuItem = new ToolStripMenuItem("Clear Credentials");
            clearCredentialsMenuItem.Click += ClearCredentialsMenuItem_Click;
            clearCredentialsMenuItem.BackColor = System.Drawing.Color.FromArgb(32, 34, 37);
            clearCredentialsMenuItem.ForeColor = System.Drawing.Color.White;

            fileMenu.DropDownItems.Add(clearCredentialsMenuItem);
            menuStrip.Items.Add(fileMenu);
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void ClearCredentialsMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists("config.json"))
            {
                File.Delete("config.json");
                MessageBox.Show("Credentials cleared. The application will now prompt for new credentials on the next run.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ApplyDarkTheme()
        {
            this.BackColor = System.Drawing.Color.FromArgb(54, 57, 63);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Console.WriteLine("Form loaded. Loading config...");
            _config = ConfigManager.LoadConfig();
            if (_config == null)
            {
                Console.WriteLine("Config not found. Prompting user for config...");
                PromptUserForConfig();
                ConfigManager.SaveConfig(_config);
            }

            InitializeDiscordBotClient();
            InitializeDiscordIPCClient();
        }

        private void PromptUserForConfig()
        {
            _config = new AppConfig();

            _config.ClientId = Prompt.ShowDialog("Enter your Discord Application Client ID:", "Setup");
            _config.ClientSecret = Prompt.ShowDialog("Enter your Discord Application Client Secret:", "Setup");
            _config.BotToken = Prompt.ShowDialog("Enter your Discord Bot Token:", "Setup");
        }

        private async void InitializeDiscordBotClient()
        {
            Console.WriteLine("Initializing Discord bot client...");
            _botClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildVoiceStates
            });

            _botClient.Log += LogAsync;
            _botClient.Ready += BotReadyAsync;
            _botClient.UserVoiceStateUpdated += UserVoiceStateUpdatedAsync;
            _botClient.Disconnected += DisconnectedAsync;
            _botClient.Connected += ConnectedAsync;

            await _botClient.LoginAsync(TokenType.Bot, _config.BotToken);
            await _botClient.StartAsync();
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log);
            return Task.CompletedTask;
        }

        private Task BotReadyAsync()
        {
            Console.WriteLine("Bot is connected and ready.");
            return Task.CompletedTask;
        }

        private Task ConnectedAsync()
        {
            Console.WriteLine("Bot reconnected.");
            return Task.CompletedTask;
        }

        private async Task DisconnectedAsync(Exception ex)
        {
            Console.WriteLine($"Bot disconnected: {ex.Message}. Attempting to reconnect...");
            int retryDelay = 5000; // Initial retry delay in milliseconds

            while (true)
            {
                try
                {
                    await _botClient.StartAsync();
                    Console.WriteLine("Bot reconnected successfully.");
                    break;
                }
                catch (Exception reconnectEx)
                {
                    Console.WriteLine($"Reconnect attempt failed: {reconnectEx.Message}. Retrying in {retryDelay / 1000} seconds...");
                    await Task.Delay(retryDelay);
                    retryDelay = Math.Min(retryDelay * 2, 60000); // Exponential backoff with a maximum delay of 60 seconds
                }
            }
        }

        private async void InitializeDiscordIPCClient()
        {
            Console.WriteLine("Initializing DiscordIPC client...");
            _ipcClient = new DiscordIPC(_config.ClientId);
            await _ipcClient.InitAsync();
            Console.WriteLine("DiscordIPC client initialized.");

            string accessToken;
            try
            {
                Console.WriteLine("Preparing authorization request...");
                var authorizeArgs = new Authorize.Args
                {
                    scopes = new List<string> { "identify", "guilds.members.read", "guilds", "rpc.voice.read", "rpc.voice.write", "rpc" },
                    client_id = _config.ClientId
                };

                Console.WriteLine("Sending authorization request...");
                Authorize.Data codeResponse = await _ipcClient.SendCommandAsync(authorizeArgs);

                if (codeResponse == null || string.IsNullOrEmpty(codeResponse.code))
                {
                    Console.WriteLine("Failed to receive authorization code.");
                    return;
                }

                Console.WriteLine("Authorization request sent, received code: " + codeResponse.code);
                accessToken = await GetAccessTokenFromAuthCodeAsync(codeResponse.code);
                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("Failed to receive access token.");
                    return;
                }

                Console.WriteLine("Access token received: " + accessToken);
            }
            catch (ErrorResponseException ex)
            {
                Console.WriteLine("User denied authorization: " + ex.Message);
                MessageBox.Show("Authorization denied. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An unexpected error occurred: " + ex.Message);
                MessageBox.Show("An unexpected error occurred. Please try again later.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Authenticate
                Console.WriteLine("Sending authenticate request...");
                await _ipcClient.SendCommandAsync(new Authenticate.Args { access_token = accessToken });
                Console.WriteLine("Authenticate request sent.");

                // Get the currently selected voice channel
                Console.WriteLine("Retrieving selected voice channel...");
                var selectedChannelResponse = await _ipcClient.SendCommandAsync(new GetSelectedVoiceChannel.Args());

                if (selectedChannelResponse == null || string.IsNullOrEmpty(selectedChannelResponse.id))
                {
                    Console.WriteLine("User is not in a voice channel.");
                    MessageBox.Show("You are not in a voice channel.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string channelId = selectedChannelResponse.id;
                Console.WriteLine("User is in voice channel: " + channelId);

                // Retrieve details of the selected voice channel
                Console.WriteLine("Retrieving details of the selected voice channel...");
                var channelResponse = await _ipcClient.SendCommandAsync(new GetChannel.Args { channel_id = channelId });
                if (channelResponse == null)
                {
                    Console.WriteLine("Failed to retrieve channel details.");
                    MessageBox.Show("Failed to retrieve channel details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Display the channel details
                Console.WriteLine("Channel name: " + channelResponse.name);
                BeginInvoke(new Action(() =>
                {
                    var channelLabel = new Label { Text = "Channel: " + channelResponse.name, AutoSize = true, ForeColor = System.Drawing.Color.White };
                    _userPanel.Controls.Add(channelLabel);
                    _userPanel.SetFlowBreak(channelLabel, true);
                }));

                // Get users in the voice channel using the bot
                await GetUsersInVoiceChannel(channelId);
            }
            catch (ErrorResponseException ex)
            {
                Console.WriteLine($"ErrorResponseException: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                MessageBox.Show("An error occurred. Please try again later.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task GetUsersInVoiceChannel(string channelId)
        {
            if (_botClient == null)
            {
                Console.WriteLine("Bot client is not initialized.");
                return;
            }

            var channel = _botClient.GetChannel(ulong.Parse(channelId)) as IVoiceChannel;
            if (channel == null)
            {
                Console.WriteLine("Failed to retrieve voice channel.");
                return;
            }

            var users = await channel.GetUsersAsync().FlattenAsync();
            foreach (var user in users)
            {
                // Exclude the bot from the user list
                if (user.Id == _botClient.CurrentUser.Id)
                {
                    continue;
                }

                Console.WriteLine("User: " + user.Username);
                _userMuteStatus[user.Id] = false; // Initialize mute status to false

                await AddUserToPanelAsync(user);
            }
        }

        private async Task<PictureBox> GetUserAvatar(IGuildUser user)
        {
            string avatarUrl = user.GetAvatarUrl(size: 64) ?? user.GetDefaultAvatarUrl();
            var pictureBox = new PictureBox
            {
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Margin = new Padding(0, 0, 10, 0)
            };

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(avatarUrl);
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    pictureBox.Image = System.Drawing.Image.FromStream(stream);
                }
            }

            return pictureBox;
        }

        private async void VolumeSlider_Scroll(object sender, EventArgs e, ulong userId)
        {
            var slider = sender as TrackBar;
            if (slider == null)
            {
                return;
            }

            var volume = (int?)(slider.Value);

            try
            {
                var args = new SetUserVoiceSettings.Args
                {
                    user_id = userId.ToString(),
                    volume = volume
                };
                await _ipcClient.SendCommandAsync(args);
                Console.WriteLine($"Volume set to {volume} for user {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set volume: {ex.Message}");
            }
        }

        private async void MuteButton_Click(object sender, EventArgs e, ulong userId)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            bool isMuted = _userMuteStatus[userId];

            try
            {
                var args = new SetUserVoiceSettings.Args
                {
                    user_id = userId.ToString(),
                    mute = !isMuted
                };
                await _ipcClient.SendCommandAsync(args);
                _userMuteStatus[userId] = !isMuted;
                button.Text = isMuted ? "Mute" : "Unmute";
                Console.WriteLine($"User {userId} has been {(isMuted ? "unmuted" : "muted")}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to {(isMuted ? "unmute" : "mute")} user: {ex.Message}");
            }
        }

        private Task UserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            // Move all the work to a background task
            Task.Run(async () =>
            {
                Console.WriteLine($"UserVoiceStateUpdatedAsync: {user.Username}, {oldState.VoiceChannel?.Name} -> {newState.VoiceChannel?.Name}");

                if (!(user is IGuildUser guildUser) || _userPanels == null || _botClient == null) return;

                if (newState.VoiceChannel != null)
                {
                    // User joined a voice channel
                    if (newState.VoiceChannel.Id != oldState.VoiceChannel?.Id)
                    {
                        if (!_userPanels.ContainsKey(user.Id))
                        {
                            // Add user to the panel
                            await AddUserToPanelAsync(guildUser);
                        }
                    }
                }
                else
                {
                    // User left the voice channel
                    if (_userPanels.ContainsKey(user.Id))
                    {
                        // Remove user from the panel
                        var panel = _userPanels[user.Id];
                        _userPanels.Remove(user.Id);
                        BeginInvoke(new Action(() => _userPanel.Controls.Remove(panel)));
                    }
                }
            });

            return Task.CompletedTask;
        }

        private async Task AddUserToPanelAsync(IGuildUser user)
        {
            _userMuteStatus[user.Id] = false;

            var userPanel = new Panel
            {
                Width = 325,
                Height = 100,
                Margin = new Padding(10),
                BackColor = System.Drawing.Color.FromArgb(54, 57, 63),
                Anchor = AnchorStyles.Left // Anchor to the left
            };

            var avatar = await GetUserAvatar(user);
            var userLabel = new Label
            {
                Text = user.Username,
                AutoSize = true,
                Margin = new Padding(10, 0, 0, 0),
                ForeColor = System.Drawing.Color.White,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };

            var volumeSlider = new TrackBar
            {
                Minimum = 0,
                Maximum = 200,
                Value = 100,
                Width = 150,
                TickStyle = TickStyle.None,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            volumeSlider.Scroll += (s, e) => VolumeSlider_Scroll(s, e, user.Id);

            var muteButton = new Button
            {
                Text = "Mute",
                BackColor = System.Drawing.Color.FromArgb(32, 34, 37),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(10, 0, 0, 0)
            };
            muteButton.Click += (s, e) => MuteButton_Click(s, e, user.Id);

            userPanel.Controls.Add(avatar);
            userPanel.Controls.Add(userLabel);
            userPanel.Controls.Add(volumeSlider);
            userPanel.Controls.Add(muteButton);

            avatar.Location = new Point(10, 10);
            userLabel.Location = new Point(80, 10);
            volumeSlider.Location = new Point(80, 40);
            muteButton.Location = new Point(240, 40);

            BeginInvoke(new Action(() =>
            {
                _userPanel.Controls.Add(userPanel);
                _userPanels[user.Id] = userPanel;
            }));
        }

        private async Task<string> GetAccessTokenFromAuthCodeAsync(string authCode)
        {
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                {
                    { "client_id", _config.ClientId },
                    { "client_secret", _config.ClientSecret },
                    { "grant_type", "authorization_code" },
                    { "code", authCode },
                    { "redirect_uri", "http://localhost:1337/callback" }
                };

                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync("https://discord.com/api/oauth2/token", content);
                var responseString = await response.Content.ReadAsStringAsync();

                var tokenResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
                if (tokenResponse != null && tokenResponse.ContainsKey("access_token"))
                {
                    return tokenResponse["access_token"];
                }
                else
                {
                    Console.WriteLine("Failed to parse access token from response.");
                    return null;
                }
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            Console.WriteLine("Form is closing, disposing clients...");
            _ipcClient?.Dispose();
            _botClient?.Dispose();
            Console.WriteLine("Clients disposed.");
        }
    }

    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Console.WriteLine("Creating prompt form...");
            Form prompt = new Form()
            {
                Width = 500,
                Height = 200,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowIcon = false,
                ShowInTaskbar = false,
                BackColor = System.Drawing.Color.FromArgb(54, 57, 63),
                ForeColor = System.Drawing.Color.White
            };

            Label textLabel = new Label() { Left = 20, Top = 20, Text = text, AutoSize = true, ForeColor = System.Drawing.Color.White };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 440, BackColor = System.Drawing.Color.FromArgb(64, 68, 75), ForeColor = System.Drawing.Color.White };
            Button confirmation = new Button() { Text = "Ok", Left = 360, Width = 100, Top = 90, DialogResult = DialogResult.OK, BackColor = System.Drawing.Color.FromArgb(32, 34, 37), ForeColor = System.Drawing.Color.White };

            confirmation.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            Console.WriteLine("Showing prompt dialog...");
            var result = prompt.ShowDialog();
            Console.WriteLine("Prompt dialog closed.");

            return result == DialogResult.OK ? textBox.Text : string.Empty;
        }
    }
}
