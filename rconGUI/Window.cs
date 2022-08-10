using CoreRCON;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace rconGUI
{
    public partial class Window : Form
    {
        public static Logger logger;

        public RCON client;

        public bool isConnected;

        List<string> prevCommands = new List<string>();
        int commandIndex = -1;

        public Window()
        {
            InitializeComponent();

            logger = new Logger((message) => 
            {
                ThreadSafeInvoke(() => consoleOutput.AppendText(message + "\n"));
            });
        }

        void ThreadSafeInvoke(Action action)
        {
            if (InvokeRequired)
                BeginInvoke(action);
            else
                action.Invoke();
        }

        public async void Connect()
        {
            connectButton.Enabled = false;

            await Task.Run(() =>
            {
                try
                {
                    client = new RCON(IPAddress.Parse(addressField.Text), (ushort)portField.Value, passwordField.Text);

                    // Open the connection
                    var connect = client.ConnectAsync();

                    logger.Log($"Connecting to {addressField.Text}:{portField.Value}...");

                    while (!connect.IsCompleted) { }

                    if (connect.IsCompletedSuccessfully)
                    {
                        var log = new LogReceiver(50000, new IPEndPoint(IPAddress.Parse(addressField.Text), (ushort)portField.Value));
                        log.ListenRaw(chat =>
                        {
                            logger.Log(chat);
                        });

                        isConnected = true;

                        logger.Log($"Connection successful!\n\n=== CONNECTION BEGIN ===\n");

                        client.OnDisconnected += () =>
                        {
                            logger.Log($"\n=== CONNECTION END ===\n\nDisconnected from {addressField.Text}:{portField.Value}");
                            isConnected = false;
                            UpdateUIState();
                        };

                        UpdateUIState();
                    }
                    else
                    {
                        throw new Exception("Invalid server address or credentials");
                    }

                }
                catch (Exception e)
                {
                    logger.Log($"Failed to connect; {e.Message}");
                }
            });

            connectButton.Enabled = true;
        }

        public void Disconnect()
        {
            client.Dispose();
        }

        void UpdateUIState()
        {
            ThreadSafeInvoke(() =>
            {
                connectButton.Text = isConnected ? "Disconnect" : "Connect";

                statusLabel.Text = isConnected ? $"Connected to {addressField.Text}:{portField.Value}" : "Not connected";

                addressField.Enabled = !isConnected;
                portField.Enabled = !isConnected;
                passwordField.Enabled = !isConnected;

                executeButton.Enabled = isConnected;
                commandField.Enabled = isConnected;
            });
        }

        public async void ExecuteCommand()
        {
            if(isConnected)
            {
                string cmd = commandField.Text;

                logger.Log($"> {cmd}");

                prevCommands = prevCommands.Prepend(cmd).ToList();
                commandIndex = -1;

                commandField.Text = "";

                commandField.Enabled = false;
                executeButton.Enabled = false;

                logger.Log(await client.SendCommandAsync(cmd));

                commandField.Enabled = true;
                executeButton.Enabled = true;

                commandField.Focus();
            }
        }

        private void Window_Load(object sender, EventArgs e)
        {
            addressField.Text = Properties.Settings.Default.address;
            portField.Value = Properties.Settings.Default.port;

            FormClosing += (s, a) =>
            {
                Properties.Settings.Default.address = addressField.Text;
                Properties.Settings.Default.port = portField.Value;

                Properties.Settings.Default.Save();
            };

            UpdateUIState();

            logger.Log("Welcome to rconGUI!\n\nTo connect to your servers RCON please enter your server details in the field above this text and hit continue.\n");
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            if (!isConnected)
                Connect();
            else
                Disconnect();
        }

        private void executeButton_Click(object sender, EventArgs e)
        {
            ExecuteCommand();
        }

        private void consoleOutput_TextChanged(object sender, EventArgs e)
        {
            consoleOutput.SelectionStart = consoleOutput.Text.Length;
            consoleOutput.ScrollToCaret();
        }

        private void commandField_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.KeyCode)
            {
                case Keys.Enter:
                    ExecuteCommand();
                    break;

                case Keys.Down:

                    commandIndex--;

                    if (commandIndex < 0 || prevCommands.Count == 0)
                    {
                        commandField.Text = "";
                        commandIndex = -1;
                    }
                    else
                    {
                        commandField.Text = prevCommands[commandIndex];
                    }

                    break;

                case Keys.Up:

                    if (commandIndex < prevCommands.Count - 1)
                    {
                        commandIndex++;
                        commandField.Text = prevCommands[commandIndex];
                    }

                    break;
            }
        }
    }
}
