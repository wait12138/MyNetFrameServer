using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UI
{
    public class MainForm : Form
    {
        private Label lblIp;
        private TextBox txtIp;
        private Label lblPort;
        private TextBox txtPort;
        private Button btnStart;
        private Button btnStop;
        private Button btnRefresh;
        private Button btnRefreshScene;
        private CheckBox chkAutoRefresh;
        private ListBox lstOnline;
        private DataGridView dgvPlayers;
        private DataGridView dgvObjects;
        private System.Windows.Forms.Timer autoRefreshTimer;
        private TextBox txtLog;

        public MainForm()
        {
            Text = "MyNetFrame 服务器控制台";
            Width = 960;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;

            lblIp = new Label { Text = "IP:", Left = 12, Top = 14, Width = 30 };
            txtIp = new TextBox { Left = 48, Top = 10, Width = 140, Text = "127.0.0.1" };

            lblPort = new Label { Text = "端口:", Left = 200, Top = 14, Width = 40 };
            txtPort = new TextBox { Left = 244, Top = 10, Width = 80, Text = "8080" };

            btnStart = new Button { Text = "启动", Left = 340, Top = 8, Width = 80, Height = 28 };
            btnStop = new Button { Text = "停止", Left = 430, Top = 8, Width = 80, Height = 28, Enabled = false };
            btnRefresh = new Button { Text = "刷新在线列表", Left = 520, Top = 8, Width = 140, Height = 28 };
            btnRefreshScene = new Button { Text = "刷新场景信息", Left = 666, Top = 8, Width = 140, Height = 28 };
            chkAutoRefresh = new CheckBox { Text = "自动刷新", Left = 812, Top = 12, Width = 90, Height = 20 };

            lstOnline = new ListBox { Left = 12, Top = 48, Width = 300, Height = 240, Anchor = AnchorStyles.Top | AnchorStyles.Left };

            dgvPlayers = new DataGridView
            {
                Left = 324,
                Top = 48,
                Width = 600,
                Height = 240,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            dgvObjects = new DataGridView
            {
                Left = 324,
                Top = 300,
                Width = 600,
                Height = 248,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            txtLog = new TextBox { Left = 12, Top = 300, Width = 300, Height = 248, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };

            autoRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };

            Controls.AddRange(new Control[] { lblIp, txtIp, lblPort, txtPort, btnStart, btnStop, btnRefresh, btnRefreshScene, chkAutoRefresh, lstOnline, dgvPlayers, dgvObjects, txtLog });

            btnStart.Click += (_, __) => StartServer();
            btnStop.Click += (_, __) => StopServer();
            btnRefresh.Click += (_, __) => RefreshOnlineList();
            btnRefreshScene.Click += (_, __) => RefreshSceneInfo();
            chkAutoRefresh.CheckedChanged += (_, __) => autoRefreshTimer.Enabled = chkAutoRefresh.Checked;
            autoRefreshTimer.Tick += (_, __) => { RefreshSceneInfo(); RefreshOnlineList(); };

            Load += (_, __) => InitLogging();
            FormClosing += (_, e) => { try { Program.serverSocket?.Close(); } catch { } };

            InitGrids();
        }

        private void InitLogging()
        {
            Console.SetOut(new TextBoxWriter(txtLog));
            Console.SetError(new TextBoxWriter(txtLog));
            Log("日志输出已重定向到GUI\r\n");
        }

        private void StartServer()
        {
            if (Program.serverSocket != null)
            {
                Log("服务器已在运行\r\n");
                return;
            }
            var ip = txtIp.Text.Trim();
            if (!int.TryParse(txtPort.Text.Trim(), out var port))
            {
                MessageBox.Show("端口必须为数字", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                Program.serverSocket = new ServerSocket();
                Program.serverSocket.Start(ip, port);
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                Log($"服务器启动成功：{ip}:{port}\r\n");
                RefreshOnlineList();
            }
            catch (Exception ex)
            {
                Log("启动失败: " + ex.Message + "\r\n");
            }
        }

        private void StopServer()
        {
            try
            {
                Program.serverSocket?.Close();
                Program.serverSocket = null;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                Log("服务器已停止\r\n");
                lstOnline.Items.Clear();
            }
            catch (Exception ex)
            {
                Log("停止失败: " + ex.Message + "\r\n");
            }
        }

        private void RefreshOnlineList()
        {
            lstOnline.Items.Clear();
            var list = Program.serverSocket?.GetOnlinePlayerList() ?? new List<string>();
            if (list.Count == 0)
            {
                lstOnline.Items.Add("暂无在线玩家");
            }
            else
            {
                foreach (var id in list)
                {
                    lstOnline.Items.Add(id);
                }
            }
        }

        private void InitGrids()
        {
            // Players grid columns
            dgvPlayers.Columns.Clear();
            dgvPlayers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "玩家ID", DataPropertyName = "clientID", Width = 120 });
            dgvPlayers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "posX", DataPropertyName = "posX", Width = 80 });
            dgvPlayers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "posY", DataPropertyName = "posY", Width = 80 });
            dgvPlayers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "posZ", DataPropertyName = "posZ", Width = 80 });
            dgvPlayers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "rotX", DataPropertyName = "rotX", Width = 80 });
            dgvPlayers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "rotY", DataPropertyName = "rotY", Width = 80 });
            dgvPlayers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "rotZ", DataPropertyName = "rotZ", Width = 80 });

            // Objects grid columns
            dgvObjects.Columns.Clear();
            dgvObjects.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "物体ID", DataPropertyName = "objectID", Width = 120 });
            dgvObjects.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "posX", DataPropertyName = "posX", Width = 80 });
            dgvObjects.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "posY", DataPropertyName = "posY", Width = 80 });
            dgvObjects.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "posZ", DataPropertyName = "posZ", Width = 80 });
            dgvObjects.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "rotX", DataPropertyName = "rotX", Width = 80 });
            dgvObjects.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "rotY", DataPropertyName = "rotY", Width = 80 });
            dgvObjects.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "rotZ", DataPropertyName = "rotZ", Width = 80 });
        }

        private void RefreshSceneInfo()
        {
            RefreshPlayersGrid();
            RefreshObjectsGrid();
        }

        private void RefreshPlayersGrid()
        {
            var players = new List<PlayerInfo>();
            if (Program.serverSocket != null)
            {
                lock (Program.serverSocket.playerInfoDic)
                {
                    players = new List<PlayerInfo>(Program.serverSocket.playerInfoDic.Values);
                }
            }
            dgvPlayers.DataSource = players.Select(p => new {
                p.clientID, p.posX, p.posY, p.posZ, p.rotX, p.rotY, p.rotZ
            }).ToList();
        }

        private void RefreshObjectsGrid()
        {
            var objects = new List<ObjectInfo>();
            if (Program.serverSocket != null)
            {
                lock (Program.serverSocket.objectInfoDic)
                {
                    objects = new List<ObjectInfo>(Program.serverSocket.objectInfoDic.Values);
                }
            }
            dgvObjects.DataSource = objects.Select(o => new {
                o.objectID, o.posX, o.posY, o.posZ, o.rotX, o.rotY, o.rotZ
            }).ToList();
        }

        private void Log(string text)
        {
            if (txtLog.IsHandleCreated)
            {
                if (txtLog.InvokeRequired)
                {
                    txtLog.Invoke(new Action(() => txtLog.AppendText(text)));
                }
                else
                {
                    txtLog.AppendText(text);
                }
            }
        }

        private sealed class TextBoxWriter : TextWriter
        {
            private readonly TextBox _box;
            public TextBoxWriter(TextBox box) => _box = box;
            public override Encoding Encoding => Encoding.UTF8;
            public override void Write(char value) => Append(value.ToString());
            public override void Write(string? value) => Append(value ?? string.Empty);
            public override void WriteLine(string? value) => Append((value ?? string.Empty) + Environment.NewLine);

            private void Append(string text)
            {
                if (!_box.IsHandleCreated) return;
                if (_box.InvokeRequired)
                {
                    _box.Invoke(new Action(() => _box.AppendText(text)));
                }
                else
                {
                    _box.AppendText(text);
                }
            }
        }
    }
}