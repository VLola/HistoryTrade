using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Telegram.Bot;
using Telegram.Bot.Types.InputFiles;
using TL;

namespace HistoryTrade
{
    public partial class Form1 : Form
    {
		long ChatId { get; set; }
		string path_screen = Directory.GetCurrentDirectory() + @"/screen.jpg";
		static string path = Directory.GetCurrentDirectory() + "/users/";
		private readonly ManualResetEventSlim _codeReady = new ManualResetEventSlim();
		private WTelegram.Client _client;
		private User _user;
		private List<Symbol> symbols = new List<Symbol>();
		private List<FullHistory> full_history = new List<FullHistory>();
		private static TelegramBotClient Bot;
		private static string token { get; set; } = "5432994682:AAEEWPQ_3G2xyLLPaYVzf_JGw0-dcwb--wY";
		public Form1()
		{
			InitializeComponent();
			DataGridViewColumn column1 = new DataGridViewColumn();
			DataGridViewColumn column2 = new DataGridViewColumn();
			DataGridViewColumn column3 = new DataGridViewColumn();
			DataGridViewColumn column4 = new DataGridViewColumn();
			DataGridViewColumn column5 = new DataGridViewColumn();
			DataGridViewColumn column6 = new DataGridViewColumn();
			DataGridViewColumn column7 = new DataGridViewColumn();
			DataGridViewColumn column8 = new DataGridViewColumn();
			DataGridViewColumn column9 = new DataGridViewColumn();
			DataGridViewColumn column10 = new DataGridViewColumn();
			column1.Name = "Symbol";
			column1.HeaderText = "Symbol";
			column1.Width = 100;
			column1.CellTemplate = new DataGridViewTextBoxCell();
			dataGridView1.Columns.Add(column1);
			column2.Name = "Strategy";
			column2.HeaderText = "Strategy";
			column2.Width = 50;
			column2.CellTemplate = new DataGridViewTextBoxCell();
			dataGridView1.Columns.Add(column2);
			column3.Name = "Count +";
			column3.HeaderText = "Count +";
			column3.Width = 50;
			column3.CellTemplate = new DataGridViewTextBoxCell();
			column3.DefaultCellStyle.BackColor = Color.LightGreen;
			dataGridView1.Columns.Add(column3);
			column4.Name = "Count -";
			column4.HeaderText = "Count -";
			column4.Width = 50;
			column4.CellTemplate = new DataGridViewTextBoxCell();
			column4.DefaultCellStyle.BackColor = Color.LightPink;
			dataGridView1.Columns.Add(column4);
			column5.Name = "Longs";
			column5.HeaderText = "Longs";
			column5.Width = 50;
			column5.CellTemplate = new DataGridViewTextBoxCell();
			dataGridView1.Columns.Add(column5);
			column6.Name = "Long +";
			column6.HeaderText = "Long +";
			column6.Width = 50;
			column6.CellTemplate = new DataGridViewTextBoxCell();
			column6.DefaultCellStyle.BackColor = Color.LightSkyBlue;
			dataGridView1.Columns.Add(column6);
			column7.Name = "Shorts";
			column7.HeaderText = "Shorts";
			column7.Width = 50;
			column7.CellTemplate = new DataGridViewTextBoxCell();
			dataGridView1.Columns.Add(column7);
			column8.Name = "Short +";
			column8.HeaderText = "Short +";
			column8.Width = 50;
			column8.CellTemplate = new DataGridViewTextBoxCell();
			column8.DefaultCellStyle.BackColor = Color.LightSkyBlue;
			dataGridView1.Columns.Add(column8);
			column9.Name = "Profit";
			column9.HeaderText = "Profit";
			column9.Width = 50;
			column9.CellTemplate = new DataGridViewTextBoxCell();
			column9.DefaultCellStyle.BackColor = Color.LightGreen;
			dataGridView1.Columns.Add(column9);
			column10.Name = "Loss";
			column10.HeaderText = "Loss";
			column10.Width = 50;
			column10.CellTemplate = new DataGridViewTextBoxCell();
			column10.DefaultCellStyle.BackColor = Color.LightPink;
			dataGridView1.Columns.Add(column10);
			WTelegram.Helpers.Log = (l, s) => Debug.WriteLine(s);
			Bot = new TelegramBotClient(token);
			UsersWatcher();
		}
		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			_client?.Dispose();
			Properties.Settings.Default.Save();
		}
		private void Button1_Click(object sender, System.EventArgs e)
		{
			new FormAddUser().ShowDialog();
		}
		private async void buttonLogin_Click(object sender, EventArgs e)
		{
			buttonLogin.Enabled = false;
			listBox.Items.Add($"Connecting & login into Telegram servers...");
			_client = new WTelegram.Client(Config);
			_user = await _client.LoginUserIfNeeded();
			listBox.Items.Add($"We are now connected as {_user}");
			_client.Update += Client_Update;
		}
		string Config(string what)
		{
			Model.User user = (Model.User)comboBox1.SelectedItem;
			switch (what)
			{
                case "api_id": return user.ApiId;
                case "api_hash": return user.ApiHash;
                case "phone_number": return user.PhoneNumber;
                case "verification_code":
				case "password":
					BeginInvoke(new Action(() => CodeNeeded(what.Replace('_', ' '))));
					_codeReady.Reset();
					_codeReady.Wait();
					return textBoxCode.Text;
				default: return null;
			};
		}
		private void CodeNeeded(string what)
		{
			labelCode.Text = what + ':';
			textBoxCode.Text = "";
			labelCode.Visible = textBoxCode.Visible = buttonSendCode.Visible = true;
			textBoxCode.Focus();
			listBox.Items.Add($"A {what} is required...");
		}
		private void buttonSendCode_Click(object sender, EventArgs e)
		{
			labelCode.Visible = textBoxCode.Visible = buttonSendCode.Visible = false;
			_codeReady.Set();
		}
		private void Client_Update(IObject arg)
		{
			UpdatesBase updates = (UpdatesBase)arg;
			if (updates == null) return;
            else
            {
				foreach (var update in updates.UpdateList)
                {
					switch (update)
                    {
						case UpdateNewMessage unm: DisplayMessage(unm.message); break;
					}
				}

			}
		}
		private void DisplayMessage(MessageBase messageBase)
		{
            //if (true)
            if (messageBase.Peer.ID == 1729192251)
            {
				TL.Message m = (TL.Message)messageBase;
				if (m.message.Contains("#") && !m.message.Contains("Sold") && !m.message.Contains("Bought"))
				{
					symbols.Add(new Symbol(m.message));
					full_history.Clear();
					dataGridView1.Rows.Clear();

					foreach (var item in symbols)
					{
						if (full_history.Count > 0)
						{
							bool check = false;
							foreach (var it in full_history)
							{
								if (item.SymbolName == it.SymbolName && item.Strategy == it.Strategy)
								{
									check = true;
									if (item.isPositive)
									{
										it.Profit += item.Profit;
										it.CountPositive++;
										if (item.isLong)
										{
											it.CountLong++;
											it.CountLongPlus++;
										}
										else
										{
											it.CountShort++;
											it.CountShortPlus++;
										}
									}
									else
									{
										it.Loss += item.Profit;
										it.CountNegative++;
										if (item.isLong) it.CountLong++;
										else it.CountShort++;
									}
								}
							}
							if (!check) AddHistory(item);
						}
						else AddHistory(item);
					}
					foreach (var it in full_history)
					{
						dataGridView1.Rows.Add(it.SymbolName, it.Strategy, it.CountPositive, it.CountNegative, it.CountLong, it.CountLongPlus,it.CountShort, it.CountShortPlus, it.Profit, it.Loss);
					}
					dataGridView1.AutoSize = true;
					dataGridView1.Sort(dataGridView1.Columns[0], ListSortDirection.Ascending);
					Bitmap bmp = new Bitmap(this.dataGridView1.Width - 30, this.dataGridView1.Height - 30);
					this.dataGridView1.DrawToBitmap(bmp, this.dataGridView1.ClientRectangle);
					bmp.Save(path_screen);

                    if(ChatId != 0)
                    {
						Telegram.Bot.Types.ChatId chatId = new Telegram.Bot.Types.ChatId(ChatId);
						FileStream fsSource = new FileStream(path_screen, FileMode.Open, FileAccess.Read);
						InputOnlineFile file = new InputOnlineFile(fsSource);
						Bot.SendPhotoAsync(chatId: chatId, photo: file);
					}
				}
			}
            if (checkBox1.Checked)
            {
				TL.Message m = (TL.Message)messageBase;
				if(m.message == "TEST")
                {
					Model.User user = (Model.User)comboBox1.SelectedItem;
					user.ChatId = messageBase.From.ID;
					checkBox1.Checked = false;
					string json = JsonConvert.SerializeObject(user);
					File.WriteAllText(path + user.UserName, json);
				}
			}
			
		}
		private void AddHistory(Symbol item)
        {
			FullHistory history = new FullHistory();
			history.SymbolName = item.SymbolName;
			history.Strategy = item.Strategy;
			if (item.isPositive)
			{
				history.Profit = item.Profit;
				history.CountPositive++;
				if (item.isLong)
				{
					history.CountLong++;
					history.CountLongPlus++;
				}
				else
				{
					history.CountShort++;
					history.CountShortPlus++;
				}
			}
			else
			{
				history.Loss = item.Profit;
				history.CountNegative++;
				if (item.isLong) history.CountLong++;
				else history.CountShort++;
			}
			full_history.Add(history);
		}
		private void UsersWatcher()
		{
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);
			LoadUsers();
			FileSystemWatcher watcher = new FileSystemWatcher();
			watcher.Path = path;
			watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastAccess | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Changed += Error_watcher_Changed;
			watcher.EnableRaisingEvents = true;
		}

        private void Error_watcher_Changed(object sender, FileSystemEventArgs e)
        {
			LoadUsers();
		}
		private void LoadUsers()
        {
            if (Directory.GetFiles(path).Length > 0)
            {
				comboBox1.Items.Clear();
				foreach (var item in Directory.GetFiles(path))
				{
					Model.User user = JsonConvert.DeserializeObject<Model.User>(File.ReadAllText(item));
					comboBox1.Items.Add(user);
				}
				comboBox1.SelectedIndex = 0;
				Model.User user_chat = (Model.User)comboBox1.SelectedItem;
				ChatId = user_chat.ChatId;
				label2.Text = ChatId.ToString();
			}
		}

        public class FullHistory
        {
			public string SymbolName { get; set; }
			public string Strategy { get; set; }
			public decimal Profit { get; set; }
			public decimal Loss { get; set; }
			public int CountPositive { get; set; }
			public int CountNegative { get; set; }
			public int CountLong { get; set; }
			public int CountShort { get; set; }
			public int CountLongPlus { get; set; }
			public int CountShortPlus { get; set; }
		}
		public class Symbol
        {
			public string SymbolName { get; set; }
			public string Strategy { get; set; }
			public decimal Profit { get; set; }
			public bool isPositive { get; set; }
			public bool isLong { get; set; }
			public Symbol(string text)
			{
				SymbolName = Regex.Match(text, @"#(.*),").Groups[1].Value;
				Strategy = Regex.Match(text, @"<(.*)>").Groups[1].Value;
				Profit = Convert.ToDecimal(Regex.Match(text, @"\$(.*) \(").Groups[1].Value.Replace('.', ','));
				if (Profit > 0m) isPositive = true;
				else isPositive = false;
				if (text.Contains(": ⬆ (F)")) isLong = true;
			}
        }
	}
}
